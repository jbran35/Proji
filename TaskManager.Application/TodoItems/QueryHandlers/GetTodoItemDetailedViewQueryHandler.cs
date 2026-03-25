using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManager.Application.Common;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.TodoItems.QueryHandlers
{
    /// <summary>
    /// Handles a request/query to get the detailed view of a TodoItem. 
    /// Not currently used, given the level of detail the TodoItem entity currently encapsulates. 
    /// Could be used in the future if the TodoItem class is expanded. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="logger"></param>
    /// <param name="cache"></param>
    public class GetTodoItemDetailedViewQueryHandler(IUnitOfWork unitOfWork, ILogger<GetTodoItemDetailedViewQueryHandler> logger, 
        IDistributedCache cache) : IRequestHandler<GetTodoItemDetailedViewQuery, Result<TodoItemEntry>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<GetTodoItemDetailedViewQueryHandler> _logger = logger;
        private readonly IDistributedCache _cache = cache;
        public async Task<Result<TodoItemEntry>> Handle(GetTodoItemDetailedViewQuery query, CancellationToken cancellationToken)
        {
            var projectDetailsKey = CacheKeys.ProjectDetailedViews(query.UserId, query.ProjectId);
            try
            {
                var cachedProjectJson = await _cache.GetStringAsync(projectDetailsKey, cancellationToken);

                if (!string.IsNullOrEmpty(cachedProjectJson))
                {
                    var project = JsonSerializer.Deserialize<ProjectDetailedViewDto>(cachedProjectJson);

                    if (project is not null)
                    {
                        var cachedTodoItem = project.TodoItems.FirstOrDefault(t => t.Id == query.TodoItemId);

                        if (cachedTodoItem is not null)
                        {
                            return Result<TodoItemEntry>.Success(cachedTodoItem);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            var todoItem = await _unitOfWork.TodoItemRepository.GetTodoItemByIdAsync(query.TodoItemId, cancellationToken);
            
            if(todoItem is null)
                return Result<TodoItemEntry>.Failure(ErrorCode.TodoItemNotFound, "Task Not Found");
                
            if ((todoItem.OwnerId != query.UserId || todoItem.Project.OwnerId != query.UserId) && todoItem.AssigneeId != query.UserId)
                return Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "You do not own this task");

            var todoItemDetailedViewDto = new TodoItemDetailedViewDto
            {
                Id = todoItem.Id,
                Title = todoItem.Title.Value,
                Description = todoItem.Description.Value,
                ProjectTitle = todoItem.Project.Title,
                AssigneeName = todoItem.Assignee?.FullName ?? string.Empty,
                OwnerName = todoItem.Owner?.FullName ?? string.Empty,
                Priority = todoItem.Priority ?? Priority.None,
                DueDate = todoItem.DueDate,
                CreatedOn = todoItem.CreatedOn,
                Status = todoItem.Status
            };

            return Result<TodoItemEntry>.Success(todoItemDetailedViewDto);
        }
    }
}
