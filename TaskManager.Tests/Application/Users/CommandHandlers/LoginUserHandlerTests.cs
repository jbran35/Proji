using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.CommandHandlers;
using TaskManager.Application.Users.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Users.CommandHandlers
{
    public class LoginUserHandlerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly LoginUserHandler _handler;

        public LoginUserHandlerTests()
        {
            _mockUserManager = MockUserManager.GetMockUserManager();
            
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null!, null!, null!, null!);

            _mockTokenService = new Mock<ITokenService>();
            
            _handler = new LoginUserHandler(_mockUserManager.Object, _mockSignInManager.Object, _mockTokenService.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsAuthError()
        {
            // Arrange
            var command = new LoginUserCommand("nonexistent", "password");
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.AuthError);
            result.ErrorMessage.Should().Be("Invalid Credentials");
        }

        [Fact]
        public async Task Handle_InvalidPassword_ReturnsUnexpectedError()
        {
            // Arrange
            var user = new User { UserName = "testuser", FirstName = "Test", LastName = "User" };
            var command = new LoginUserCommand("testuser", "wrongpassword");
            
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetSecurityStampAsync(user)).ReturnsAsync("stamp");
            
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UnexpectedError);
            result.ErrorMessage.Should().Be("Unexpected Error Logging In");
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            var user = new User { UserName = "testuser", FirstName = "Test", LastName = "User" };
            var command = new LoginUserCommand("testuser", "correctpassword");
            var expectedToken = "generated-jwt-token";
            
            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetSecurityStampAsync(user)).ReturnsAsync("stamp");
            
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
                .ReturnsAsync(SignInResult.Success);
            
            _mockTokenService.Setup(s => s.CreateToken(user)).Returns(expectedToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Token.Should().Be(expectedToken);
        }
    }
}
