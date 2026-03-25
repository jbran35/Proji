using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Presentation.Components.Models;
using TaskManager.Presentation.Components.Pages;
using TaskManager.Presentation.Enums;

namespace TaskManager.Presentation.Components.Modals
{
    /// <summary>
    /// Allows the user to edit the details for a todo item, as well as opening the NewAssigneeModal to add a new assignee to their 
    /// group/todo item. 
    /// </summary>
    public partial class EditTodoItemModal : AppComponentBase
    {
        #region Parameters
        [Parameter] public Guid ProjectId { get; set; }
        [Parameter] public EventCallback OnRestoreEditModalClick { get; set; }
        [Parameter] public EventCallback<NewAssigneeCallingOriginEnum> OnShowNewAssigneeModalClick { get; set; }
        #endregion

        private Modal _editTodoItemModal = default!;
        private EditTodoItem _editTodoItemForm = default!;
        private bool _isUpdateClicked;
        private TodoItemEntry TodoItem { get; set; } = new TodoItemEntry();
        public TodoItemModel Model = new();

        public async Task OnShowEditTodoItemModalClick(TodoItemEntry todoItem)
        {
            TodoItem = todoItem;
            await _editTodoItemModal.ShowAsync();
        }

        public async Task OnHideEditTodoItemModalClick() => await _editTodoItemModal.HideAsync();
        private async Task OnUpdateTodoItemModalClick()
        {
            _isUpdateClicked = true;
            await _editTodoItemForm.UpdateTodoItem();
            await _editTodoItemModal.HideAsync();
            _isUpdateClicked = false;
        }

        public async Task OnShowNewAssigneeModal(TodoItemModel passedModel)
        {
            Model = passedModel;
            await OnShowNewAssigneeModalClick.InvokeAsync(NewAssigneeCallingOriginEnum.EditTodoItem);
            await _editTodoItemModal.HideAsync();
        }

        public async Task RestoreModal() // Used to restore details entered after the user clicks into the AddNewAssigneeModal
        {
            await _editTodoItemModal.ShowAsync();
            _editTodoItemForm.RestoreForm();
        }
    }
}
