using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.TodoItems.CommandHandlers;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Application.TodoItems.CommandHandlers
{
    public class AssignTodoItemCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly AssignTodoItemCommandHandler _handler;

        public AssignTodoItemCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            var mockLogger = new Mock<ILogger<AssignTodoItemCommandHandler>>();
            _handler = new AssignTodoItemCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new AssignTodoItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsTodoItemNotFound_WhenTodoItemDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new AssignTodoItemCommand(userId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(command.TodoItemId, It.IsAny<CancellationToken>())).ReturnsAsync((TodoItem?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.TodoItemNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var command = new AssignTodoItemCommand(userId, projectId, todoItemId, Guid.NewGuid());

            var title = Title.Create("Task").Value;
            var description = Description.Create("Desc").Value;
            var todoItem = TodoItem.Create(title, description, otherUserId, projectId, null, Priority.Medium, null).Value;
            
            var project = Project.Create(title, description, otherUserId).Value;
            typeof(TodoItem).GetProperty("Project")?.SetValue(todoItem, project);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var command = new AssignTodoItemCommand(userId, projectId, todoItemId, assigneeId);

            var title = Title.Create("Task").Value;
            var description = Description.Create("Desc").Value;
            var todoItem = TodoItem.Create(title, description, userId, projectId, null, Priority.Medium, null).Value;
            
            var project = Project.Create(title, description, userId).Value;
            typeof(TodoItem).GetProperty("Project")?.SetValue(todoItem, project);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockUserManager.Setup(m => m.FindByIdAsync(assigneeId.ToString())).ReturnsAsync(new User { FirstName = "Assignee", LastName = "User" });
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.AssigneeId.Should().Be(assigneeId);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
