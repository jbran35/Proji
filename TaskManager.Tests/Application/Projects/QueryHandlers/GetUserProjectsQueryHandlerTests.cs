using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.Queries;
using TaskManager.Application.Projects.QueryHandlers;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Tests.Application.Projects.QueryHandlers
{
    public class GetUserProjectsQueryHandlerTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly GetUserProjectsQueryHandler _handler;

        public GetUserProjectsQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<GetUserProjectsQueryHandler>>();
            _handler = new GetUserProjectsQueryHandler(mockUnitOfWork.Object, _mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsFromCache_WhenCacheIsNotEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProjectsQuery(userId);
            var cachedTiles = new List<ProjectTileDto>
            {
                new ProjectTileDto { Id = Guid.NewGuid(), OwnerId = userId, Title = "Cached Project", CreatedOn = DateTime.UtcNow }
            };
            var cachedJson = JsonSerializer.Serialize(cachedTiles);
            var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(cachedBytes);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Title.Should().Be("Cached Project");
            _mockProjectRepository.Verify(r => r.GetAllProjectsByOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsFromRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProjectsQuery(userId);
            
            var projectTileMock = new Mock<IProjectTile>();
            projectTileMock.Setup(p => p.Id).Returns(Guid.NewGuid());
            projectTileMock.Setup(p => p.OwnerId).Returns(userId);
            projectTileMock.Setup(p => p.Title).Returns("Repo Project");
            projectTileMock.Setup(p => p.CreatedOn).Returns(DateTime.UtcNow);
            projectTileMock.Setup(p => p.Status).Returns(Status.Incomplete);

            var projectTiles = new List<IProjectTile> { projectTileMock.Object };

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetAllProjectsByOwnerIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(projectTiles.AsReadOnly());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Title.Should().Be("Repo Project");
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }
    }
}
