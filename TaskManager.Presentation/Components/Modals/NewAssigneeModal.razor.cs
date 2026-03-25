using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Application.UserConnections.DTOs.Requests;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// Allows the user to add a new assignee/user to their group. 
    /// Can be accessed from MyGroup, EditTodoItemModal, or AddTodoItemModal
    /// </summary>
    public partial class NewAssigneeModal : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected AssigneeListStateService AssigneeListStateService { get; set; } = default!;
        [Inject] protected TodoItemDraftStateService TodoItemDraftStateService { get; set; } = default!;
        [Parameter] public EventCallback OnAssigneeSaved { get; set; }
        [Parameter] public EventCallback OnCanceled { get; set; }
        #endregion

        private Guid AssigneeId { get; set; } = Guid.Empty;
        private Modal _newAssigneeModal = default!;
        private bool _assigneeFound;
        private string _email = string.Empty;

        public async Task OnShowNewAssigneeModal() => await _newAssigneeModal.ShowAsync();

        public async Task OnCancelClick()
        {
            await _newAssigneeModal.HideAsync();
            await OnCanceled.InvokeAsync();
        }

        public async Task SearchForUser()
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                ToastService.Notify(new(ToastType.Danger, "Please Enter An Email"));
                return;
            }

            var client = ApiClientService.GetClient();
            var response = await client.GetAsync($"api/Account/{_email}");

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

            _email = result.Email;
            AssigneeId = result.Id;
            _assigneeFound = true; //Disables the Search button and enables the Add button.
            ToastService.Notify(new(ToastType.Success, "User Found!"));
        }

        public async void OnSaveAssigneeModalClick()
        {
            var request = new CreateUserConnectionRequest
            {
                AssigneeId = AssigneeId,
            };

            var client = ApiClientService.GetClient();
            var response = await client.PostAsJsonAsync($"api/Account/assignees", request);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<UserConnectionDto>();
            if (result is not null)
            {
                await AssigneeListStateService.SetAssigneeInCacheAsync(result);
                TodoItemDraftStateService.SetAssigneeInModel(result);

                ToastService.Notify(new(ToastType.Success, "User Added To Your Group!"));
                await OnAssigneeSaved.InvokeAsync();
            }

            _assigneeFound = !_assigneeFound;
            _email = string.Empty;
            await _newAssigneeModal.HideAsync();
        }
    }
}
