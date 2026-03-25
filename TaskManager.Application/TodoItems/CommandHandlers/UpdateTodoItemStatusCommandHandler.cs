using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;


namespace TaskManager.Application.TodoItems.CommandHandlers
{
    /// <summary>
    /// Handles a request to change the status of a TodoItem between Complete/Incomplete.
    /// If the item is assigned, and the assignee updates the status > Sends a notification to the owner - so they see that update. 
    /// If the item is assigned, and the owner upates the status > Sends a notification to the assignee - so they see that update.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="updateService"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class UpdateTodoItemStatusCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, ITodoItemUpdateNotificationService updateService,
        IDistributedCache cache, ILogger<UpdateTodoItemStatusCommandHandler> logger) : IRequestHandler<UpdateTodoItemStatusCommand, Result<TodoItemEntry>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ITodoItemUpdateNotificationService _updateService = updateService;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<UpdateTodoItemStatusCommandHandler> _logger = logger;

        public async Task<Result<TodoItemEntry>> Handle(UpdateTodoItemStatusCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if(user is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.UserNotFound, "User Not Found");

            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(command.TodoItemId, cancellationToken);
            if(todoItem is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.TodoItemNotFound, "TodoItem Not Found");

            bool hasAssignee = todoItem.AssigneeId is not null && todoItem.AssigneeId != Guid.Empty;
            bool userIsOwner = user.Id == todoItem.OwnerId;
            bool userIsAssignee = user.Id == todoItem.AssigneeId;

            if (!userIsAssignee && !userIsOwner)
                return Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "You Do Not Have Access To This Project Or Task");

            if(todoItem.Project.Status == Status.Deleted)
                return Result<TodoItemEntry>.Failure(ErrorCode.ObjectDeleted, "This Project Has Been Deleted");

            if (todoItem.Status == Status.Incomplete)
                todoItem.MarkAsComplete();

            else if (todoItem.Status == Status.Complete)
            {
                todoItem.MarkAsIncomplete();
                todoItem.Project.MarkAsIncomplete();
            }

            try
            {
                _unitOfWork.TodoItemRepository.Update(todoItem);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var listEntryDto = new TodoItemEntry
                {
                    Id = todoItem.Id,
                    Title = todoItem.Title,
                    ProjectTitle = todoItem.Project.Title,
                    AssigneeName = todoItem.Assignee?.FullName,
                    OwnerName = todoItem.Owner?.FullName ?? string.Empty,
                    Priority = todoItem.Priority ?? Priority.None,
                    DueDate = todoItem.DueDate,
                    CreatedOn = todoItem.CreatedOn,
                    Status = todoItem.Status
                };

                var ownerDetailedViewKey = CacheKeys.ProjectDetailedViews(todoItem.OwnerId, todoItem.ProjectId);
                await _cache.RemoveAsync(ownerDetailedViewKey, CancellationToken.None); 

                if (hasAssignee)
                {
                    var assignedItemsKey = CacheKeys.AssignedTodoItems(todoItem.AssigneeId!.Value);
                    await _cache.RemoveAsync(assignedItemsKey, CancellationToken.None);

                    if (userIsAssignee)
                    {
                        Console.WriteLine("Sending Update");
                        var ownerTilesKey = CacheKeys.ProjectTiles(todoItem.OwnerId);
                        await _cache.RemoveAsync(ownerTilesKey, CancellationToken.None);
                        await _updateService.NotifyTodoItemUpdated(todoItem.OwnerId.ToString());

                    }

                    if (userIsOwner)
                        await _updateService.NotifyTodoItemUpdated(todoItem.AssigneeId!.Value.ToString());
                }

                return Result<TodoItemEntry>.Success(listEntryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue updating task status");
                return Result<TodoItemEntry>.Failure(ErrorCode.UnexpectedError, "Error Updating Task Status.");
            }
        }
    }
}
