using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Commands
{
    /// <summary>
    /// A command representing a request to change the status of a Todo Item (i.e., marking it complete/incomplete). 
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="TodoItemId"></param>
    public record UpdateTodoItemStatusCommand(
        Guid UserId,
        Guid TodoItemId
        ) : IRequest<Result<TodoItemEntry>>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectTiles(UserId)];
    }
}
