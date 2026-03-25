using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.CommandHandlers;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Application.TodoItems.CommandHandlers
{
    public class UpdateTodoItemStatusCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly UpdateTodoItemStatusCommandHandler _handler;

        public UpdateTodoItemStatusCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            var mockUpdateService = new Mock<ITodoItemUpdateNotificationService>();
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<UpdateTodoItemStatusCommandHandler>>();
            _handler = new UpdateTodoItemStatusCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockUpdateService.Object, _mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new UpdateTodoItemStatusCommand(Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserIsNotOwnerOrAssignee()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var command = new UpdateTodoItemStatusCommand(userId, todoItemId);

            var title = Title.Create("Test").Value;
            var description = Description.Create("Desc").Value;
            var todoItem = TodoItem.Create(title, description, otherUserId, projectId, null, Priority.Medium, null).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User", Id = userId });
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenStatusUpdated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var command = new UpdateTodoItemStatusCommand(userId, todoItemId);

            var user = new User { FirstName = "Test", LastName = "User", Id = userId };
            var title = Title.Create("Test").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, userId).Value;
            var todoItem = TodoItem.Create(title, description, userId, projectId, null, Priority.Medium, null).Value;
            
            typeof(TodoItem).GetProperty("Project")?.SetValue(todoItem, project);
            typeof(Entry).GetProperty("Owner")?.SetValue(todoItem, user);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.Status.Should().Be(Status.Complete);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
