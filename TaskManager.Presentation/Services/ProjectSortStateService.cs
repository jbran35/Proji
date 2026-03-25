using TaskManager.Presentation.Enums;

namespace TaskManager.Presentation.Services
{
    /// <summary>
    /// A service to keep track of a user's sort selection.
    /// </summary>
    public class ProjectSortStateService
    {
        private SortOption _selectedSort = SortOption.DateDesc;

        public SortOption? GetSortingMethod()
        {
            return _selectedSort;
        }

        public void SetSortingMethod(SortOption option)
        {
            _selectedSort = option;
        }
    }
}
