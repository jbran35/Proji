using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManager.Application.Common;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.TodoItems.QueryHandlers
{
    /// <summary>
    /// Handles the request/query to retrieve the items assigned to the user. 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class GetAssignedTodoItemsQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, 
        ILogger<GetAssignedTodoItemsQueryHandler> logger) : IRequestHandler<GetAssignedTodoItemsQuery, 
        Result<List<TodoItemEntry>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<GetAssignedTodoItemsQueryHandler> _logger = logger;
        private readonly IDistributedCache _cache = cache;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<Result<List<TodoItemEntry>>> Handle(GetAssignedTodoItemsQuery query, CancellationToken cancellationToken)
        {
            string key = CacheKeys.AssignedTodoItems(query.UserId);

            try
            {
                var cachedTodoItems = await _cache.GetStringAsync(key, cancellationToken);

                if (!string.IsNullOrEmpty(cachedTodoItems))
                {
                    var tasks = JsonSerializer.Deserialize<List<TodoItemEntry>>(cachedTodoItems);
                    return Result<List<TodoItemEntry>>.Success(tasks!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            var readOnlyList = await _unitOfWork.TodoItemRepository.GetMyAssignedTodoItemsAsync(query.UserId, cancellationToken); 

            var assignedItems = readOnlyList.Cast<TodoItemEntry>().ToList();

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                };

                string serializedList = JsonSerializer.Serialize(assignedItems, _jsonOptions);
                await _cache.SetStringAsync(key, serializedList, cacheOptions, cancellationToken);
            }

            catch(Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            return Result<List<TodoItemEntry>>.Success(assignedItems); 
        }
    }
}
