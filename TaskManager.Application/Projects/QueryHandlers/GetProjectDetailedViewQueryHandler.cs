using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManager.Application.Common;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.Mappers;
using TaskManager.Application.Projects.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.QueryHandlers
{
    /// <summary>
    /// Handles a GetProjectDetailedViewQuery, retrieves a project's detailed view (i.e., Project Details, Item Counts, and Todo Items),
    /// and puts the detailed view in the user's Redis cache key.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class GetProjectDetailedViewQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<GetProjectDetailedViewQueryHandler> logger) : IRequestHandler<GetProjectDetailedViewQuery, Result<ProjectDetailedViewDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<GetProjectDetailedViewQueryHandler> _logger = logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<Result<ProjectDetailedViewDto>> Handle(GetProjectDetailedViewQuery query, CancellationToken cancellationToken)
        {
            var key = CacheKeys.ProjectDetailedViews(query.UserId, query.ProjectId);

            try
            {
                var cachedProjectJson = await _cache.GetStringAsync(key, cancellationToken);

                if (!string.IsNullOrEmpty(cachedProjectJson))
                {
                    var cachedProject = JsonSerializer.Deserialize<ProjectDetailedViewDto>(cachedProjectJson);

                    if (cachedProject is not null)
                        return Result<ProjectDetailedViewDto>.Success(cachedProject);
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            var project = await _unitOfWork.ProjectRepository.GetProjectDetailedViewAsync(query.ProjectId, cancellationToken);
            if (project is null)
                return Result<ProjectDetailedViewDto>.Failure(ErrorCode.ProjectNotFound, "Project Not Found");

            if (query.UserId != project.OwnerId)
                return Result<ProjectDetailedViewDto>.Failure(ErrorCode.Forbidden, "Not Project Owner.");

            var projectDetailedViewDto = project.ToProjectDetailedViewDto();

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };

                var serializedDto = JsonSerializer.Serialize(projectDetailedViewDto, JsonOptions);
                await _cache.SetStringAsync(key, serializedDto, cacheOptions, cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            return Result<ProjectDetailedViewDto>.Success(projectDetailedViewDto);
        }
    }
}
