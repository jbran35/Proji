using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// Confirmation modal for deleting a todo item/removing it from a project.
    /// </summary>
    public partial class DeleteTodoItemModal : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Parameter] public Guid TodoItemId { get; set; }
        [Parameter] public Guid ProjectId { get; set; }
        #endregion

        private bool _isDeleteClicked;
        private Modal _deleteModal = default!;

        public async Task OnShowDeleteTodoItemModalClick(TodoItemEntry todoItem)
        {
            TodoItemId = todoItem.Id;
            await _deleteModal.ShowAsync();
        }

        private async Task OnHideDeleteTodoItemModalClick() => await _deleteModal.HideAsync();

        private async Task OnDeleteTodoItemClick()
        {
            _isDeleteClicked = true; // Prevents multiple clicks from initiating duplicate calls.
            var client = ApiClientService.GetClient();
            var response = await client.DeleteAsync($"api/todoitems/{TodoItemId}");

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Success, "Task Deleted Successfully!"));
                await ProjectStateService.RemoveTodoItem(ProjectId, TodoItemId); // Updating State Service for easy UI updating.
                await _deleteModal.HideAsync();
            }

            _isDeleteClicked = false;
        }
    }
}
