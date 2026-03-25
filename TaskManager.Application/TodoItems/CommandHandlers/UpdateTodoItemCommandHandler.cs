using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.Events;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.TodoItems.CommandHandlers
{
    /// <summary>
    /// Handles a request to update the details for a Todo Item. 
    /// Sends a notification to any affected assignees, so they see the update immediately.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    /// <param name="mediator"></param>
    public class UpdateTodoItemCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, 
        ILogger<UpdateTodoItemCommandHandler> logger, IMediator mediator) : IRequestHandler<UpdateTodoItemCommand, Result<TodoItemEntry>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<UpdateTodoItemCommandHandler> _logger = logger;
        private readonly IMediator _mediator = mediator;

        public async Task<Result<TodoItemEntry>> Handle(UpdateTodoItemCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.UserNotFound, "User Not Found");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(command.ProjectId, cancellationToken);
            if (project is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.ProjectNotFound, "Project Not Found");
            
            if (project.OwnerId != user.Id)
                return Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "Must Be Project Owner To Update This Task");

            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(command.TodoItemId, cancellationToken);
            if (todoItem is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.TodoItemNotFound, "Task Not Found");
            
            if(todoItem.OwnerId != user.Id || todoItem.ProjectId != project.Id)
                return Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "Must Be The Owner To Update This Task");

            if (command.NewTitle is not null)
            {
                var titleResult = Title.Create(command.NewTitle);
                if (titleResult.IsFailure)
                    return Result<TodoItemEntry>.Failure(ErrorCode.TitleError, titleResult.ErrorMessage ?? "Invalid Title");

                todoItem.UpdateTitle(titleResult.Value);
            }

            if (command.NewDescription is not null)
            {
                var descriptionResult = Description.Create(command.NewDescription);
                if (descriptionResult.IsFailure)
                    return Result<TodoItemEntry>.Failure(ErrorCode.DescriptionError, descriptionResult.ErrorMessage ?? "Invalid Description");

                todoItem.UpdateDescription(descriptionResult.Value);
            }

            if (command.NewPriority is not null)
                todoItem.UpdatePriority(command.NewPriority.Value);

            //Used to determine who needs to be updated from this transaction.
            bool hasOldAssignee = todoItem.AssigneeId is not null && todoItem.AssigneeId != Guid.Empty;
            Guid oldAssigneeId = hasOldAssignee ? todoItem.AssigneeId!.Value : Guid.Empty;
            
            bool hasNewAssignee = command.AssigneeId is not null && command.AssigneeId != Guid.Empty;
            Guid newAssigneeId = hasNewAssignee ? command.AssigneeId!.Value : Guid.Empty;

            bool switchingAssignees = hasOldAssignee && hasNewAssignee && command.AssigneeId != todoItem.AssigneeId;
            bool unassigning = hasOldAssignee && !hasNewAssignee;
            bool previouslyUnassigned = !hasOldAssignee && hasNewAssignee;
            bool keepingAssignee = hasOldAssignee && hasNewAssignee && todoItem.AssigneeId == command.AssigneeId; 

            if (switchingAssignees || previouslyUnassigned)
            {
                var newAssignee = await _userManager.FindByIdAsync(command.AssigneeId!.Value.ToString());
                if (newAssignee is null)
                    return Result<TodoItemEntry>.Failure(ErrorCode.AssigneeNotFound, "Assignee Not Found");

                todoItem.AssignToUser(command.AssigneeId.Value);
            }

            if (unassigning)
                todoItem.Unassign();
            
            if (command.NewDueDate.HasValue && command.NewDueDate.Value != DateTime.MinValue)
                todoItem.UpdateDueDate(command.NewDueDate.Value);

            try
            {
                _unitOfWork.TodoItemRepository.Update(todoItem);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                var listEntryDto = new TodoItemEntry
                {
                    Id = todoItem.Id,
                    OwnerId = todoItem.OwnerId, 
                    AssigneeId = todoItem.AssigneeId,
                    Title = todoItem.Title,
                    Description = todoItem.Description,
                    ProjectTitle = todoItem.Project.Title,
                    AssigneeName = todoItem.Assignee?.FullName ?? string.Empty,
                    OwnerName = todoItem.Owner?.FullName ?? string.Empty,
                    Priority = todoItem.Priority ?? Priority.None,
                    DueDate = todoItem.DueDate,
                    CreatedOn = todoItem.CreatedOn,
                    Status = todoItem.Status
                };

                if (previouslyUnassigned || unassigning || switchingAssignees || keepingAssignee)
                {
                    var assignmentEvent = new TodoItemAssignmentChangedEvent(oldAssigneeId, newAssigneeId);
                    await _mediator.Publish(assignmentEvent, cancellationToken);
                }

                return Result<TodoItemEntry>.Success(listEntryDto);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Updating Task");
                Console.WriteLine(ex);
                return Result<TodoItemEntry>.Failure(ErrorCode.UnexpectedError, "Issue Updating Task."); 
            }
        }
    }
}
