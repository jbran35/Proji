using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// A modal to confirm that the user wishes to complete a project (in turn - completing all of its todo items).
    /// </summary>
    public partial class CompleteProjectModal : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Parameter] public Guid ProjectId { get; set; }
        #endregion

        private bool _isCompleteClicked;
        private Modal _completeProjectModal = default!;
        
        public async Task OnShowCompleteProjectModalClick() => await _completeProjectModal.ShowAsync();
        private async Task OnHideCompleteProjectModal() => await _completeProjectModal.HideAsync();

        private async Task OnCompleteProjectClick()
        {
            _isCompleteClicked = true; //Disables button so multiple clicks don't initiate multiple calls.
            await _completeProjectModal.HideAsync();
            var client = ApiClientService.GetClient();

            var response = await client.PatchAsync("api/projects/" + ProjectId.ToString() + "/status", null);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<CompleteProjectResponse>();
            if (result is not null)
            {
                await ProjectStateService.SetProjectTile(result.ProjectTile); // Updating state service to prevent redundant API calls.
            }

            ToastService.Notify(new(ToastType.Success, "Project Successfully Completed!"));
            _isCompleteClicked = false;
        }
    }
}
