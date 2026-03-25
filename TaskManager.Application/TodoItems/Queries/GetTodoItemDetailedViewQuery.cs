using MediatR;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.TodoItems.Queries
{
    /// <summary>
    /// A query representing a request to retrieve a detailed view of a TodoItem.
    /// </summary>
    public record GetTodoItemDetailedViewQuery : IRequest<Result<TodoItemEntry>>
    {
        public required Guid UserId { get; init; }
        public required Guid TodoItemId { get; init; }
        public Guid ProjectId { get; set; }

    }
}
