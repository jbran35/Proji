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
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Application.Projects.QueryHandlers
{
    public class GetProjectDetailsQueryHandlerTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly GetProjectDetailsQueryHandler _handler;

        public GetProjectDetailsQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<GetProjectDetailsQueryHandler>>();
            _handler = new GetProjectDetailsQueryHandler(mockUnitOfWork.Object, _mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsFromDetailedViewCache_WhenDetailedViewIsCached()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailsQuery(userId, projectId);
            var cachedDetailedView = new ProjectDetailedViewDto
            {
                Id = projectId,
                OwnerId = userId,
                Title = "Cached Project",
                Description = "Cached Description",
                CreatedOn = DateTime.UtcNow,
                Status = Status.Incomplete
            };
            var cachedJson = JsonSerializer.Serialize(cachedDetailedView);
            var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(cachedBytes);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Cached Project");
            _mockProjectRepository.Verify(r => r.GetProjectWithoutTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsFailure_WhenProjectNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailsQuery(userId, projectId);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailsQuery(userId, projectId);

            var title = Title.Create("Test Project").Value;
            var description = Description.Create("Test Description").Value;
            var project = Project.Create(title, description, otherUserId).Value;

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ReturnsSuccessAndCaches_WhenFoundInRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailsQuery(userId, projectId);

            var title = Title.Create("Project Title").Value;
            var description = Description.Create("Project Description").Value;
            var project = Project.Create(title, description, userId).Value;

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Project Title");
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }
    }
}
