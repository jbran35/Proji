using MediatR;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Projects.Queries
{
    /// <summary>
    /// A query representing a request to retrieve all of a user's projects in tile form - for their dashboard/MyProjects page.
    /// </summary>
    /// <param name="UserId"></param>
    public record GetUserProjectsQuery(Guid UserId) : IRequest<Result<List<ProjectTileDto>>>;
}