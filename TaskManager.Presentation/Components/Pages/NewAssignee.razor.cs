using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class NewAssignee : AppComponentBase
    {
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;

        private bool _assigneeFound;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private Guid AssigneeId { get; set; } = Guid.Empty;

        public async Task SearchForUser()
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                ToastService.Notify(new(ToastType.Danger, "Please Enter An Email"));
                return;
            }

            var client = ApiClientService.GetClient();
            var response = await client.GetAsync($"api/Account/Search/{_email}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ToastService.Notify(new(ToastType.Danger, errorMessage));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<GetUserResponse>();
            if (result is null)
            {
                ToastService.Notify(new(ToastType.Danger, "Unexpected Error Adding Assignee To Your Group"));
                return;
            }

            _firstName = result.FirstName;
            _lastName = result.LastName;
            _email = result.Email;
            AssigneeId = result.Id;

            _assigneeFound = true;
            ToastService.Notify(new(ToastType.Success, "User Found!"));
        }

        public async Task<GetUserResponse> GetUserDetails()
        {
            var assignee = new GetUserResponse
            {
                FirstName = _firstName,
                LastName = _lastName,
                Email = _email,
                Id = AssigneeId
            };

            if (assignee.Id == Guid.Empty)
                return new GetUserResponse();

            return assignee;
        }
    }
}
