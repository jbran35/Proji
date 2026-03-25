using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Commands
{
    /// <summary>
    /// Represents a request to update the details for a project (i.e., Title or Description)
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    /// <param name="NewTitle"></param>
    /// <param name="NewDescription"></param>
    public record UpdateProjectCommand(
       Guid UserId,
       Guid ProjectId,
       string? NewTitle,
       string? NewDescription
    ) : IRequest<Result<ProjectDetailsDto>>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectTiles(UserId), CacheKeys.ProjectDetailedViews(UserId, ProjectId)];
    }
}
