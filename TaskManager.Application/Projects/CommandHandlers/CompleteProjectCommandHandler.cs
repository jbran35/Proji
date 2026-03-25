using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Application.Projects.Events;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.CommandHandlers
{
    /// <summary>
    /// Handles marking a project, and all of its incomplete Todo Items, as complete, as well as publishing an event to update 
    /// the Todo Item for any assignees who may be viewing it. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    /// <param name="mediator"></param>
    public class CompleteProjectCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, 
       ILogger<CompleteProjectCommandHandler> logger, IMediator mediator) : IRequestHandler<CompleteProjectCommand, Result<CompleteProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<CompleteProjectCommandHandler> _logger = logger;
        private readonly IMediator _mediator = mediator; 
        public async Task<Result<CompleteProjectResponse>> Handle(CompleteProjectCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<CompleteProjectResponse>.Failure(ErrorCode.UserNotFound, "User not found.");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithTasksAsync(command.ProjectId, cancellationToken);
            if(project is null)
                return Result<CompleteProjectResponse>.Failure(ErrorCode.ProjectNotFound, "Project not found.");

            if(command.UserId != project.OwnerId)
                return Result<CompleteProjectResponse>.Failure(ErrorCode.Forbidden, "Not Project Owner.");

            var result = project.MarkAsComplete(); // TodoItems wil be updated by the Project

            if (result.IsFailure)
            {
                _logger.LogInformation("Failed To Mark Tasks As Complete");
                return Result<CompleteProjectResponse>.Failure(ErrorCode.DomainRuleViolation, result.ErrorMessage!);
            }

            try
            {
                await _unitOfWork.ProjectRepository.CompleteProjectTodoItems(project.Id, cancellationToken);

                _unitOfWork.ProjectRepository.Update(project);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newProjectTile = new ProjectTileDto
                {
                    Id = project.Id,
                    Title = project.Title,
                    TotalTodoItemCount = project.TodoItems.Count,
                    CompleteTodoItemCount = project.TodoItems.Count,
                    CreatedOn = project.CreatedOn,
                    Status = Status.Complete
                }; 

                var response = new CompleteProjectResponse(newProjectTile);

                var completedEvent = new ProjectCompletedEvent(project.Id); // Used to delete assignee's cached assigned todo items and to update their view.
                await _mediator.Publish(completedEvent, cancellationToken);

                return Result<CompleteProjectResponse>.Success(response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Completing Project");
                return Result<CompleteProjectResponse>.Failure(ErrorCode.UnexpectedError, "An error occurred while completing the project.");
            }
        }
    }
}
