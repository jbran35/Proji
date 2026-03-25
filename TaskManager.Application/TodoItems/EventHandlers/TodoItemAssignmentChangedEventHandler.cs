using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.Events;

namespace TaskManager.Application.TodoItems.EventHandlers
{
    /// <summary>
    /// Handles the TodoItemAssignmentChangedEvent so that the old assignee's Redis key is cleared 
    /// and they see the TodoItem removed from their view immediately. 
    /// Not currently used, but could be in the future. 
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="updateNotificationService"></param>
    public class TodoItemAssignmentChangedEventHandler(IDistributedCache cache, ITodoItemUpdateNotificationService updateNotificationService)
        : INotificationHandler<TodoItemAssignmentChangedEvent>
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ITodoItemUpdateNotificationService _updateNotificationService = updateNotificationService; 
        public async Task Handle(TodoItemAssignmentChangedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.OldAssigneeId is not null && notification.OldAssigneeId != Guid.Empty)
            {
                await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(notification.OldAssigneeId.Value), CancellationToken.None);
                await _updateNotificationService.NotifyTodoItemUpdated(notification.OldAssigneeId.Value.ToString());
            }

            if(notification.NewAssigneeId is not null && notification.NewAssigneeId != Guid.Empty && notification.NewAssigneeId != notification.OldAssigneeId)
            {
                await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(notification.NewAssigneeId.Value), CancellationToken.None);
                await _updateNotificationService.NotifyTodoItemUpdated(notification.NewAssigneeId.Value.ToString()); 
            }
        }
    }
}
