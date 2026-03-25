using MediatR;

namespace TaskManager.Application.Projects.Events
{
    /// <summary>
    /// An event representing an assigned item being created - used to notify the assigne
    /// so they can see the new item immediately.
    /// </summary>
    /// <param name="AssigneeId"></param>
    public record AssignedTodoItemCreatedEvent(
        Guid? AssigneeId) : INotification;
}
