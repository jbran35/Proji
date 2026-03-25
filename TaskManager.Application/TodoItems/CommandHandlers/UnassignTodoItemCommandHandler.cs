using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Application.Common;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.TodoItems.CommandHandlers
{
    /// <summary>
    /// NOT USED CURRENTLY, but could be used to add quick unassignment options on the ProjectDetailedView page. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="cache"></param>
    /// <param name="userManager"></param>
    public class UnassignTodoItemCommandHandler(IUnitOfWork unitOfWork, IDistributedCache cache, UserManager<User> userManager) : IRequestHandler<UnassignTodoItemCommand, Result>
    {
            private readonly IUnitOfWork _unitOfWork = unitOfWork;
            private readonly IDistributedCache _cache = cache;
            private readonly UserManager<User> _userManager = userManager;
        public async Task<Result> Handle(UnassignTodoItemCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result.Failure(ErrorCode.UserNotFound, "User Not Found.");

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(command.ProjectId, cancellationToken);
            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(command.TodoItemId, cancellationToken);

            if(project is null)
                return Result.Failure(ErrorCode.ProjectNotFound, "Project Not Found.");
            
            if(todoItem is null)
                return Result.Failure(ErrorCode.TodoItemNotFound, "Task Not Found.");
            
            if (todoItem.ProjectId != project.Id || project.OwnerId != user.Id || todoItem.OwnerId != user.Id)
                return Result.Failure(ErrorCode.Forbidden, "Forbidden");

            var assigneeKey = string.Empty;
            if (todoItem.AssigneeId is not null && todoItem.AssigneeId != Guid.Empty)
                assigneeKey = CacheKeys.AssignedTodoItems((Guid) todoItem.AssigneeId); 

            var result = todoItem.Unassign();

            if (result.IsFailure)
                return Result.Failure(ErrorCode.DomainRuleViolation, result.ErrorMessage ?? "Failed To Unassign The Task");

            try
            {
                _unitOfWork.TodoItemRepository.Update(todoItem);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (assigneeKey != string.Empty)
                    await _cache.RemoveAsync(assigneeKey, cancellationToken);
            }
            catch (Exception)
            {
                return Result.Failure(ErrorCode.UnexpectedError, "Issue Unassigning Task");
            }

            return Result.Success();
        }
    }
}
