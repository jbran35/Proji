using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.CommandHandlers
{
    /// <summary>
    /// Handles project deletion and removes any assignees' Redis keys as to prevent them from seeing stale information.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class DeleteProjectCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, IDistributedCache cache, 
        ILogger<DeleteProjectCommandHandler> logger) : IRequestHandler<DeleteProjectCommand, Result<DeleteProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork; 
        private readonly UserManager<User> _userManager = userManager;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<DeleteProjectCommandHandler> _logger = logger; 
        public async Task<Result<DeleteProjectResponse>> Handle(DeleteProjectCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<DeleteProjectResponse>.Failure(ErrorCode.UserNotFound, "User not found.");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(command.ProjectId, cancellationToken);
            if (project is null)
                return Result<DeleteProjectResponse>.Failure(ErrorCode.ProjectNotFound, "Project not found.");

            if (project.OwnerId != command.UserId)
                return Result<DeleteProjectResponse>.Failure(ErrorCode.Forbidden, "Not Project Owner.");

            //Handling assignee keys here because they may not be easily accessible from Presentation
            //at the time of project deletion (to pass to CacheInvalidator).
            var assigneeIds = await _unitOfWork.ProjectRepository.GetProjectIncompleteTodoItemAssigneeIds(command.ProjectId, cancellationToken);

            try
            {
                _unitOfWork.ProjectRepository.Delete(project);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (assigneeIds.Count > 0) // No need to remove keys if there are no assignees
                {
                    foreach (var key in assigneeIds)
                    {
                        await _cache.RemoveAsync(CacheKeys.AssignedTodoItems(key), CancellationToken.None); 
                    }
                }

                var response = new DeleteProjectResponse(project.Id, "Project Successfully Deleted");
                
                return Result<DeleteProjectResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Removing Project");
                return Result<DeleteProjectResponse>.Failure(ErrorCode.UnexpectedError, "An error occurred while deleting the project.");
            }
        }
    }
}
