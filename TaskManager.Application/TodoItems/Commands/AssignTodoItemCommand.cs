using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Commands
{
    /// <summary>
    /// A command representing a request to assign a todo item to a user. 
    /// Not currently used.
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    /// <param name="TodoItemId"></param>
    /// <param name="AssigneeId"></param>
    public record AssignTodoItemCommand(
        
        Guid UserId,
        Guid ProjectId,
        Guid TodoItemId,
        Guid AssigneeId) : IRequest<Result>, ICacheInvalidator
    {
        public string[] Keys => [.. GetKeys()];
        private IEnumerable<string> GetKeys()
        {
            yield return CacheKeys.ProjectDetailedViews(UserId, ProjectId);

            if(AssigneeId != Guid.Empty && AssigneeId != UserId)
            {
                yield return CacheKeys.AssignedTodoItems(AssigneeId); 
            }
        }
    }
}
