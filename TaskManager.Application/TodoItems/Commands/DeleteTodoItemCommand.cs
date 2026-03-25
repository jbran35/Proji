using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Commands
{
    /// <summary>
    /// Represents a request to delete a todo item and remove it from a project.
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="TodoItemId"></param>
    public record DeleteTodoItemCommand(
    Guid UserId,
    Guid TodoItemId

    ) : IRequest<Result>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectTiles(UserId)];
    }
}


