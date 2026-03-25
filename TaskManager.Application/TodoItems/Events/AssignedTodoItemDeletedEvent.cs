using MediatR;

namespace TaskManager.Application.TodoItems.Events
{
    /// <summary>
    /// An event created when a todo item that is assigned to another user is deleted.
    /// </summary>
    /// <param name="AssigneeId"></param>
    public record AssignedTodoItemDeletedEvent(
        Guid? AssigneeId) : INotification;
}
