using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.Projects.CommandHandlers
{
    /// <summary>
    /// Handles project creation and validation of its initial properties.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    public class CreateProjectCommandHandler(
        IUnitOfWork unitOfWork, UserManager<User> userManager, 
        ILogger<CreateProjectCommandHandler> logger) : IRequestHandler<CreateProjectCommand, Result<ProjectTileDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<CreateProjectCommandHandler> _logger = logger;
        public async Task<Result<ProjectTileDto>> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<ProjectTileDto>.Failure(ErrorCode.UserNotFound, "User not found.");

            var projectTitleResult = Title.Create(command.Title);
            if (projectTitleResult.IsFailure)
                return Result<ProjectTileDto>.Failure(ErrorCode.DomainRuleViolation, projectTitleResult.ErrorMessage ?? "Invalid project title.");

            var projectDescriptionResult = Description.Create(command.Description!);
            if (projectDescriptionResult.IsFailure)
                return Result<ProjectTileDto>.Failure(ErrorCode.DomainRuleViolation, projectDescriptionResult.ErrorMessage ?? "Invalid project description.");

            var projectResult = Project.Create(projectTitleResult.Value, projectDescriptionResult.Value, command.UserId);
            if (projectResult.IsFailure)
                return Result<ProjectTileDto>.Failure(ErrorCode.DomainRuleViolation, projectResult.ErrorMessage ?? "Failed to create project.");

            var projectDetails = new ProjectTileDto
            {
                Id = projectResult.Value.Id,
                OwnerId = user.Id,
                Title = projectResult.Value.Title,
                Description = projectResult.Value.Description,
                TotalTodoItemCount = 0,
                CompleteTodoItemCount = 0,
                CreatedOn = projectResult.Value.CreatedOn,
                Status = Status.Incomplete //New projects are assumed to be incomplete
            };

            try
            {
                _unitOfWork.ProjectRepository.Add(projectResult.Value);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProjectTileDto>.Success(projectDetails); 
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Creating Project");
                return Result<ProjectTileDto>.Failure(ErrorCode.UnexpectedError, "An error occurred while saving the project.");
            }
        }
    }
}
