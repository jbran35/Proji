using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Security.Claims;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure;
using TaskManager.Presentation;
using TaskManager.Presentation.Components;
using TaskManager.Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazorBootstrap();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AssignedTodoItemsStateService>();
builder.Services.AddScoped<AssigneeListStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider<User>>();
builder.Services.AddScoped<CircuitHandler, CacheCleanupCircuitHandler>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ProfileStateService>();
builder.Services.AddScoped<ProjectApiService>();
builder.Services.AddScoped<ProjectSortStateService>();
builder.Services.AddScoped<ProjectStateService>();
builder.Services.AddScoped<SignalRConnectionService>();
builder.Services.AddScoped<TodoItemDraftStateService>();
builder.Services.AddScoped<TokenProviderService>();
builder.Services.AddSignalR();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.AllowedForNewUsers = false;
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireDigit = true;

})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "BlazorAuth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/denied";
    options.SlidingExpiration = true;
});

#endregion

#region API Setup

builder.Services.AddHttpClient<ApiClientService>("PublicAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7109");
}).AddTransientHttpErrorPolicy(policyBuilder =>
    policyBuilder.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5)));
    

builder.Services.AddHttpClient<ApiClientService>("API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7109");

}).AddHttpMessageHandler<AuthHeaderHandler>()
.AddTransientHttpErrorPolicy(policyBuilder =>
    policyBuilder.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5)));


builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var authProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var authHandler = new AuthHeaderHandler(authProvider)
    {
        InnerHandler = new HttpClientHandler()
    };

    var baseClient = factory.CreateClient("API");
    var authenticatedClient = new HttpClient(authHandler)
    {
        BaseAddress = baseClient.BaseAddress
    };

    return new ApiClientService(authenticatedClient);
});

#endregion

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase)
        && context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/myProjects");
        return;
    }
    await next();
});

#region Endpoints
app.MapPost("/account/login", async ([FromForm] string username, [FromForm] string password,
    SignInManager<User> signInManager, UserManager<User> userManager, [FromServices] ITokenService tokenService,
    [FromServices] IHttpClientFactory httpClientFactory) =>
{

    var client = httpClientFactory.CreateClient("PublicAPI");
    var requestBody = new { UserName = username, Password = password };
    
    var response = await client.PostAsJsonAsync("api/Account/login", requestBody);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Redirect("/login?error=InvalidCredentials");
    }

    var result = await response.Content.ReadFromJsonAsync<LoginUserResponse>();
    if (result is null || string.IsNullOrEmpty(result.Token))
        return Results.Redirect("/login?error=InvalidCredentials");

    var user = await userManager.FindByNameAsync(username);
    if (user is null)
        return Results.Redirect("/login?error=InvalidCredentials");

    var stamp = await userManager.GetSecurityStampAsync(user);
    if (string.IsNullOrEmpty(stamp))
        await userManager.UpdateSecurityStampAsync(user);

    var tokenClaim = new List<Claim>
        {
            new("jwt_token", result.Token),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

    await signInManager.SignInWithClaimsAsync(user, isPersistent: true, tokenClaim);
    return Results.Redirect("/myProjects");
});


app.MapPost("/account/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});


app.MapGet("/account/refresh-identity", async (
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    ClaimsPrincipal principal,
    HttpContext context,
    [FromServices] ITokenService tokenService) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
        return Results.Redirect("/login");

    var token = principal.FindFirst("jwt_token")?.Value;
    if (string.IsNullOrWhiteSpace(token))
        token = tokenService.CreateToken(user);

    var newClaims = new List<Claim>
        {
            new("jwt_token", token),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

    await signInManager.SignInWithClaimsAsync(user, isPersistent: true, newClaims); //Overwrite the existing cookie, containing the new claims for the user.

    return Results.Redirect(context.Request.Headers.Referer.ToString());
});

#endregion

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

