using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Commands
{
    /// <summary>
    /// Represents a request to create a new project for the user. 
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    public record CreateProjectCommand(
        Guid UserId,
        string Title,
        string? Description) : IRequest<Result<ProjectTileDto>>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.ProjectTiles(UserId)];
    }
}
