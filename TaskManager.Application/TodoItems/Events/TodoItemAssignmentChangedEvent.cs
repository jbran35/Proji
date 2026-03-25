using MediatR;

namespace TaskManager.Application.TodoItems.Events
{
    /// <summary>
    /// An event created when the assignee for a todo item is changed.
    /// Not currently used, as this can go through updating a todo item, generally. 
    /// If quick assignment changes are implemented in the future - this could be used.
    /// </summary>
    /// <param name="OldAssigneeId"></param>
    /// <param name="NewAssigneeId"></param>
    public record TodoItemAssignmentChangedEvent(
        Guid? OldAssigneeId,
        Guid? NewAssigneeId) : INotification;

}
