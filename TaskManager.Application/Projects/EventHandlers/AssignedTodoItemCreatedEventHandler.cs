using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.Events;

namespace TaskManager.Application.Projects.EventHandlers
{
    /// <summary>
    /// Handles AssignedTodoItemCreatedEvent to reset the assignee's cached assigned items in Redis, and notify them so they 
    /// see the new TodoItem immediately.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="updateNotificationService"></param>
    public class AssignedTodoItemCreatedEventHandler(IDistributedCache cache, 
        ITodoItemUpdateNotificationService updateNotificationService) : INotificationHandler<AssignedTodoItemCreatedEvent>
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ITodoItemUpdateNotificationService _updateNotificationService = updateNotificationService;
        public async Task Handle(AssignedTodoItemCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.AssigneeId is not null && notification.AssigneeId != Guid.Empty)
            {
                await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(notification.AssigneeId.Value), cancellationToken);
                await _updateNotificationService.NotifyTodoItemUpdated(notification.AssigneeId.Value.ToString());
            }
        }
    }
}
