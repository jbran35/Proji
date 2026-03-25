using MediatR;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.UserConnections.Queries
{
    /// <summary>
    /// A query representing a request to retrieve all of a user's active connections with other users.
    /// </summary>
    /// <param name="UserId"></param>
    public record GetActiveUserConnectionsQuery(Guid UserId) : IRequest<Result<IEnumerable<UserConnectionDto>>>; 
}
