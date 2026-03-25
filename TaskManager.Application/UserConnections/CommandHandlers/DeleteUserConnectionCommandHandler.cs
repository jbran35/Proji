using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskManager.Application.TodoItems.Events;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Application.UserConnections.Events;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.UserConnections.CommandHandlers
{
    /// <summary>
    /// Handles a request/command to delete an assignee from a user's group.
    /// Unassigns any items owned by the user that were assigned to the assignee. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="userManager"></param>
    /// <param name="logger"></param>
    /// <param name="mediator"></param>
    public class DeleteUserConnectionCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager,
        ILogger<DeleteUserConnectionCommandHandler> logger, IMediator mediator) : IRequestHandler<DeleteUserConnectionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly ILogger<DeleteUserConnectionCommandHandler> _logger = logger;
        private readonly IMediator _mediator = mediator;
        public async Task<Result> Handle(DeleteUserConnectionCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure(ErrorCode.UserNotFound, "User Not Found");

            var connection = await _unitOfWork.UserConnectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
            if (connection is null || connection.UserId != request.UserId)
                return Result.Failure(ErrorCode.ConnectionNotFound,"Issue Loading Assignee Connection");

            if (connection.UserId != user.Id)
                return Result.Failure(ErrorCode.Forbidden,"Forbidden");
                
            var assignee = await _userManager.FindByIdAsync(connection.AssigneeId.ToString()); 
            if(assignee is null)
                return Result.Failure(ErrorCode.AssigneeNotFound, "Assignee Not Found");
            
            var assigneeId = assignee.Id; 

            var taskIdsToUnassign = await _unitOfWork.TodoItemRepository.GetMyTodoItemsAssignedToUser(user.Id, connection.AssigneeId, cancellationToken);
            
            bool itemsWereUnassigned = false;
            try
            {
                if (taskIdsToUnassign.Count != 0)
                {
                    await _unitOfWork.TodoItemRepository.UnassignTasksByIdAsync(taskIdsToUnassign, cancellationToken);
                    itemsWereUnassigned = true;

                }

                _unitOfWork.UserConnectionRepository.Delete(connection);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (itemsWereUnassigned)
                {
                    var deletionEvent = new UserRemovedFromGoupEvent(assigneeId);
                    await _mediator.Publish(deletionEvent, cancellationToken);
                }

                return Result.Success("Deleted Successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue Deleting User Connection");
                return Result.Failure(ErrorCode.UnexpectedError, "Issue Deleting Assignee"); 
            }
        }
    }
}
