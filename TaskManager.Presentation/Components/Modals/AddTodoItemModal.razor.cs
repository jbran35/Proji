using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using TaskManager.Presentation.Components.Models;
using TaskManager.Presentation.Components.Pages;
using TaskManager.Presentation.Enums;

namespace TaskManager.Presentation.Components.Modals
{
    public partial class AddTodoItemModal : ComponentBase
    {
        [Parameter] public Guid ProjectId { get; set; }
        [Parameter] public EventCallback<NewAssigneeCallingOriginEnum> OnShowNewAssigneeModalClick { get; set; }

        private Modal _addTodoItemModal = default!;
        private NewTodoItem _addTodoItemForm = default!;
        private readonly TodoItemModel _model = new();
        private bool _isSaveClicked;
        public async Task OnShowAddTodoItemModalClick(Guid projectId)
        {
            _model.ProjectId = projectId;
            await _addTodoItemModal.ShowAsync();
        }

        public async Task OnHideAddTodoItemModalClick() => await _addTodoItemModal.HideAsync();

        private async Task OnCreateTodoItemClick()
        {
            _isSaveClicked = true;
            await _addTodoItemForm.CreateTodoItem();
            await _addTodoItemModal.HideAsync();
            _isSaveClicked = false;
        }

        public async Task OnShowNewAssigneeModal()
        {
            var origin = NewAssigneeCallingOriginEnum.AddTodoItem;
            await OnShowNewAssigneeModalClick.InvokeAsync(origin); //Origin is used to restore the proper modal after AddNewAssigneeModal is closed
            await _addTodoItemModal.HideAsync();
        }

        public async Task RestoreModal()
        {
            await _addTodoItemModal.ShowAsync();
            _addTodoItemForm.RestoreForm();
        }
    }
}
