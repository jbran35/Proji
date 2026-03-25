using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TaskManager.Application.TodoItems.CommandHandlers;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Application.TodoItems.CommandHandlers
{
    public class UnassignTodoItemCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly UnassignTodoItemCommandHandler _handler;

        public UnassignTodoItemCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            _mockCache = new Mock<IDistributedCache>();
            
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            _handler = new UnassignTodoItemCommandHandler(_mockUnitOfWork.Object, _mockCache.Object, _mockUserManager.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new UnassignTodoItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsProjectNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UnassignTodoItemCommand(userId, Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsTodoItemNotFound_WhenTodoItemDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var command = new UnassignTodoItemCommand(userId, projectId, Guid.NewGuid());
            
            var title = Title.Create("Project").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, userId).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
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
            var command = new UnassignTodoItemCommand(userId, projectId, todoItemId);

            var title = Title.Create("Test").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, otherUserId).Value;
            var todoItem = TodoItem.Create(title, description, otherUserId, projectId, null, Priority.Medium, null).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
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
            var command = new UnassignTodoItemCommand(userId, projectId, todoItemId);

            var title = Title.Create("Test").Value;
            var description = Description.Create("Desc").Value;
            var project = Project.Create(title, description, userId).Value;
            
            typeof(Entity).GetProperty("Id")?.SetValue(project, projectId);

            var todoItem = TodoItem.Create(title, description, userId, projectId, assigneeId, Priority.Medium, null).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { FirstName = "Test", LastName = "User", Id = userId });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(todoItemId, It.IsAny<CancellationToken>())).ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.AssigneeId.Should().BeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
