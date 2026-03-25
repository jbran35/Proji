using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Application.TodoItems.Events;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.TodoItems.CommandHandlers
{
    /// <summary>
    /// Handles a request to delete a todo item and remove it from a project.
    /// Creates & publishes a AssignedTodoItemDeletedEvent - so the assignee can see the deletion instantly.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="cache"></param>
    /// <param name="mediator"></param>
    public class DeleteTodoItemCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, IDistributedCache cache, 
        IMediator mediator, ITodoItemUpdateNotificationService notificationService) : IRequestHandler<DeleteTodoItemCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IDistributedCache _cache = cache;
        private readonly IMediator _mediator = mediator;
        private readonly ITodoItemUpdateNotificationService _notificationService = notificationService;

        public async Task<Result> Handle(DeleteTodoItemCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if(user is null)
                return Result.Failure(ErrorCode.UserNotFound,"User Not Found");
           
            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(command.TodoItemId, cancellationToken);
            if (todoItem is null)
                return Result.Failure(ErrorCode.TodoItemNotFound, "Task Not Found");

            if (todoItem.OwnerId != user.Id || todoItem.Project.OwnerId != command.UserId)
                return Result.Failure(ErrorCode.Forbidden, "You are not authorized to delete this task"); 

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(todoItem.ProjectId, cancellationToken); 
            if(project is null)
                return Result.Failure(ErrorCode.ProjectNotFound, "Task's Project Not Found");

            Guid assigneeId = Guid.Empty;
            bool hasAssigneeId = todoItem.AssigneeId is not null && todoItem.AssigneeId != Guid.Empty; 
            if (hasAssigneeId)
                assigneeId = todoItem.AssigneeId!.Value; 

            var projectDetailsKey = CacheKeys.ProjectDetailedViews(user.Id, project.Id); 

            try
            {
                todoItem.Project.DeleteTodoItem(todoItem);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _cache.RemoveAsync(projectDetailsKey, CancellationToken.None);
                await _notificationService.NotifyTodoItemUpdated(user.Id.ToString());

                var deletionEvent = new AssignedTodoItemDeletedEvent(assigneeId);
                await _mediator.Publish(deletionEvent, cancellationToken);
            }

            catch (Exception ex)
            {
                return Result.Failure(ErrorCode.UnexpectedError, $"Issue Deleting Task: {ex}"); 
            }

            return Result.Success();
        }
    }
}
