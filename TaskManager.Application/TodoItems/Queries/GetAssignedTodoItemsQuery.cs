using MediatR;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Queries
{
    /// <summary>
    /// Query representing a request to retrieve a user's assigned todo items. 
    /// </summary>
    /// <param name="UserId"></param>
    public record GetAssignedTodoItemsQuery(Guid UserId) : IRequest<Result<List<TodoItemEntry>>>; 
}
