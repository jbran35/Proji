using Microsoft.AspNetCore.Components;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Components.Pages;

namespace TaskManager.Presentation.Components.Tiles
{
    /// <summary>
    /// Tile view of a project that is displayed on the project owner's dashboard. Contains item counts & a progress graph.
    /// </summary>
    public partial class ProjectTile : ComponentBase
    {
        #region Injections & Parameters
        [Inject] protected NavigationManager NavManager { get; set; } = default!; 
        [Parameter] public required ProjectTileDto Project { get; set; }
        #endregion

        public bool MenuIsOpen = false;

        private void NavToProjectDetails() => NavManager.NavigateTo($"/project/{Project.Id}");

        /*private void ToggleTileOptions()
        {
           // _dropdownElement.ToggleAsync();
            StateHasChanged();
        }*/
        
        private double CalculatedProgress
        {
            get
            {
                if (Project.TotalTodoItemCount == 0) return 0;

                return Math.Round((double)Project.CompleteTodoItemCount / Project.TotalTodoItemCount * 100, 2);
            }
        }

        #region Modals & Logic

        private EditProjectModal _editModal = default!;
        private DeleteProjectModal _deleteModal = default!;
        private CompleteProjectModal _completeModal = default!;
        public required EditProject EditProjectForm;


        private async Task OnShowEditProjectModalClick()
        {
            await _editModal.OnShowEditProjectModalClick();
        }

        private async Task OnShowDeleteProjectModalClick()
        {
            await _deleteModal.OnShowDeleteProjectModalClick();
        }

        private async Task OnShowCompleteProjectModalClick()
        {
            await _completeModal.OnShowCompleteProjectModalClick();
        }

        #endregion
    }
}
