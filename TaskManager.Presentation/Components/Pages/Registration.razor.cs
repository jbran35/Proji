using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class Registration : AppComponentBase
    {
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        private readonly RegisterModel _registerModel = new();
        public class RegisterModel
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

        }

        public class RegisterResponse
        {
            public string? Token { get; set; }
        }

        private async Task RegisterUser()
        {
            var client = ApiClientService.GetClient();
            var response = await client.PostAsJsonAsync("api/Account/registration", _registerModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();

                if (result is not null)
                    ToastService.Notify(new(ToastType.Success, result.Message));

                NavigationManager.NavigateTo("/login");
            }
        }
    }
}
