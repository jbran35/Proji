using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Enums;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class ProjectDetailedView : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected ProjectApiService ProjectApiService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Inject] protected IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Inject] protected SignalRConnectionService SignalRConnectionService { get; set; } = default!;
        [Inject] protected ProjectSortStateService ProjectSortStateService { get; set; } = default!;

        [Parameter] public Guid ProjectId { get; set; }
        #endregion

        private Func<Task>? _reopenPreviousModal;
        protected override async Task OnInitializedAsync()
        {
            SignalRConnectionService.OnTodoItemUpdated += HandleStateChanged; //Lets the project owner see updates immediately if assignees mark todo items as complete
            ProjectStateService.OnChange += RefreshState;

            await SignalRConnectionService.InitializeConnection();
            await LoadProject();
            RecalculateProgress();
        }

        //Check that the project is in cache > If TodoItemList is null - it hasn't been pulled from API
        //> If Empty - has been pulled but there are no TodoItems.
        private async Task LoadProject()
        {
            _projectDetails = await ProjectStateService.GetProjectDetails(ProjectId);

            if (_projectDetails is not null)
            {
                RefreshProjectDetails(_projectDetails);
                _isLoading = false;
                return;
            }

            try
            {
                _projectDetails = await ProjectApiService.GetProjectAsync(ProjectId);
                if (_projectDetails is not null)
                {
                    await ProjectStateService.SetAllProjectDetails(_projectDetails);
                    RefreshProjectDetails(_projectDetails);
                    RecalculateProgress();
                }
            }

            catch (ArgumentNullException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "ProjectId Needed To Pull Project Details");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (NullReferenceException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Unexpected Error Retrieving Project");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (UnauthorizedAccessException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Not authorized. Please log in.");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                ToastService.Notify(new ToastMessage(ToastType.Danger, _errorMessage));
            }

            finally
            {
                _isLoading = false;
            }
        }

        private async Task OnAssigneeModalClosed() //Restores either EditTodoItemModal or AddTodoItemModal
        {
            if (_reopenPreviousModal is not null)
            {
                await _reopenPreviousModal.Invoke();
                _reopenPreviousModal = null;
            }
        }

        private async Task OpenNewAssigneeModalFromAdd()
        {
            await _addTodoItemModal.OnHideAddTodoItemModalClick();
            _reopenPreviousModal = async () => await _addTodoItemModal.RestoreModal();
            await _newAssigneeModal.OnShowNewAssigneeModal();
        }

        private async Task OpenNewAssigneeModalFromEdit()
        {
            await _editTodoItemModal.OnHideEditTodoItemModalClick();
            _reopenPreviousModal = async () => await _editTodoItemModal.RestoreModal();
            await _newAssigneeModal.OnShowNewAssigneeModal();
        }

        private void RecalculateProgress()
        {
            if (_todoItemList is null || _todoItemList.Count == 0)
            {
                _totalTodoItems = 0;
                _completeTodoItems = 0;
                _percentComplete = 0.0;
                return;
            }

            _totalTodoItems = _todoItemList.Count;
            _completeTodoItems = _todoItemList.Count(t => t.Status == Status.Complete);

            if (_totalTodoItems > 0)
                _percentComplete = Math.Round(((double)_completeTodoItems / _totalTodoItems) * 100.00, 2);

            else
                _percentComplete = 0.0;
        }


        private async Task RefreshFromCache() 
        {
            var details = await ProjectStateService.GetProjectDetails(ProjectId);
            if (details is not null)
                RefreshProjectDetails(details);
        }

        private string _projectTitle = string.Empty;
        private string _projectDescription = string.Empty;
        private int _totalTodoItems;
        private int _completeTodoItems;
        private double _percentComplete;
        private string _todoItemFilter = string.Empty;
        private bool _isLoading = true;
        private string _errorMessage = string.Empty;
        private ProjectDetailedViewDto? _projectDetails;
        private List<TodoItemEntry>? _todoItemList;


        //IQueryable needed for QuickGrid todo item list
        private IQueryable<TodoItemEntry> FilteredTodoItems =>
           (_todoItemList ?? [])
           .Where(t => string.IsNullOrEmpty(_todoItemFilter) || 
                       t.Title.Contains(_todoItemFilter, StringComparison.OrdinalIgnoreCase)).AsQueryable();

        public async void RefreshState()
        {
            await InvokeAsync(async () =>
            {
                var project = await ProjectStateService.GetProjectDetails(ProjectId);

                if (project is not null)
                {
                    RefreshProjectDetails(project);
                    StateHasChanged();
                }
            });
        }
        private void HandleStateChanged()
        {
            InvokeAsync(async () =>
            {
                await ProjectStateService.ClearProjectDetails(ProjectId);
                await ProjectStateService.ClearProjectTiles();
                await LoadProject();
            });
        }

        public void Dispose()
        {
            SignalRConnectionService.OnTodoItemUpdated -= HandleStateChanged;
            ProjectStateService.OnChange -= RefreshState;
        }

        private async Task UpdateTodoItemStatus(Guid todoItemId)
        {
            try
            {
                var updatedTodoItem = await ProjectApiService.UpdateTodoItemStatus(todoItemId);
                if (updatedTodoItem is not null)
                {
                    await ProjectStateService.UpdateTodoItemStatus(ProjectId, updatedTodoItem.Id);
                    await RefreshFromCache();
                    RecalculateProgress();
                }
            }

            catch (ArgumentNullException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Task ID Needed To Update Its Status");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (NullReferenceException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Unexpected Error Updating Task Status");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (UnauthorizedAccessException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Not authorized. Please log in.");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/myprojects");
            }

            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
            }
        }

        private void RefreshProjectDetails(ProjectDetailedViewDto newDetails)
        {
            _projectTitle = newDetails.Title;
            _projectDescription = newDetails.Description ?? string.Empty;
            _totalTodoItems = newDetails.TotalTodoItemCount;
            _completeTodoItems = newDetails.CompleteTodoItemCount;
            _percentComplete = _totalTodoItems > 0
                ? Math.Round((double)_completeTodoItems / _totalTodoItems * 100, 2)
                : 0;

            _todoItemList = [.. newDetails.TodoItems];
        }

        #region Modal & Logic
        private DeleteTodoItemModal _deleteTodoModal = default!;
        private TodoItemDetailsModal _detailsModal = default!;
        private EditTodoItemModal _editTodoItemModal = default!;
        private AddTodoItemModal _addTodoItemModal = default!;
        private NewAssigneeModal _newAssigneeModal = default!;
        private EditProjectModal _editProjectModal = default!;

        private async Task OnShowEditProjectModalClick() => await _editProjectModal.OnShowEditProjectModalClick();
        private async Task OnShowEditModalClick(TodoItemEntry todoItem) => await _editTodoItemModal.OnShowEditTodoItemModalClick(todoItem);
        private async Task OnEditFromDetailsClick(TodoItemEntry todoItem) => await _editTodoItemModal.OnShowEditTodoItemModalClick(todoItem);
        private async Task OnAddTaskModalClick() => await _addTodoItemModal.OnShowAddTodoItemModalClick(ProjectId);
        private async Task OnShowDetailsModalClick(TodoItemEntry todoItem) => await _detailsModal.OnShowDetailsModalClick(todoItem);
        private async Task OnShowDeleteModalClick(TodoItemEntry todoItem) => await _deleteTodoModal.OnShowDeleteTodoItemModalClick(todoItem);
        #endregion
    }
}
