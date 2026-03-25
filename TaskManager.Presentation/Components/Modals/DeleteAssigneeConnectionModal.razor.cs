using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// Confirmation modal through which user's delete/remove assignees from their group. 
    /// </summary>
    public partial class DeleteAssigneeConnectionModal : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected AssigneeListStateService AssigneeListStateService { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Parameter] public required UserConnectionDto Connection { get; set; }
        #endregion

        private Modal _deleteAssigneeModal = default!;
        private bool _isDeleteClicked;
        public async Task OnShowDeleteAssigneeModalClick() => await _deleteAssigneeModal.ShowAsync();
        private void OnHideDeleteAssigneeModalClick() => _deleteAssigneeModal.HideAsync();
        
        private async Task OnDeleteAssignee()
        {
            _isDeleteClicked = true; // Disables the button to prevent multiple calls. 

            var client = ApiClientService.GetClient();
            var response = await client.DeleteAsync($"api/Account/assignees/{Connection.Id}");

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            ToastService.Notify(new(ToastType.Success, "User Removed From Your Group!"));

            //Local State Service adjustments to reduce repetitive API calls. 
            await AssigneeListStateService.RemoveFromCacheAsync(Connection);
            await ProjectStateService.UnassignUserFromTasks(Connection.AssigneeId);

            await _deleteAssigneeModal.HideAsync();
            _isDeleteClicked = false;
        }
    }
}
