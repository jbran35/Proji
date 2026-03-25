using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.UserConnections.CommandHandlers;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Tests.Application.UserConnections.CommandHandlers
{
    public class DeleteUserConnectionCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserConnectionRepository> _mockUserConnectionRepository;
        private readonly Mock<ITodoItemRepository> _mockTodoItemRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly DeleteUserConnectionCommandHandler _handler;

        public DeleteUserConnectionCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserConnectionRepository = new Mock<IUserConnectionRepository>();
            _mockTodoItemRepository = new Mock<ITodoItemRepository>();
            _mockUnitOfWork.Setup(u => u.UserConnectionRepository).Returns(_mockUserConnectionRepository.Object);
            _mockUnitOfWork.Setup(u => u.TodoItemRepository).Returns(_mockTodoItemRepository.Object);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<DeleteUserConnectionCommandHandler>>();
            _handler = new DeleteUserConnectionCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockLogger.Object, mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new DeleteUserConnectionCommand(Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var connectionId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var command = new DeleteUserConnectionCommand(userId, connectionId);

            var connectionResult = UserConnection.Create(userId, assigneeId);
            var connection = connectionResult.Value;
            typeof(Entity).GetProperty("Id")?.SetValue(connection, connectionId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "User", LastName = "One" });
            _mockUserManager.Setup(m => m.FindByIdAsync(assigneeId.ToString())).ReturnsAsync(new User { Id = assigneeId, FirstName = "User", LastName = "Two" });
            _mockUserConnectionRepository.Setup(r => r.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
            _mockTodoItemRepository.Setup(r => r.GetMyTodoItemsAssignedToUser(userId, assigneeId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUserConnectionRepository.Verify(r => r.Delete(connection), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
