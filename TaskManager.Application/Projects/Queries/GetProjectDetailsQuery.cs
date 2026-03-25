using MediatR;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Queries
{
    /// <summary>
    /// A query representing a request to retrieve a project's basic details (no counts or tasks).
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    public record GetProjectDetailsQuery(Guid UserId, Guid ProjectId) : IRequest<Result<ProjectDetailsDto>>;
}
