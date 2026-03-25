using Microsoft.AspNetCore.Components;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class MyGroup : ComponentBase, IDisposable
    {
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Inject] protected AssigneeListStateService AssigneeListStateService { get; set; } = default!;

        private string _assigneeFilter = string.Empty;
        private NewAssigneeModal _newAssigneeModal = default!;
        private bool _isLoading = true;
        private List<UserConnectionDto> _assignees = [];

        private IEnumerable<UserConnectionDto> FilteredAssignees =>
        string.IsNullOrWhiteSpace(_assigneeFilter)
            ? _assignees
            : _assignees.Where(a =>
                (a.AssigneeName.Contains(_assigneeFilter, StringComparison.OrdinalIgnoreCase))).AsQueryable();


        protected override async Task OnInitializedAsync()
        {
            AssigneeListStateService.OnChange += RefreshState;
            await LoadAssigneesAsync();
            _isLoading = false;
        }

        private async void RefreshState()
        {
            await InvokeAsync(async () =>
            {
                var assignees = await AssigneeListStateService.GetAssigneesFromCacheAsync();
                if (assignees is not null)
                {
                    _assignees = assignees;
                    StateHasChanged();
                }
            });
        }

        public void Dispose()
        {
            AssigneeListStateService.OnChange -= RefreshState;
            GC.SuppressFinalize(this);
        }

        private async Task LoadAssigneesAsync()
        {
            var cachedAssignees = await AssigneeListStateService.GetAssigneesFromCacheAsync();
            if (cachedAssignees is not null)
            {
                _assignees = cachedAssignees;
                _isLoading = false;
                return;
            }

            try
            {
                var client = ApiClientService.GetClient();
                
                var response = await client.GetAsync("api/Account/Assignees");
                if (!response.IsSuccessStatusCode) return; 

                var result = await response.Content.ReadFromJsonAsync<List<UserConnectionDto>>();
                if (result is not null)
                {
                    await AssigneeListStateService.SetAssigneesInCacheAsync(result);
                    var assignees = result;
                    _assignees = assignees;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            finally
            {
                _isLoading = false;
            }
        }
        private async Task OnShowAssigneeModalAsync() => await _newAssigneeModal.OnShowNewAssigneeModal();
    }
}
