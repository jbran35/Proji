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
    public class GetProjectDetailedViewQueryHandlerTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly GetProjectDetailedViewQueryHandler _handler;

        public GetProjectDetailedViewQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<GetProjectDetailedViewQueryHandler>>();
            _handler = new GetProjectDetailedViewQueryHandler(mockUnitOfWork.Object, _mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsCachedValue_WhenCacheIsNotEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailedViewQuery(userId, projectId);
            var cachedDto = new ProjectDetailedViewDto
            {
                Id = projectId,
                OwnerId = userId,
                Title = "Cached Project",
                Description = "Cached Description",
                Status = Status.Incomplete,
                TodoItems = new List<TaskManager.Application.TodoItems.DTOs.TodoItemEntry>()
            };
            var cachedJson = JsonSerializer.Serialize(cachedDto);
            var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(cachedBytes);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Cached Project");
            _mockProjectRepository.Verify(r => r.GetProjectDetailedViewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsFailure_WhenProjectNotFoundInRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var query = new GetProjectDetailedViewQuery(userId, projectId);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectDetailedViewAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync((IProjectDetailedView?)null);

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
            var query = new GetProjectDetailedViewQuery(userId, projectId);

            var projectMock = new Mock<IProjectDetailedView>();
            projectMock.Setup(p => p.OwnerId).Returns(otherUserId);
            projectMock.Setup(p => p.TodoItems).Returns(new List<ITodoItemEntry>());

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectDetailedViewAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(projectMock.Object);

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
            var query = new GetProjectDetailedViewQuery(userId, projectId);

            var projectMock = new Mock<IProjectDetailedView>();
            projectMock.Setup(p => p.Id).Returns(projectId);
            projectMock.Setup(p => p.OwnerId).Returns(userId);
            projectMock.Setup(p => p.Title).Returns("Project Title");
            projectMock.Setup(p => p.Description).Returns("Project Description");
            projectMock.Setup(p => p.TodoItems).Returns(new List<ITodoItemEntry>());

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockProjectRepository.Setup(r => r.GetProjectDetailedViewAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(projectMock.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Project Title");
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }
    }
}
