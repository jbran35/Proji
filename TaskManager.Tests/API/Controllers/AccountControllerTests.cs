using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.API.DTOs.Account;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Application.UserConnections.DTOs.Requests;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.DTOs;
using TaskManager.Application.Users.DTOs.Requests;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.API.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AccountController(_mediatorMock.Object);
    }

    private void SetUserContext(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetEmptyUserContext()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    #region Login

    [Fact]
    public async Task Login_WhenSuccess_ReturnsOk()
    {
        // Arrange
        var command = new LoginUserCommand("testuser", "Password123!");
        var expectedResponse = new LoginUserResponse { Token = "test-token" };
        var expectedResult = Result<LoginUserResponse>.Success(expectedResponse);

        _mediatorMock.Setup(m => m.Send(command, default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(command);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Login_WhenAuthError_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginUserCommand("testuser", "WrongPassword");
        var expectedResult = Result<LoginUserResponse>.Failure(ErrorCode.AuthError, "Invalid credentials");

        _mediatorMock.Setup(m => m.Send(command, default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(command);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "Invalid credentials" });
    }

    [Fact]
    public async Task Login_WhenOtherError_ReturnsBadRequest()
    {
        // Arrange
        var command = new LoginUserCommand("testuser", "Password123!");
        var expectedResult = Result<LoginUserResponse>.Failure(ErrorCode.DomainRuleViolation, "Some error");

        _mediatorMock.Setup(m => m.Send(command, default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(command);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { Code = ErrorCode.DomainRuleViolation, Message = "Some error" });
    }

    #endregion

    #region Registration

    [Fact]
    public async Task Register_WhenSuccess_ReturnsOk()
    {
        // Arrange
        var model = new RegisterModel
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };
        var expectedResponse = new RegisterUserResponse { Success = true, Message = "User registered" };
        var expectedResult = Result<RegisterUserResponse>.Success(expectedResponse);

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(model);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Register_WhenModelIsNull_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Register(null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WhenFailure_ReturnsBadRequest()
    {
        // Arrange
        var model = new RegisterModel { Username = "test", Email = "test@test.com", Password = "123", FirstName = "F", LastName = "L" };
        var expectedResult = Result<RegisterUserResponse>.Failure(ErrorCode.DomainRuleViolation, "Registration failed");

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(model);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { Code = ErrorCode.DomainRuleViolation, Message = "Registration failed" });
    }

    #endregion

    #region UpdateUserInfo

    [Fact]
    public async Task UpdateUserInfo_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();
        var request = new UpdateProfileRequest { Id = Guid.NewGuid(), FirstName = "New" };

        var result = await _controller.UpdateUserInfo(request);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task UpdateUserInfo_WhenIdMismatch_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateProfileRequest { Id = Guid.NewGuid(), FirstName = "New" }; // Different ID

        SetUserContext(Guid.Empty.ToString());
        
        var result = await _controller.UpdateUserInfo(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateUserInfo_WhenUserNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateProfileRequest { Id = userId, FirstName = "New" };
        var expectedResult = Result<UpdateProfileResponse>.Failure(ErrorCode.UserNotFound, "User not found");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProfileCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateUserInfo(request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateUserInfo_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateProfileRequest { Id = userId, FirstName = "New", LastName = "Last", Email = "new@test.com", UserName = "newuser" };
        var profile = new UserProfileDto(userId, "New", "Last", "new@test.com", "newuser");
        var expectedResponse = new UpdateProfileResponse { Profile = profile, Token = "new-token" };
        var expectedResult = Result<UpdateProfileResponse>.Success(expectedResponse);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProfileCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateUserInfo(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    #endregion

    #region Assignees

    [Fact]
    public async Task AddAssignee_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var assigneeId = Guid.NewGuid();
        var request = new CreateUserConnectionRequest { AssigneeId = assigneeId };
        var expectedDto = new UserConnectionDto { Id = Guid.NewGuid(), AssigneeId = assigneeId };
        var expectedResult = Result<UserConnectionDto>.Success(expectedDto);

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserConnectionCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.AddAssignee(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task DeleteAssigneeAsync_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var connectionId = Guid.NewGuid();
        var expectedResult = Result.Success("Deleted");

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUserConnectionCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.DeleteAssigneeAsync(connectionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    #endregion
}
