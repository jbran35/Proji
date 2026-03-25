using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Presentation.Components.Modals;
using TaskManager.Presentation.Enums;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class MyProjects : AppComponentBase, IDisposable
    {
        #region Injections
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Inject] protected ProjectStateService ProjectStateService { get; set; } = default!;
        [Inject] protected ProjectSortStateService ProjectSortStateService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationProvider { get; set; } = default!;
        [Inject] protected ProjectApiService ProjectApiService { get; set; } = default!;
        [Inject] protected SignalRConnectionService SignalRConnectionService { get; set; } = default!;
        [Inject] protected WelcomedService WelcomedService { get; set; } = default!;
        [Inject] protected new ILogger<MyProjects> Logger { get; set; } = default!; 

        #endregion

        private bool _isLoading = true;
        private bool _hasLoaded;
        private string _errorMessage = string.Empty;
        private SortOption _currentSort;


        protected override async Task OnInitializedAsync()
        {
            ProjectStateService.OnChange += RefreshState;
            SignalRConnectionService.OnTodoItemUpdated += HandleStateChanged; //Lets the project owner see updates immediately if assignees mark todo items as complete


            var cachedSort = ProjectSortStateService.GetSortingMethod();
            if (cachedSort is not null)
                _currentSort = (SortOption)cachedSort;
            else
                _currentSort = SortOption.DateDesc;

            var authState = await AuthenticationProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
                await LoadProjects();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                
                var isConnected = await SignalRConnectionService.InitializeConnection();
                if (!isConnected){
                    NavManager.NavigateTo("/login");
                }
            }
        }

        private void HandleStateChanged()
        {
            InvokeAsync(async () =>
            {
                _hasLoaded = false;
                await ProjectStateService.ClearProjectTiles(); //Pings ProjectStateService.OnChange
            });
        }


        private async Task LoadProjects()
        {
            if (_hasLoaded) return;
            _hasLoaded = true;

            //If cache is null or empty > Call API to get projects > Save projects to cache
            // Add to ProjectsList and Projects IQueryable for display.
            try
            {
                var projTiles = await ProjectStateService.GetUserProjectTiles(); 
                if (projTiles is not null && projTiles.Count != 0)
                {
                    _projectsList = projTiles;
                    _isLoading = false;
                    return;
                }

                _projectsList = await ProjectApiService.GetMyProjectsAsync();
                await ProjectStateService.SetProjectTiles(_projectsList);

                if (WelcomedService.GetWelcomedStatus() == false)
                {
                    ToastService.Notify(new(ToastType.Success, "Welcome!"));
                    WelcomedService.MarkWelcomed();
                }
            }

            catch (NullReferenceException ex)
            {
                _errorMessage = ex.Message;
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/login");
            }

            catch (UnauthorizedAccessException ex)
            {
                _errorMessage = ex.Message;
                Logger.LogError(ex, "Failed To Load Projects");
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
                NavManager.NavigateTo("/login");
            }

            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed To Load Projects");
                _errorMessage = ex.Message;
                ToastService.Notify(new(ToastType.Danger, _errorMessage));
            }

            finally
            {
                _isLoading = false;
            }
        }
        public void Dispose()
        {
            ProjectStateService.OnChange -= RefreshState;
            GC.SuppressFinalize(this);
        }

        private async void RefreshState()
        {
            await InvokeAsync(async () =>
            {
                var projectTiles = await ProjectStateService.GetUserProjectTiles();
                if (projectTiles is not null)
                {
                    _projectsList = projectTiles;
                    StateHasChanged();
                }

                else
                {
                    await LoadProjects();
                    StateHasChanged();
                }
            });
        }

        private List<ProjectTileDto> _projectsList = [];

        private IEnumerable<ProjectTileDto> FilteredProjects =>
           ApplySorting(_projectsList
               .Where(m => string.IsNullOrEmpty(_projectFilter)
               || m.Title.Contains(_projectFilter, StringComparison.OrdinalIgnoreCase)));

        private string _projectFilter = string.Empty;

        private void UpdateSort(SortOption option)
        {
            _currentSort = option;
            ProjectSortStateService.SetSortingMethod(option);
            StateHasChanged();
        }

        private IEnumerable<ProjectTileDto> ApplySorting(IEnumerable <ProjectTileDto> query)
        {
            return _currentSort switch
            {
                SortOption.NameAsc => query.OrderBy(p => p.Title),
                SortOption.NameDesc => query.OrderByDescending(p => p.Title),

                SortOption.ProgressAsc => query.OrderBy(p => p.TotalTodoItemCount != 0.00 ? Math.Round((double)p.CompleteTodoItemCount / p.TotalTodoItemCount * 100, 2) : 0.00),
                SortOption.ProgressDesc => query.OrderByDescending(p => p.TotalTodoItemCount != 0 ? Math.Round((double)p.CompleteTodoItemCount / p.TotalTodoItemCount * 100, 2) : 0.00),

                SortOption.DateDesc => query.OrderBy(p => p.CreatedOn),
                SortOption.DateAsc => query.OrderByDescending(p => p.CreatedOn),
                _ => query
            };
        }

        private AddProjectModal _addModal = default!;
        private async Task OpenAddProjectModal()
        {
            await _addModal.OnShowAddProjectModalClick();
        }
    }
}
