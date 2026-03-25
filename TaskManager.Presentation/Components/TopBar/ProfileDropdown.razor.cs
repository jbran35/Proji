using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.TopBar
{
    /// <summary>
    /// Displays a user's profile details, offers them the option to update their profile, offers a logout button.
    /// </summary>
    public partial class ProfileDropdown : ComponentBase, IDisposable
    {
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private ProfileStateService ProfileStateService { get; set; } = default!;

        private EditProfileModal _editProfileModal = default!;

        private string FirstName { get; set; } = string.Empty;
        private string LastName { get; set; } = string.Empty;
        private string Email { get; set; } = string.Empty;
        private string UserName { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            ProfileStateService.OnProfileChanged += HandleProfileUpdated;
            await LoadProfile();
        }
        private async Task LoadProfile()
        {
            var profile = ProfileStateService.GetProfile(); //After update, can pull details from cache. If not - pull from new/fresh cookie.
            FirstName = profile.FirstName;
            LastName = profile.LastName;
            Email = profile.Email;
            UserName = profile.UserName;

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(UserName))
                await PullFromClaims();

            StateHasChanged();
        }
        private async Task PullFromClaims()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value ?? "Couldn't Load First Name";
            LastName = user.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
            Email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }
        public void HandleProfileUpdated()
        {
            InvokeAsync(async () =>
            {
                await LoadProfile();
            });
        }
        public void Dispose() => ProfileStateService.OnProfileChanged -= HandleProfileUpdated;
        private async Task OnShowEditProfileModal()
        {
            await LoadProfile();

            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(UserName))
                await _editProfileModal.OnShowEditProfileModalClick(FirstName, LastName, Email, UserName);
        }
    }
}

