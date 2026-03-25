using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Common;

namespace TaskManager.Application.UserConnections.Commands
{
    /// <summary>
    /// A command representing a request to remove a new UserConnection
    /// (i.e., to remove an assignee from a user's group). 
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="ConnectionId"></param>
    public record DeleteUserConnectionCommand(
       Guid UserId,
       Guid ConnectionId) : IRequest<Result>, ICacheInvalidator
    {
        public string[] Keys => [CacheKeys.Connections(UserId)];
};
}
