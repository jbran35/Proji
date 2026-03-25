using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Commands
{
    /// <summary>
    /// Represents a request to update the status for a project and all of its incomplete todo items to complete.
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    public record CompleteProjectCommand(
        Guid UserId,
        Guid ProjectId
    ) : IRequest<Result<CompleteProjectResponse>>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectDetailedViews(UserId, ProjectId), CacheKeys.ProjectTiles(UserId)];
    }
}
