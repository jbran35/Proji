using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManager.Application.Common;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.Mappers;
using TaskManager.Application.Projects.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.QueryHandlers
{
    /// <summary>
    /// Handles the GetUserProjectsQuery and retrieves the project tiles for a user's project - when they go to the MyProjects page.
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class GetUserProjectsQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<GetUserProjectsQueryHandler> logger) : IRequestHandler<GetUserProjectsQuery, Result<List<ProjectTileDto>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<GetUserProjectsQueryHandler> _logger = logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<Result<List<ProjectTileDto>>> Handle(GetUserProjectsQuery query, CancellationToken cancellationToken)
        {
            var key = CacheKeys.ProjectTiles(query.UserId);

            try
            {
                var cachedTiles = await _cache.GetStringAsync(key, cancellationToken);

                if (!string.IsNullOrEmpty(cachedTiles))
                { 
                    var tiles = JsonSerializer.Deserialize<List<ProjectTileDto>>(cachedTiles);

                    return Result<List<ProjectTileDto>>.Success(tiles!);
                }
            }

            catch(Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            var readOnlyList = await _unitOfWork.ProjectRepository
                .GetAllProjectsByOwnerIdAsync(query.UserId, cancellationToken);

            var projectTiles = readOnlyList.ToProjectTileDtoList();

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };

                var projectTilesJson = JsonSerializer.Serialize(projectTiles, JsonOptions);
                await _cache.SetStringAsync(key, projectTilesJson, cacheOptions, cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Error:");
            }

            return Result<List<ProjectTileDto>>.Success(projectTiles, "Projects Retrieved Successfully");
        }
    }
}
