using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Application.TodoItems.QueryHandlers;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Application.TodoItems.QueryHandlers
{
    public class GetAssignedTodoItemsQueryHandlerTests
    {
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly GetAssignedTodoItemsQueryHandler _handler;

        public GetAssignedTodoItemsQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<GetAssignedTodoItemsQueryHandler>>();
            _handler = new GetAssignedTodoItemsQueryHandler(mockUnitOfWork.Object, _mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsFromCache_WhenCacheIsNotEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAssignedTodoItemsQuery(userId);
            var cachedTasks = new List<TodoItemEntry>
            {
                new TodoItemEntry { Id = Guid.NewGuid(), Title = "Cached Task", Status = Status.Incomplete }
            };
            var cachedJson = JsonSerializer.Serialize(cachedTasks);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(cachedJson));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Title.Should().Be("Cached Task");
            _mockTodoItemRepository.Verify(r => r.GetMyAssignedTodoItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsFromRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAssignedTodoItemsQuery(userId);
            
            var taskMock = new Mock<ITodoItemEntry>();
            taskMock.Setup(t => t.Id).Returns(Guid.NewGuid());
            taskMock.Setup(t => t.Title).Returns("Repo Task");
            taskMock.Setup(t => t.Status).Returns(Status.Incomplete);

            var tasks = new List<ITodoItemEntry> 
            { 
                new TodoItemEntry { Id = Guid.NewGuid(), Title = "Repo Task", Status = Status.Incomplete } 
            };

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockTodoItemRepository.Setup(r => r.GetMyAssignedTodoItemsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(tasks.AsReadOnly());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Title.Should().Be("Repo Task");
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }
    }
}
