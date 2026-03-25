using MediatR;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Users.Commands
{
    /// <summary>
    /// Represents a request for a user to log in.
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="Password"></param>
    public record LoginUserCommand(
        string UserName, 
        string Password) : IRequest<Result<LoginUserResponse>>;
}
