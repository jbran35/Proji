using MediatR;

namespace TaskManager.Application.Projects.Events
{
    /// <summary>
    /// An event representing a project being intentionally marked as complete.
    /// Used to clear the Redis key for the assignee and update their view immediately.
    /// </summary>
    /// <param name="ProjectId"></param>
    public record ProjectCompletedEvent(Guid? ProjectId) : INotification;
}
