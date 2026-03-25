using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.Events;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.EventHandlers
{
    /// <summary>
    /// Handles a ProjectCompletedEvent to reset any assignee's cached assigned items in Redis, and notify them so they 
    /// see the update immediately.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="updateNotificationService"></param>
    /// <param name="projectRepository"></param>
    public class ProjectCompletedEventHandler (IDistributedCache cache, 
        ITodoItemUpdateNotificationService updateNotificationService, IProjectRepository projectRepository)
        : INotificationHandler<ProjectCompletedEvent>
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ITodoItemUpdateNotificationService _updateNotificationService = updateNotificationService;
        private readonly IProjectRepository _projectRepo = projectRepository;
    public async Task Handle(ProjectCompletedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.ProjectId is not null && notification.ProjectId != Guid.Empty)
            {
                var assigneeIds = await _projectRepo.GetProjectIncompleteTodoItemAssigneeIds(notification.ProjectId.Value, cancellationToken);

                foreach (var id in assigneeIds)
                {
                    await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(id), cancellationToken);
                    await _updateNotificationService.NotifyTodoItemUpdated(id.ToString());
                }
            }
        }
    }
}
