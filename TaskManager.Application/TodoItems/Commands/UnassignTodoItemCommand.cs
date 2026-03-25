using MediatR;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Commands
{
    /// <summary>
    /// A command representing a request to unassign a todo item from a user. 
    /// Not used currently, but could be if quick assign/unassign functions were desired in the future.
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    /// <param name="TodoItemId"></param>
    public record UnassignTodoItemCommand(

        Guid UserId,
        Guid ProjectId,
        Guid TodoItemId) : IRequest<Result>;
}
