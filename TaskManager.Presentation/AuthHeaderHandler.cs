using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;

namespace TaskManager.Presentation
{
    /// <summary>
    /// Adds a user's token to their request headers. 
    /// </summary>
    /// <param name="authStateProvider"></param>
    public class AuthHeaderHandler(AuthenticationStateProvider authStateProvider) : DelegatingHandler
    {
        private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("jwt_token")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}