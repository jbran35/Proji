using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.Events;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.Projects.CommandHandlers
{
    /// <summary>
    /// Handles task creation and adding it to a project, as well as notifying the assignee of a task (if applicable). 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    /// <param name="mediator"></param>
    public class AddTodoItemCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager,
        ILogger<AddTodoItemCommandHandler> logger, IMediator mediator, ITodoItemUpdateNotificationService notificationService) : IRequestHandler<AddTodoItemCommand, Result<TodoItemEntry>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<AddTodoItemCommandHandler> _logger = logger;
        private readonly IMediator _mediator = mediator;
        private readonly ITodoItemUpdateNotificationService _notificationService = notificationService;
        public async Task<Result<TodoItemEntry>> Handle(AddTodoItemCommand command, CancellationToken cancellationToken)
        {
            User? user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.UserNotFound, "User not found.");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(command.ProjectId, cancellationToken);
            if (project is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.ProjectNotFound, "Project not found.");

            if(!project.OwnerId.Equals(command.UserId))
                return Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "Unauthorized.");

            var assigneeId = Guid.Empty;
            var hasAssigneeId = command.AssigneeId != Guid.Empty && command.AssigneeId is not null;
            var assigneeIsValidated = false;

            if (hasAssigneeId)
            {
                assigneeId = command.AssigneeId!.Value;
                User? assignee = await _userManager.FindByIdAsync(assigneeId.ToString());

                if (assignee is null)
                    return Result<TodoItemEntry>.Failure(ErrorCode.AssigneeNotFound, "Assignee Could Not Be Found.");

                assigneeIsValidated = true;
            }

            var todoItemTitleResult = Title.Create(command.Title);
            if (todoItemTitleResult.IsFailure)
                return Result<TodoItemEntry>.Failure(ErrorCode.TitleError, todoItemTitleResult.ErrorMessage ?? "Invalid project title.");

            var todoItemDescriptionResult = Description.Create(command.Description!);
            if (todoItemDescriptionResult.IsFailure)
                return Result<TodoItemEntry>.Failure(ErrorCode.DescriptionError, todoItemDescriptionResult.ErrorMessage ?? "Invalid project description.");

            var todoItemResult = TodoItem.Create(todoItemTitleResult.Value, todoItemDescriptionResult.Value, command.UserId, command.ProjectId, command.AssigneeId,
                command.Priority, command.DueDate);

            if (todoItemResult.IsFailure)
                return Result<TodoItemEntry>.Failure(ErrorCode.DomainRuleViolation, todoItemResult.ErrorMessage ?? "Failed to create todo item.");

            var todoItem = todoItemResult.Value;

            project.AddTodoItem(todoItem);
            project.MarkAsIncomplete(); //Adding a TodoItem to a completed project effectively makes the project incomplete. 
            _unitOfWork.TodoItemRepository.Add(todoItem);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var listEntryDto = new TodoItemEntry
                {
                    Id = todoItem.Id,
                    AssigneeId = todoItem.AssigneeId, 
                    OwnerId = todoItem.OwnerId,
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

                if (!assigneeIsValidated) return Result<TodoItemEntry>.Success(listEntryDto);
                
                //Event ensures that assignee's My Assigned TodoItems list is removed from Redis Cache & that they see the new item immediately.
                var assignedTodoItemCreatedEvent = new AssignedTodoItemCreatedEvent(assigneeId); 
                await _mediator.Publish(assignedTodoItemCreatedEvent, cancellationToken);
                return Result<TodoItemEntry>.Success(listEntryDto);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Encountered"); 
                return Result<TodoItemEntry>.Failure(ErrorCode.UnexpectedError, $"An error occurred while adding the todo item.");
            }
        }
    }
}
