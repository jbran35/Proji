using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.UserConnections.CommandHandlers
{
    /// <summary>
    /// Handles the CreateUserConnectionCommand to create a connection between a user and a new assignee. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    public class CreateUserConnectionCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager) : IRequestHandler<CreateUserConnectionCommand, Result<UserConnectionDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        public async Task<Result<UserConnectionDto>> Handle(CreateUserConnectionCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result<UserConnectionDto>.Failure(ErrorCode.UserNotFound, "Account Not Found");

            var assignee = await _userManager.FindByIdAsync(request.AssigneeId.ToString());
            if (assignee is null)
                return Result<UserConnectionDto>.Failure(ErrorCode.AssigneeNotFound, "Assignee Not Found");

            var areAlreadyConnected = await _unitOfWork.UserConnectionRepository.AnyConnectionExistsAsync(user.Id, assignee.Id, cancellationToken);
            if (areAlreadyConnected)
                return Result<UserConnectionDto>.Failure(ErrorCode.DomainRuleViolation,"It Looks Like You're Already Connected With This User");

            var newConnectionResult = UserConnection.Create(user.Id, assignee.Id);
            if (newConnectionResult.IsFailure)
                return Result<UserConnectionDto>.Failure(ErrorCode.DomainRuleViolation, newConnectionResult.ErrorMessage ?? "Failed to add user to your group.");

            var newConnection = newConnectionResult.Value;
            try
            {
                _unitOfWork.UserConnectionRepository.Add(newConnection);
                await _unitOfWork.SaveChangesAsync(cancellationToken); 
            }

            catch (Exception)
            {
                return Result<UserConnectionDto>.Failure(ErrorCode.UnexpectedError, "Error Adding Assignee");
            }

            var connectionDto = new UserConnectionDto
            {
                Id = newConnection.Id,
                UserId = newConnection.UserId,
                AssigneeId = newConnection.AssigneeId,
                AssigneeName = assignee.FullName,
                AssigneeEmail = assignee.Email ?? string.Empty
            };

            return Result<UserConnectionDto>.Success(connectionDto); 
        }
    }
}
