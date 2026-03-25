using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class MyAssignedTodoItems : AppComponentBase, IDisposable
    {
        #region Injections
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Inject] protected AssignedTodoItemsStateService AssignedTodoItemsStateService { get; set; } = default!;
        [Inject] protected SignalRConnectionService SignalRConnectionService { get; set; } = default!;
        #endregion

        #region Setup
        private string _todoItemFilter = string.Empty; //Filter string for searching
        private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
        private List<TodoItemEntry>? _todoItemList;
        private bool _isLoading = true;
        private string _errorMessage = string.Empty;

        private TodoItemDetailsModal _detailsModal = default!;
        private IQueryable<TodoItemEntry> FilteredTodoItems => //Needed for QuickGrid
          (_todoItemList ?? [])
          .Where(t => string.IsNullOrEmpty(_todoItemFilter) ||
                      t.Title.Contains(_todoItemFilter, StringComparison.OrdinalIgnoreCase)).AsQueryable();

        #endregion
        protected override async Task OnInitializedAsync()
        {
            SignalRConnectionService.OnTodoItemUpdated += HandleStateChanged; //Allows for immediate updating if the owner changes the todo item
            await SignalRConnectionService.InitializeConnection();
            await LoadMyTodoItems();
        }

        private void HandleStateChanged()
        {
            InvokeAsync(async () =>
            {
                AssignedTodoItemsStateService.Clear();
                await LoadMyTodoItems();
                StateHasChanged();
            });
        }

        public void Dispose()
        {
            SignalRConnectionService.OnTodoItemUpdated -= HandleStateChanged;
            GC.SuppressFinalize(this);
        }   
        private async Task UpdateCompletionStatus(Guid todoItemId)
        {
            var client = ApiClientService.GetClient();
            var response = await client.PatchAsync($"api/todoitems/{todoItemId}/status", null);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                ToastService.Notify(new(ToastType.Danger, errorMsg));
                return;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<TodoItemEntry>();

                if (!response.IsSuccessStatusCode)
                {
                    ToastService.Notify(new(ToastType.Danger, $"Failed to complete task. Status code: {response.StatusCode}"));
                    return;
                }

                if (result is null)
                {
                    ToastService.Notify(new(ToastType.Danger, "Returned task is null"));
                    return;
                }

                AssignedTodoItemsStateService.AddTodoItem(result);
                ToastService.Notify(new(ToastType.Success, "Task Status Updated!"));
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task OnShowTodoItemDetails(TodoItemEntry todoItem) => await _detailsModal.OnShowDetailsModalClick(todoItem);
        private async Task LoadMyTodoItems()
        {
            _isLoading = true;
            _errorMessage = string.Empty;

            var cachedAssigneedItems = AssignedTodoItemsStateService.GetTodoItems(); //Checks cache before calling API. 

            if (cachedAssigneedItems.Count != 0)
            {
                _todoItemList = cachedAssigneedItems;
                _isLoading = false;
                return;
            }

            try
            {
                var client = ApiClientService.GetClient();
                var response = await client.GetAsync("api/todoitems/MyAssignedTasks");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<TodoItemEntry>>();
                    
                    if (result is not null)
                    {
                        _todoItemList = [.. result.Select(t => new TodoItemEntry
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Description = t.Description ?? string.Empty,
                            ProjectTitle = t.ProjectTitle,
                            OwnerName = t.OwnerName,
                            Priority = t.Priority ?? Domain.Enums.Priority.None,
                            DueDate = t.DueDate ?? null,
                            CreatedOn = t.CreatedOn,
                            Status = t.Status,

                        })];

                        AssignedTodoItemsStateService.SetTodoItemsAsync(_todoItemList);
                    }

                    else
                        _errorMessage = await response.Content.ReadAsStringAsync();
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    _errorMessage = "Not authorized. Please log in.";

                else
                    _errorMessage = $"API returned status code: {response.StatusCode}";
            }

            catch (Exception ex)
            {
                _errorMessage = $"Error: {ex.Message}";
            }

            _isLoading = false;
        }
    }
}
