using MediatR;

namespace TaskManager.Application.UserConnections.Events
{
    /// <summary>
    /// An event representing a user deleting an assignee for their group.
    /// </summary>
    /// <param name="AssigneeId"></param>
    public record UserRemovedFromGoupEvent(Guid? AssigneeId) : INotification;
    
}
