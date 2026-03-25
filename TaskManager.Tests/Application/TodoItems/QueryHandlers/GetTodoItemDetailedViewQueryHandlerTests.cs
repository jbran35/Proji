using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Application.TodoItems.QueryHandlers;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Application.TodoItems.QueryHandlers
{
    public class GetTodoItemDetailedViewQueryHandlerTests
    {
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly GetTodoItemDetailedViewQueryHandler _handler;

        public GetTodoItemDetailedViewQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<GetTodoItemDetailedViewQueryHandler>>();
            _handler = new GetTodoItemDetailedViewQueryHandler(mockUnitOfWork.Object, mockLogger.Object, _mockCache.Object);
        }

        [Fact]
        public async Task Handle_ReturnsFromProjectCache_WhenAvailable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var query = new GetTodoItemDetailedViewQuery { UserId = userId, ProjectId = projectId, TodoItemId = todoItemId };
            
            var cachedProject = new ProjectDetailedViewDto
            {
                Id = projectId,
                Title = "Test Project",
                OwnerId = userId,
                TodoItems = new List<TodoItemEntry>
                {
                    new TodoItemEntry { Id = todoItemId, Title = "Cached Task" }
                }
            };
            var cachedJson = JsonSerializer.Serialize(cachedProject);
            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(cachedJson));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Cached Task");
            _mockTodoItemRepository.Verify(r => r.GetTodoItemByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsFromRepository_WhenNotCached()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var query = new GetTodoItemDetailedViewQuery { UserId = userId, ProjectId = projectId, TodoItemId = todoItemId };

            var title = Title.Create("Repo Task").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, userId).Value;
            var todoItem = TodoItem.Create(title, description, userId, projectId, null, Priority.Medium, null).Value;
            
            typeof(Entity).GetProperty("Id")?.SetValue(todoItem, todoItemId);
            typeof(TodoItem).GetProperty("Project")?.SetValue(todoItem, project);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("Repo Task");
        }
    }
}
