using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManager.Application.Users.CommandHandlers;
using TaskManager.Application.Users.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Users.CommandHandlers
{
    public class RegisterUserHandlerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly RegisterUserHandler _handler;

        public RegisterUserHandlerTests()
        {
            _mockUserManager = MockUserManager.GetMockUserManager();
            _handler = new RegisterUserHandler(_mockUserManager.Object);
        }

        [Fact]
        public async Task Handle_UserAlreadyExistsByUsername_ReturnsFailure()
        {
            // Arrange
            var command = new RegisterUserCommand("existinguser", "Pass123!", "test@test.com", "John", "Doe");
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync(new User { UserName = "existinguser", FirstName = "John", LastName = "Doe" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
            result.ErrorMessage.Should().Be("Account Already Exists");
        }

        [Fact]
        public async Task Handle_UserAlreadyExistsByEmail_ReturnsFailure()
        {
            // Arrange
            var command = new RegisterUserCommand("newuser", "Pass123!", "existing@test.com", "John", "Doe");
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(new User { UserName = "other", Email = "existing@test.com", FirstName = "John", LastName = "Doe" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
            result.ErrorMessage.Should().Be("Account Already Exists");
        }

        [Fact]
        public async Task Handle_CreationFails_ReturnsUnexpectedError()
        {
            // Arrange
            var command = new RegisterUserCommand("newuser", "Pass123!", "new@test.com", "John", "Doe");
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UnexpectedError);
            result.ErrorMessage.Should().Contain("User creation failed: Password too weak");
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var command = new RegisterUserCommand("newuser", "Pass123!", "new@test.com", "John", "Doe");
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
            result.Value.Message.Should().Be("User created successfully");
        }
    }
}
