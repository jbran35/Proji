using MediatR;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Domain.Common;

namespace TaskManager.Application.UserConnections.Commands
{
    /// <summary>
    /// A command representing a request to create a new UserConnection
    /// (i.e., to add a new assignee to a user's group). 
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="AssigneeId"></param>
    public record CreateUserConnectionCommand(
        Guid UserId, 
        Guid AssigneeId
        ) : IRequest<Result<UserConnectionDto>>;
}
