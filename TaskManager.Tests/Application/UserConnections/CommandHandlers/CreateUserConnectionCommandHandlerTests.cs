using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManager.Application.UserConnections.CommandHandlers;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Tests.Application.UserConnections.CommandHandlers
{
    public class CreateUserConnectionCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserConnectionRepository> _mockUserConnectionRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly CreateUserConnectionCommandHandler _handler;

        public CreateUserConnectionCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserConnectionRepository = new Mock<IUserConnectionRepository>();
            _mockUnitOfWork.Setup(u => u.UserConnectionRepository).Returns(_mockUserConnectionRepository.Object);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            _handler = new CreateUserConnectionCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var command = new CreateUserConnectionCommand(Guid.NewGuid(), Guid.NewGuid());
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
            var assigneeId = Guid.NewGuid();
            var command = new CreateUserConnectionCommand(userId, assigneeId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "User", LastName = "One" });
            _mockUserManager.Setup(m => m.FindByIdAsync(assigneeId.ToString())).ReturnsAsync(new User { Id = assigneeId, FirstName = "User", LastName = "Two", Email = "two@test.com" });
            _mockUserConnectionRepository.Setup(r => r.AnyConnectionExistsAsync(userId, assigneeId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.AssigneeName.Should().Be("User Two");
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
