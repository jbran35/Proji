using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.UserConnections.Events;

namespace TaskManager.Application.UserConnections.EventHandlers
{
    /// <summary>
    /// Handles the UserRemovedFromGroupEvent so that
    /// the removed user no longer sees todo itmes that were assigned to them. 
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="updateNotificationService"></param>
    public class UserRemovedFromGroupEventHandler(IDistributedCache cache, ITodoItemUpdateNotificationService updateNotificationService)
        : INotificationHandler<UserRemovedFromGoupEvent>
    {
        private readonly IDistributedCache _cache = cache; 
        private readonly ITodoItemUpdateNotificationService _updateNotificationService = updateNotificationService;
        public async Task Handle(UserRemovedFromGoupEvent notification, CancellationToken cancellationToken)
        {
            if (notification.AssigneeId is not null && notification.AssigneeId != Guid.Empty)
            {
                await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(notification.AssigneeId.Value), cancellationToken);
                await _updateNotificationService.NotifyTodoItemUpdated(notification.AssigneeId.Value.ToString());
            }
        }
    }
}
