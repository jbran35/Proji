using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.CommandHandlers
{
    /// <summary>
    /// Handles updating a project's details and remove's an assignee's Assigned Tasks list so they don't keep seeing stale information.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class UpdateProjectCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, IDistributedCache cache, 
        ILogger<UpdateProjectCommandHandler> logger) : IRequestHandler<UpdateProjectCommand, Result<ProjectDetailsDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IDistributedCache _cache = cache; 
        private readonly ILogger<UpdateProjectCommandHandler> _logger = logger; 
        public async Task<Result<ProjectDetailsDto>> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<ProjectDetailsDto>.Failure(ErrorCode.UserNotFound, "User not found.");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(command.ProjectId, cancellationToken);
            if (project is null)
                return Result<ProjectDetailsDto>.Failure(ErrorCode.ProjectNotFound, "Project not found.");

            if (project.OwnerId != command.UserId)
                return Result<ProjectDetailsDto>.Failure(ErrorCode.Forbidden, "Unauthorized: You do not have permission to update this project.");

            if(command.NewTitle is not null && command.NewTitle != project.Title)
            {
                var updateTitleResult = project.UpdateTitle(command.NewTitle); 
                if (updateTitleResult.IsFailure)
                    return Result<ProjectDetailsDto>.Failure(ErrorCode.TitleError, updateTitleResult.ErrorMessage ?? "Failed to update project Title.");
            }

            if (command.NewDescription is not null && command.NewDescription != project.Description)
            {
                var updateDescriptionResult = project.UpdateDescription(command.NewDescription);
                if (updateDescriptionResult.IsFailure)
                    return Result<ProjectDetailsDto>.Failure(ErrorCode.DescriptionError, updateDescriptionResult.ErrorMessage ?? "Failed to update project description.");
            }

            //Handling assignee keys here because they may not be accessible from Presentation
            //at the time of project completion (to pass to CacheInvalidator)
            var assigneeIds = await _unitOfWork.ProjectRepository.GetProjectIncompleteTodoItemAssigneeIds(command.ProjectId, cancellationToken);

            try
            {
                _unitOfWork.ProjectRepository.Update(project);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                var response = new ProjectDetailsDto
                {
                    Id = project.Id,
                    Title = project.Title,
                    Description = project.Description,
                    CreatedOn = project.CreatedOn
                };

                if (assigneeIds.Count > 0) // No need to remove keys if no changes are made.
                {
                    foreach (var key in assigneeIds)
                    {
                        await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(key), CancellationToken.None);
                    }
                }

                return Result<ProjectDetailsDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Updating Project");
                return Result<ProjectDetailsDto>.Failure(ErrorCode.UnexpectedError, $"An error occurred while updating the project description.");
            }
        }
    }
}
