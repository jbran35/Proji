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
    /// Handles the GetProjectDetailsQuery and retrieves the basic details for a project.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class GetProjectDetailsQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<GetProjectDetailsQueryHandler> logger) : IRequestHandler<GetProjectDetailsQuery, Result<ProjectDetailsDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<GetProjectDetailsQueryHandler> _logger = logger;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<Result<ProjectDetailsDto>> Handle(GetProjectDetailsQuery query, CancellationToken cancellationToken)
        {
            var key = CacheKeys.ProjectDetailedViews(query.UserId, query.ProjectId);

            try
            {
                var cachedProject = await _cache.GetStringAsync(key, cancellationToken);

                if (!string.IsNullOrEmpty(cachedProject))
                {
                    var proj = JsonSerializer.Deserialize<ProjectDetailedViewDto>(cachedProject);
                    if (proj is not null)
                    {
                        return Result<ProjectDetailsDto>.Success(new ProjectDetailsDto
                        {
                            Id = proj.Id,
                            Title = proj.Title,
                            Description = proj.Description,
                            CreatedOn = proj.CreatedOn
                        });  
                    }
                }
            }

            catch(Exception ex)
            {
                _logger.LogError(ex, "Error reading from Redis cache.");
            }

            var project = await _unitOfWork.ProjectRepository.GetProjectWithoutTasksAsync(query.ProjectId, cancellationToken);
            if (project is null)
                return Result<ProjectDetailsDto>.Failure(ErrorCode.ProjectNotFound, "Project Not Found");
            
            if(project.OwnerId != query.UserId)
                return Result<ProjectDetailsDto>.Failure(ErrorCode.Forbidden, "Not Project Owner.");

            var detailsDto = project.ToProjectDetailsDto();

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                };

                var serializedDto = JsonSerializer.Serialize(detailsDto, JsonOptions);
                await _cache.SetStringAsync(key, serializedDto, cacheOptions, cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            return Result<ProjectDetailsDto>.Success(detailsDto);

        }
    }
}
