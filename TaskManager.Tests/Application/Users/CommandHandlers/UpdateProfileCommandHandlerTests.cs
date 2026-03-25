using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.CommandHandlers;
using TaskManager.Application.Users.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Users.CommandHandlers
{
    public class UpdateProfileCommandHandlerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly UpdateProfileCommandHandler _handler;

        public UpdateProfileCommandHandlerTests()
        {
            _mockUserManager = MockUserManager.GetMockUserManager();
            _mockTokenService = new Mock<ITokenService>();
            var mockLogger = new Mock<ILogger<UpdateProfileCommandHandler>>();
            _handler = new UpdateProfileCommandHandler(_mockUserManager.Object, mockLogger.Object, _mockTokenService.Object);
        }

        [Fact]
        public async Task Handle_InvalidId_ReturnsFailure()
        {
            // Arrange
            var command = new UpdateProfileCommand(Guid.Empty, "New", "Last", "new@test.com", "newuser");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateProfileCommand(userId, "New", "Last", "new@test.com", "newuser");
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesUserAndReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FirstName = "Old", LastName = "Old", Email = "old@test.com", UserName = "olduser" };
            var command = new UpdateProfileCommand(userId, "New", "Last", "new@test.com", "newuser");
            
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.SetEmailAsync(user, command.NewEmail!))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User, string>((u, e) => u.Email = e);
            _mockUserManager.Setup(m => m.SetUserNameAsync(user, command.NewUserName!))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User, string>((u, n) => u.UserName = n);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockTokenService.Setup(s => s.CreateToken(user)).Returns("new-token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Token.Should().Be("new-token");
            result.Value.Profile!.FirstName.Should().Be("New");
            result.Value.Profile!.LastName.Should().Be("Last");
            result.Value.Profile!.Email.Should().Be("new@test.com");
            result.Value.Profile!.UserName.Should().Be("newuser");
            
            user.FirstName.Should().Be("New");
            user.LastName.Should().Be("Last");
        }

        [Fact]
        public async Task Handle_UpdateFails_ReturnsUnexpectedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FirstName = "Old", LastName = "Old", Email = "old@test.com", UserName = "olduser" };
            var command = new UpdateProfileCommand(userId, "New", "Last", "new@test.com", "newuser");
            
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.SetEmailAsync(user, command.NewEmail!)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.SetUserNameAsync(user, command.NewUserName!)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UnexpectedError);
        }
    }
}
