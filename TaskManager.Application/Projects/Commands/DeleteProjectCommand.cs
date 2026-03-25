using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Commands
{
    /// <summary>
    /// Represents a request to delete a user's project and all of its todo items. 
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    public record DeleteProjectCommand(
        Guid UserId,
        Guid ProjectId
    ) : IRequest<Result<DeleteProjectResponse>>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectDetailedViews(UserId, ProjectId), CacheKeys.ProjectTiles(UserId)];
    }
}
