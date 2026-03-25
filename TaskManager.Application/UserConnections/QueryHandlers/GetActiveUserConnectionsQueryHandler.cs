using MediatR;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Application.UserConnections.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.UserConnections.QueryHandlers
{
    /// <summary>
    /// Handles the request/query to retrieve all of a user's connections/assignees when they go to their MyGrouup page or are editing/creating a new todo item. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    public class GetActiveUserConnectionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetActiveUserConnectionsQuery, Result<IEnumerable<UserConnectionDto>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<Result<IEnumerable<UserConnectionDto>>> Handle(GetActiveUserConnectionsQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
                return Result<IEnumerable<UserConnectionDto>>.Failure(ErrorCode.DomainRuleViolation, "Invalid Request");

            var connections = await _unitOfWork.UserConnectionRepository.GetConnectionsByOwnerIdAsync(request.UserId, cancellationToken);

            var connectionDtos = connections.Select(static c => new UserConnectionDto
            {
                Id = c.Id,
                UserId = c.UserId,
                AssigneeId = c.AssigneeId,
                AssigneeName = c.Assignee?.FullName ?? string.Empty,
                AssigneeEmail = c.Assignee?.Email ?? string.Empty
            }).ToList(); 

            return Result<IEnumerable<UserConnectionDto>>.Success(connectionDtos); 
        }
    }
}
