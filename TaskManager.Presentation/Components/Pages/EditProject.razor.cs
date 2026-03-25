using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.DTOs.Requests;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    /// <summary>
    /// Corresponding page for the EditProjectModal
    /// </summary>
    public partial class EditProject : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Parameter] public Guid ProjectId { get; set; }
        #endregion

        private readonly ProjectFormModel _model = new();
        private EditContext? _editContext;

        public class ProjectFormModel
        {
            [Required(ErrorMessage = "Name required.")]
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        protected async override Task OnInitializedAsync() => _editContext = new EditContext(_model);
        protected override async Task OnParametersSetAsync() => await LoadProject(); // Waits for ProjectId

        public async Task LoadProject()
        {
            var cachedDetails = await ProjectStateService.GetProjectBasicDetails(ProjectId); // Check State Service first, then all API if needed.

            if (cachedDetails is not null && cachedDetails.Description is not null)
            {
                _model.Title = cachedDetails.Title;
                _model.Description = cachedDetails.Description ?? string.Empty;
                return;
            }

            var client = ApiClientService.GetClient();
            var response = await client.GetAsync("api/projects/" + ProjectId.ToString() + "/details");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProjectDetailsDto>();

                if (result is not null)
                {
                    await ProjectStateService.SetProjectBasicDetails(result);

                    _model.Title = result.Title;

                    if (result.Description != null) _model.Description = result.Description;

                    else _model.Description = "";
                }
            }
        }

        public async Task UpdateProject()
        {
            var isValid = _editContext?.Validate() ?? false;
            if (!isValid) return;

            var request = new UpdateProjectRequest
            {
                Title = _model.Title,
                Description = _model.Description
            };

            var client = ApiClientService.GetClient();
            var response = await client.PatchAsJsonAsync("api/projects/" + ProjectId.ToString(), request);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProjectDetailsDto>();

                if (result is not null)
                    await ProjectStateService.SetProjectBasicDetails(result);

                ToastService.Notify(new(ToastType.Success, "Project Updated Successfully!"));
            }
        }
    }
}
