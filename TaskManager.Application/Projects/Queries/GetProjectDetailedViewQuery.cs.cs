using MediatR;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Queries
{
    /// <summary>
    /// A query representing a request to get a project's details and all of its todo items.
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ProjectId"></param>
    public record GetProjectDetailedViewQuery(Guid UserId, Guid ProjectId) : IRequest<Result<ProjectDetailedViewDto>>;
}
