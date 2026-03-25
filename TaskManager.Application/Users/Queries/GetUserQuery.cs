using MediatR;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Users.Queries
{
    /// <summary>
    /// A query representing a request to retrieve the details for a given user.
    /// </summary>
    /// <param name="Email"></param>
    public record GetUserQuery(string Email) : IRequest<Result<GetUserResponse>>;

}
