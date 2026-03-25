using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TaskManager.Application.Projects.DTOs.Requests;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Presentation.Components.Models;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class NewTodoItem : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected TodoItemDraftStateService TodoItemDraftStateService { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Inject] protected AssigneeListStateService AssigneeListStateService { get; set; } = default!;
        [Parameter] public Guid ProjectId { get; set; }
        [Parameter] public EventCallback OnShowNewAssigneeModalClick { get; set; }
        [Parameter] public EventCallback<Guid> OnRestoreNewTodoItemModal { get; set; }
        #endregion

        private EditContext? _editContext;
        private readonly ValidationModel _validationModel = new();
        private readonly Domain.Enums.Priority[] _priorityOptions = Enum.GetValues<Domain.Enums.Priority>();
        private TodoItemModel Model { get; set; } = new TodoItemModel();
        private List<UserConnectionDto> Assignees { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            _editContext = new EditContext(_validationModel);
            await LoadAssigneesAsync();
        }

        private async Task LoadAssigneesAsync()
        {

            var cachedAssignees = await AssigneeListStateService.GetAssigneesFromCacheAsync(); //Check cache before calling API.
            if(cachedAssignees is not null && cachedAssignees.Count != 0)
            {
                Assignees = cachedAssignees;
                return;
            }

            var client = ApiClientService.GetClient();
            var response = await client.GetAsync($"api/Account/assignees");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<UserConnectionDto>>();
                if (result is not null)
                    Assignees = result;
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

            await OnShowNewAssigneeModalClick.InvokeAsync();
        }

        public async Task CreateTodoItem()
        {
            var isValid = _editContext?.Validate() ?? false;
            if (!isValid) return;

            if (_validationModel.AssigneeId == Guid.Empty)
                _validationModel.AssigneeId = null; //To show explicit unassigned status

            var request = new CreateTodoItemRequest
            {
                Title = _validationModel.Title,
                ProjectId = ProjectId,
                Description = _validationModel.Description,
                AssigneeId = _validationModel.AssigneeId,
                Priority = _validationModel.Priority,
                DueDate = _validationModel.DueDate,
            };

            var client = ApiClientService.GetClient();
            var response = await client.PostAsJsonAsync("api/Projects/" + ProjectId + "/tasks", request);

            if (!response.IsSuccessStatusCode)
            {
                ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<TodoItemEntry>();
            if (result is null) return;

            ToastService.Notify(new(ToastType.Success, "Task Created Successfully!"));
            await ProjectStateService.SetTodoItemInProject(ProjectId, result);
        }

        public void RestoreForm() //Restore if the user clicks into AddNewAssigneeModal
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
