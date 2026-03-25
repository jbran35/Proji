using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Users.CommandHandlers
{
    /// <summary>
    /// Handles a request/command for a user to register a new account.
    /// </summary>
    /// <param name="userManager"></param>
    public class RegisterUserHandler(UserManager<User> userManager) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
    {
        private readonly UserManager<User> _userManager = userManager;

        public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {

            var userByUsername = await _userManager.FindByNameAsync(request.UserName);
            if (userByUsername != null)
                return Result<RegisterUserResponse>.Failure(ErrorCode.DomainRuleViolation, "Account Already Exists"); 

            var userByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userByEmail is not null)
                return Result<RegisterUserResponse>.Failure(ErrorCode.DomainRuleViolation, "Account Already Exists");

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<RegisterUserResponse>.Failure(ErrorCode.UnexpectedError, $"User creation failed: {errors}");
            }

            var response = new RegisterUserResponse
            {
                Success = true,
                Message = "User created successfully"
            };

            return Result<RegisterUserResponse>.Success(response); 
        }
    }
}
