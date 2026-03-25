using FluentAssertions;
using MediatR;
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
    public class UpdateTodoItemCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly UpdateTodoItemCommandHandler _handler;

        public UpdateTodoItemCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<UpdateTodoItemCommandHandler>>();
            _handler = new UpdateTodoItemCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockLogger.Object, mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new UpdateTodoItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "New Title", "New Desc", null, null);
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserIsNotProjectOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var command = new UpdateTodoItemCommand(userId, projectId, Guid.NewGuid(), null, "New Title", "New Desc", null, null);

            var title = Title.Create("Test").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, otherUserId).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User", Id = userId });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenValidUpdate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var todoItemId = Guid.NewGuid();
            var command = new UpdateTodoItemCommand(userId, projectId, todoItemId, null, "New Title", "New Desc", Priority.High, DateTime.UtcNow.AddDays(1));

            var user = new User { FirstName = "Test", LastName = "User", Id = userId };
            var title = Title.Create("Old Title").Value;
            var description = Description.Create("Old Desc").Value;
            var project = Project.Create(title, description, userId).Value;
            typeof(Entity).GetProperty("Id")?.SetValue(project, projectId);

            var todoItem = TodoItem.Create(title, description, userId, projectId, null, Priority.Low, null).Value;
            typeof(Entity).GetProperty("Id")?.SetValue(todoItem, todoItemId);
            typeof(TodoItem).GetProperty("Project")?.SetValue(todoItem, project);
            typeof(Entry).GetProperty("Owner")?.SetValue(todoItem, user);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.Title.Value.Should().Be("New Title");
            todoItem.Description.Value.Should().Be("New Desc");
            todoItem.Priority.Should().Be(Priority.High);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
