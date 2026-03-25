using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.DTOs.Requests;
using TaskManager.Application.TodoItems.DTOs.Responses;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Presentation.Components.Models;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    /// <summary>
    /// Corresponding page for the EditTodoItemModal
    /// </summary>
    public partial class EditTodoItem : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Inject] protected TodoItemDraftStateService TodoItemDraftStateService { get; set; } = default!;
        [Inject] protected SignalRConnectionService SignalRConnectionService { get; set; } = default!;
        [Parameter] public required TodoItemEntry TodoItem { get; set; }
        [Parameter] public EventCallback<TodoItemModel> OnShowNewAssigneeModalClick { get; set; }
        [Parameter] public EventCallback<Guid> OnRestoreEditTodoItemModal { get; set; }
        [Parameter] public Guid ProjectId { get; set; }

        #endregion

        public Guid? AssigneeId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        private List<UserConnectionDto> _assignees = [];

        private readonly Domain.Enums.Priority[] _priorityOptions = Enum.GetValues<Domain.Enums.Priority>();

        private EditContext? _editContext;
        private readonly ValidationModel _validationModel = new();
        private TodoItemModel Model { get; set; } = new TodoItemModel();


        public void EditTodoItemFromDetails(TodoItemEntry todoItem) 
        {
            _validationModel.Title = todoItem.Title;
            _validationModel.Description = todoItem.Description ?? string.Empty;
            _validationModel.AssigneeId = todoItem.AssigneeId;
            _validationModel.Priority = todoItem.Priority ?? Domain.Enums.Priority.None;
            _validationModel.DueDate = todoItem.DueDate;
        }

        protected override async Task OnInitializedAsync()
        {
            _validationModel.Title = TodoItem.Title;
            _validationModel.Description = TodoItem.Description ?? string.Empty;
            _validationModel.AssigneeId = TodoItem.AssigneeId;
            _validationModel.Priority = TodoItem.Priority ?? Domain.Enums.Priority.None;
            _validationModel.DueDate = TodoItem.DueDate;
            _editContext = new EditContext(_validationModel);
            await LoadAssigneesAsync();
        }

        private async Task LoadAssigneesAsync()
        {
            var client = ApiClientService.GetClient();
            
            var response = await client.GetAsync($"api/Account/assignees");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<UserConnectionDto>>();

                if (result is not null)
                    _assignees = result;
            }
        }

        private async Task OnShowNewAssigneeModal()
        {
            Model.Title = _validationModel.Title;
            Model.Description = _validationModel.Description;
            Model.AssigneeId = _validationModel.AssigneeId;
            Model.ProjectId = ProjectId;
            Model.Priority = _validationModel.Priority;
            Model.DueDate = _validationModel.DueDate;

            TodoItemDraftStateService.SetModelInCache(Model);

            await OnShowNewAssigneeModalClick.InvokeAsync(Model); //Sends Model for restoration purposes once the user returns.
        }

        public async Task LoadTodoItem() //Because the TodoItemEntry suffices for the detailed and list views - we can assume the item is in the cache already. 
        {
            var cachedDetails = await ProjectStateService.GetTodoItem(ProjectId, TodoItem.Id); 
            if (cachedDetails is not null)
            {
                _validationModel.Title = cachedDetails.Title;
                _validationModel.Description = cachedDetails.Description ?? string.Empty;

                if (cachedDetails.AssigneeId is not null && cachedDetails.AssigneeId != Guid.Empty)
                {
                    var assignee = _assignees.FirstOrDefault(a => a.AssigneeId == cachedDetails.AssigneeId);


                    if (assignee is not null)
                        _validationModel.AssigneeId = assignee.AssigneeId;
                }

                _validationModel.AssigneeId = cachedDetails.AssigneeId;
                _validationModel.Priority = cachedDetails.Priority ?? Domain.Enums.Priority.None;
                _validationModel.DueDate = cachedDetails.DueDate;

                return;
            }

            try //Can be used if TodoItem is ever expanded so TodoItemEntry is a slimmed down version of it. 
            {
                var client = ApiClientService.GetClient();
                var response = await client.GetFromJsonAsync<GetTodoItemDetailedViewResponse>($"api/todoItems/{TodoItem.Id}");

                if (response?.TodoItemDetails is null)
                    return;

                var todoItem = response.TodoItemDetails;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task UpdateTodoItem()
        {
            var isValid = _editContext?.Validate() ?? false;
            if (!isValid) return;

            if (_validationModel.AssigneeId == Guid.Empty)
                _validationModel.AssigneeId = null;

            var request = new UpdateTodoItemRequest
            {
                ProjectId = ProjectId,
                Title = _validationModel.Title,
                Description = _validationModel.Description,
                AssigneeId = _validationModel.AssigneeId,
                Priority = _validationModel.Priority,
                DueDate = _validationModel.DueDate
            };

            var client = ApiClientService.GetClient();
            var response = await client.PatchAsJsonAsync($"api/todoItems/{TodoItem.Id}", request);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<TodoItemEntry>();
            if (result is not null)
            {
                await ProjectStateService.SetTodoItemInProject(ProjectId, result);
                ToastService.Notify(new(ToastType.Success, "Task Updated Successfully!"));
            }
        }

        public void RestoreForm() //To restore the modal's details if the user clicks into AddNewAssigneeModal.
        {
            var model = TodoItemDraftStateService.GetModelFromCache();

            _validationModel.Title = model.Title ?? string.Empty;
            _validationModel.Description = model.Description ?? string.Empty;
            _validationModel.AssigneeId = model.AssigneeId ?? Guid.Empty;
            _validationModel.Priority = model.Priority ?? Domain.Enums.Priority.None;
            _validationModel.DueDate = model.DueDate ?? null;

            _editContext = new EditContext(_validationModel);
            StateHasChanged();
        }
    }
}
