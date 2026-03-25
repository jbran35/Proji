using MediatR;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Users.Commands
{
    /// <summary>
    /// Represents a request from a user to update their profile details.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="NewFirstName"></param>
    /// <param name="NewLastName"></param>
    /// <param name="NewEmail"></param>
    /// <param name="NewUserName"></param>
    public record UpdateProfileCommand(
        Guid Id,
        string? NewFirstName,
        string? NewLastName,
        string? NewEmail,
        string? NewUserName
        ) : IRequest<Result<UpdateProfileResponse>>;
  
}
