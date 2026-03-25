using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Common
{
    //Used so that Redis cache key removal can be streamlined once a command handler finishes.
    public class CacheInvalidatorHandler<TRequest, TResponse>(IDistributedCache cache, ILogger<CacheInvalidatorHandler<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<CacheInvalidatorHandler<TRequest, TResponse>> _logger = logger; 
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next(cancellationToken);

            if (request is ICacheInvalidator cacheInvalidator && response is Result { IsSuccess: true})
            {
                foreach (var key in cacheInvalidator.Keys)
                {
                    try
                    {
                        await _cache.RemoveAsync(key, cancellationToken);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Issue Clearing Assignee's Cached Task List");
                    }
                }
            }

            return response; 
        }
    }
}
