using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Presentation.Components.Models;

namespace TaskManager.Presentation.Services
{
    /// <summary>
    /// Maintains a drafted new or edited TodoItem, so that the entered/existing details are restored when a user clicks
    /// into the AddNewAssignee modal and close out of that modal. 
    /// </summary>
    public class TodoItemDraftStateService
    {
        private TodoItemModel _model = new();

        public TodoItemModel GetModelFromCache()
        {
            return _model;
        }

        public void SetModelInCache(TodoItemModel model)
        {
            _model = model;
        }

        public void SetAssigneeInModel(UserConnectionDto newConnection)
        {
            _model.AssigneeId = newConnection.AssigneeId;
        }
    }
}
