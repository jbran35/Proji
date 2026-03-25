using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Application.Users.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Users.QueryHandlers
{
    /// <summary>
    /// Handles a request to retrieve the details of a given user.
    /// </summary>
    /// <param name="userManager"></param>
    public class GetUserQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUserQuery, Result<GetUserResponse>>
    {
        private readonly UserManager<User> _userManager = userManager;
        public async Task<Result<GetUserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Result<GetUserResponse>.Failure(ErrorCode.DomainRuleViolation, "Invalid Request");

            var user = await _userManager.FindByEmailAsync(request.Email); 

            
            if (user is null || user.Id == Guid.Empty || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                return Result<GetUserResponse>.Failure(ErrorCode.UserNotFound, "Cannot Find User");

            var foundUser = new GetUserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            return Result<GetUserResponse>.Success(foundUser); 
        }
    }
}
