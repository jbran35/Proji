using TaskManager.Application.TodoItems.DTOs;

namespace TaskManager.Presentation.Services
{
    /// <summary>
    /// A service to maintain a current state of items assigned to a user in order to reduce redundant API calls.
    /// </summary>
    public class AssignedTodoItemsStateService
    {
        private List<TodoItemEntry> _assignedTodoItemsCache = [];

        public List<TodoItemEntry> GetTodoItems()
        {
            return _assignedTodoItemsCache;
        }

        public void SetTodoItemsAsync(List<TodoItemEntry> todoItems)
        {
            _assignedTodoItemsCache = todoItems;
        }

        public void AddTodoItem(TodoItemEntry newItem)
        {
            var index = _assignedTodoItemsCache.FindIndex(t => t.Id == newItem.Id);

            if(index != -1)
            {
                _assignedTodoItemsCache.RemoveAt(index);
                _assignedTodoItemsCache.Insert(index, newItem);
            }
        }

        public void Clear()
        {
            _assignedTodoItemsCache.Clear();
        }
    }
}
