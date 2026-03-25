using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.CommandHandlers;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.TodoItems.CommandHandlers
{
    public class DeleteTodoItemCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly DeleteTodoItemCommandHandler _handler;

        public DeleteTodoItemCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);

            _mockUserManager = MockUserManager.GetMockUserManager();
            var mockCache = new Mock<IDistributedCache>();
            var mockMediator = new Mock<IMediator>();
            var mockNotificationService = new Mock<ITodoItemUpdateNotificationService>();

            _handler = new DeleteTodoItemCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockCache.Object, mockMediator.Object, mockNotificationService.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var command = new DeleteTodoItemCommand(Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_TodoItemNotFound_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var command = new DeleteTodoItemCommand(user.Id, Guid.NewGuid());
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(command.TodoItemId, It.IsAny<CancellationToken>()))
                                   .ReturnsAsync((TodoItem?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.TodoItemNotFound);
        }

        [Fact]
        public async Task Handle_UserNotAuthorized_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var otherUserId = Guid.NewGuid();
            var title = Title.Create("Title").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, otherUserId).Value; // different owner
            var todoItem = TodoItem.Create(title, desc, otherUserId, project.Id, null, null, null).Value;
            
            var prop = typeof(TodoItem).GetProperty("Project");
            prop?.SetValue(todoItem, project);

            var command = new DeleteTodoItemCommand(user.Id, todoItem.Id);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(command.TodoItemId, It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(todoItem);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResultAndDeletesItem()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var title = Title.Create("Title").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, user.Id).Value;
            var todoItem = TodoItem.Create(title, desc, user.Id, project.Id, null, null, null).Value;
            
            var prop = typeof(TodoItem).GetProperty("Project");
            prop?.SetValue(todoItem, project);
            
            project.AddTodoItem(todoItem); 

            var command = new DeleteTodoItemCommand(user.Id, todoItem.Id);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockTodoItemRepository.Setup(r => r.GetTodoItemByIdAsync(command.TodoItemId, It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(todoItem);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(todoItem.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(project);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.Status.Should().Be(Status.Deleted);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}