using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManager.Application.Users.QueryHandlers;
using TaskManager.Application.Users.Queries;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Entities;

namespace TaskManager.Tests.Application.Users.QueryHandlers
{
    public class GetUserQueryHandlerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly GetUserQueryHandler _handler;

        public GetUserQueryHandlerTests()
        {
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _handler = new GetUserQueryHandler(_mockUserManager.Object);
        }

        [Fact]
        public async Task Handle_ReturnsFailure_WhenEmailIsInvalid()
        {
            // Arrange
            var query = new GetUserQuery("");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public async Task Handle_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "notfound@test.com";
            var query = new GetUserQuery(email);
            _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenUserExists()
        {
            // Arrange
            var email = "user@test.com";
            var user = new User { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Email = email };
            var query = new GetUserQuery(email);
            _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Email.Should().Be(email);
            result.Value.FirstName.Should().Be("Test");
            result.Value.LastName.Should().Be("User");
        }
    }
}
