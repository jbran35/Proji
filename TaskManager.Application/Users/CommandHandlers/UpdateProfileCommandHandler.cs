using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.DTOs;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Users.CommandHandlers
{
    /// <summary>
    /// Handles updating a user's profile details.
    /// Provides a new token that incorporates their updated Claims. 
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    /// <param name="tokenService"></param>
    public class UpdateProfileCommandHandler(UserManager<User> userManager, 
        ILogger<UpdateProfileCommandHandler> logger, ITokenService tokenService) 
        : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<UpdateProfileCommandHandler> _logger = logger;
        private readonly ITokenService _tokenService = tokenService;
        
        public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
                return Result<UpdateProfileResponse>.Failure(ErrorCode.DomainRuleViolation,"Invalid Request");

            var user = await _userManager.FindByIdAsync(request.Id.ToString());
            if (user is null)
                return Result<UpdateProfileResponse>.Failure(ErrorCode.UserNotFound,"Account Not Found");

            if (request.NewFirstName is not null && request.NewFirstName != string.Empty && request.NewFirstName != user.FirstName)
                user.FirstName = request.NewFirstName;

            if(request.NewLastName is not null && request.NewLastName != string.Empty && request.NewLastName != user.LastName)
                user.LastName = request.NewLastName;
            
            if (request.NewEmail is not null && request.NewEmail != string.Empty && request.NewEmail != user.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, request.NewEmail);
                if (!emailResult.Succeeded)
                    return Result<UpdateProfileResponse>.Failure(ErrorCode.EmailError, "Unexpected Error Updating Email"); 
            }

            if (request.NewUserName is not null && request.NewUserName != string.Empty && request.NewUserName != user.UserName)
            {
                var userNameResult = await _userManager.SetUserNameAsync(user, request.NewUserName);
                if(!userNameResult.Succeeded)
                    return Result<UpdateProfileResponse>.Failure(ErrorCode.UserNameError, "Unexpected Error Updating UserName");
            }

            var newProfile = new UserProfileDto(
                user.Id,
                user.FirstName, 
                user.LastName,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty);

            try
            {
                var updateResult = await _userManager.UpdateAsync(user); 
                if (!updateResult.Succeeded)
                    return Result<UpdateProfileResponse>.Failure(ErrorCode.UnexpectedError, "Unexpected Error Updating Your Profile");
            }

            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Error updating profile:");
                return Result<UpdateProfileResponse>.Failure(ErrorCode.UnexpectedError,"Unexpected Error Updating Your Profile");
            }

            var newToken = _tokenService.CreateToken(user);

            var response = new UpdateProfileResponse
            {
                Profile = newProfile,
                Token = newToken
            };

            return Result<UpdateProfileResponse>.Success(response); 
        }
    }
}
