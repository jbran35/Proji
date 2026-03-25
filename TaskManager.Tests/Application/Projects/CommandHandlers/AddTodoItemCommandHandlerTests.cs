using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Projects.CommandHandlers;
using TaskManager.Application.Projects.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Projects.CommandHandlers
{
    public class AddTodoItemCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly AddTodoItemCommandHandler _handler;

        public AddTodoItemCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            _mockUserManager = MockUserManager.GetMockUserManager();
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<AddTodoItemCommandHandler>>();
            var mockNotificationService = new Mock<ITodoItemUpdateNotificationService>();

            _handler = new AddTodoItemCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockLogger.Object, mockMediator.Object, mockNotificationService.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var command = new AddTodoItemCommand(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", null, Priority.None);
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var command = new AddTodoItemCommand(Guid.NewGuid(), user.Id, null, "Title", "Desc", null, Priority.None);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((Project?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public async Task Handle_NotProjectOwner_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var otherUserId = Guid.NewGuid();
            var title = Title.Create("Title").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, otherUserId).Value;
            var command = new AddTodoItemCommand(project.Id, user.Id, null, "Task", "Desc", null, Priority.None);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(project);

            // Act
            _mockTodoItemRepository.Setup(r => r.Add(It.IsAny<TodoItem>())).Callback<TodoItem>(t => {
                var prop = typeof(TodoItem).GetProperty("Project");
                prop?.SetValue(t, project);
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResultAndAddsItem()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var title = Title.Create("Project").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, user.Id).Value;
            var command = new AddTodoItemCommand(project.Id, user.Id, null, "Task", "Task Desc", null, Priority.High);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(project);

            // Act
            _mockTodoItemRepository.Setup(r => r.Add(It.IsAny<TodoItem>())).Callback<TodoItem>(t => {
                var prop = typeof(TodoItem).GetProperty("Project");
                prop?.SetValue(t, project);
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.ErrorMessage.Should().BeNullOrEmpty();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(command.Title);
            
            _mockTodoItemRepository.Verify(r => r.Add(It.IsAny<TodoItem>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}