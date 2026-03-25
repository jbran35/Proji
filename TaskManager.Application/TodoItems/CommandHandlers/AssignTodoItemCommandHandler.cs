using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.TodoItems.CommandHandlers
{
    /// <summary>
    /// NOT USED CURRENTLY, but could be used to add quick assignment options on the ProjectDetailedView page. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    public class AssignTodoItemCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager, 
        ILogger<AssignTodoItemCommandHandler> logger) 
        : IRequestHandler<AssignTodoItemCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<AssignTodoItemCommandHandler> _logger = logger;
        public async Task<Result> Handle(AssignTodoItemCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user is null)
                return Result.Failure(ErrorCode.UserNotFound, "User Not Found.");

            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(command.TodoItemId, cancellationToken);
            
            if(todoItem is null)
                return Result.Failure(ErrorCode.TodoItemNotFound, "Task Not Found.");
            
            if (todoItem.OwnerId != command.UserId || todoItem.Project.OwnerId != command.UserId)
                return Result.Failure(ErrorCode.Forbidden, "Forbidden.");

            var assignee = await _userManager.FindByIdAsync(command.AssigneeId.ToString());
            if (assignee is null)
                return Result.Failure(ErrorCode.AssigneeNotFound, "Assignee Not Found.");

            todoItem.AssignToUser(command.AssigneeId);

            try
            {
                _unitOfWork.TodoItemRepository.Update(todoItem);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Assigning Task");
                return Result.Failure(ErrorCode.UnexpectedError, "Issue Assigning Task");
            }

            return Result.Success();
        }
    }
}
