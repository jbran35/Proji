using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TaskManager.Application.Users.DTOs.Requests;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// A modal where a user can udpate their profile details (First/Last Name, Email, UserName)
    /// </summary>
    public partial class EditProfileModal : AppComponentBase
    {
        #region Injections
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected IJSRuntime Js { get; set; } = default!;
        [Inject] protected ProfileStateService ProfileStateService { get; set; } = default!;
        #endregion

        private Modal _editProfileModal = default!;
        private bool _isSaved;
        private readonly UpdateProfileRequest _request = new();

        public async Task OnShowEditProfileModalClick(string firstName, string lastName, string email, string userName)
        {
            _request.FirstName = firstName;
            _request.LastName = lastName;
            _request.Email = email;
            _request.UserName = userName;
            await _editProfileModal.ShowAsync();
        }
        private async Task OnHideEditProfileModalClick() => await _editProfileModal.HideAsync();
        private async Task OnUpdateProfileClick()
        {
            _isSaved = true; // Disables button to prevent multiple calls

            var client = ApiClientService.GetClient();
            var response = await client.PatchAsJsonAsync("api/Account/profile", _request);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
            if (result is null)
            {
                ToastService.Notify(new(ToastType.Danger, "Unexpected Error Updating Your Profile"));
                return;
            }

            var profile = result.Profile;
            if (profile is not null)
                ProfileStateService.SetProfile(profile.Id, profile.FirstName, profile.LastName, profile.Email, profile.UserName); // Updating UI

            // Used to overwrite the user's cookie, containing their claims, w/o forcing a page reload
            // (State Service updates UI immediately, this allows for their new claims to be displayed upon refresh).
            await Js.InvokeVoidAsync("fetch", "/account/refresh-identity"); 
            await _editProfileModal.HideAsync();

            _isSaved = false;
            ToastService.Notify(new(ToastType.Success, "Profile Updated Successfully"));
        }
    }
}
