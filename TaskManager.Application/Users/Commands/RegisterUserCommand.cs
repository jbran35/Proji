using MediatR;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Users.Commands
{
    /// <summary>
    /// Represents a request for a new user to register. 
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="Password"></param>
    /// <param name="Email"></param>
    /// <param name="FirstName"></param>
    /// <param name="LastName"></param>
    public record RegisterUserCommand(
        string UserName, 
        string Password, 
        string Email, 
        string FirstName, 
        string LastName
        
        ) : IRequest<Result<RegisterUserResponse>>;
}
