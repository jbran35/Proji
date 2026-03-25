using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.DTOs.Requests;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class NewProject : AppComponentBase
    {
        #region Injections
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        #endregion

        private readonly ProjectFormModel _model = new();
        private EditContext? _editContext;

        public class ProjectFormModel //Used for validation of inputs
        {
            [Required(ErrorMessage = "Title required.")]
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        protected override Task OnInitializedAsync()
        {
            _editContext = new EditContext(_model);
            return base.OnInitializedAsync();
        }

        public async Task CreateProject()
        {
            var isValid = _editContext?.Validate() ?? false;
            if (!isValid) return;

            var request = new CreateProjectRequest
            {
                Title = _model.Name,
                Description = _model.Description
            };

            var client = ApiClientService.GetClient();
            var response = await client.PostAsJsonAsync("api/projects/", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = await response.Content.ReadAsStringAsync();
                ToastService.Notify(new(ToastType.Danger, errorResult));
            }

            else if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProjectTileDto>();
                if (result is not null)
                {
                    await ProjectStateService.SetProjectTile(result);
                }
            }
        }
    }
}
