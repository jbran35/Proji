using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Users.CommandHandlers
{
    /// <summary>
    /// Verifies the provided credentials for the user and returns their JWT if successful.
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="signInManager"></param>
    /// <param name="tokenService"></param>
    public class LoginUserHandler (UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService) : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly ITokenService _tokenService = tokenService;

        public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(command.UserName);
            if (user is null)
                return Result<LoginUserResponse>.Failure(ErrorCode.AuthError, "Invalid Credentials");

            var stamp = await _userManager.GetSecurityStampAsync(user);
            if (string.IsNullOrEmpty(stamp))
                await _userManager.UpdateSecurityStampAsync(user);

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                command.Password,
                lockoutOnFailure: true
                );

            if (result.Succeeded)
            {
                var response = new LoginUserResponse
                {
                    Token = _tokenService.CreateToken(user),
                };

                return Result<LoginUserResponse>.Success(response);
            }

            return Result<LoginUserResponse>.Failure(ErrorCode.UnexpectedError, "Unexpected Error Logging In"); 
        }
    }
}
