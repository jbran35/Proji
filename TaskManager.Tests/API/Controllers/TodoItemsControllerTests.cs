using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.DTOs.Requests;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.API.Controllers;

public class TodoItemsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly TodoItemsController _controller;

    public TodoItemsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new TodoItemsController(_mediatorMock.Object);
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

    #region UpdateTodoItemStatus

    [Fact]
    public async Task UpdateTodoItemStatus_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();
        var todoItemId = Guid.NewGuid();

        var result = await _controller.UpdateTodoItemStatus(todoItemId);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "User ID not found in token" });
    }

    [Fact]
    public async Task UpdateTodoItemStatus_WhenUserNotFound_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.UserNotFound, "User not found");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemStatusCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItemStatus(todoItemId);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task UpdateTodoItemStatus_WhenTodoItemNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.TodoItemNotFound, "Todo item not found");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemStatusCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItemStatus(todoItemId);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task UpdateTodoItemStatus_WhenForbidden_ReturnsForbid()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.Forbidden, "Forbidden");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemStatusCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItemStatus(todoItemId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateTodoItemStatus_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var todoItemEntry = new TodoItemEntry { Id = todoItemId };
        var expectedResult = Result<TodoItemEntry>.Success(todoItemEntry);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemStatusCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItemStatus(todoItemId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(todoItemEntry);
    }

    #endregion

    #region DeleteTodoItemAsync

    [Fact]
    public async Task DeleteTodoItemAsync_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();
        var todoItemId = Guid.NewGuid();

        var result = await _controller.DeleteTodoItemAsync(todoItemId);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "User ID not found in token" });
    }

    [Fact]
    public async Task DeleteTodoItemAsync_WhenTodoItemNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result.Failure(ErrorCode.TodoItemNotFound, "Not found");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTodoItemCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.DeleteTodoItemAsync(todoItemId);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task DeleteTodoItemAsync_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result.Success("Deleted successfully");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTodoItemCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.DeleteTodoItemAsync(todoItemId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo("Deleted successfully");
    }

    #endregion

    #region GetAssignedTodoItems

    [Fact]
    public async Task GetAssignedTodoItems_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();

        var result = await _controller.GetAssignedTodoItems();

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "User ID not found in token" });
    }

    [Fact]
    public async Task GetAssignedTodoItems_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var items = new List<TodoItemEntry> { new TodoItemEntry { Id = Guid.NewGuid() } };
        var expectedResult = Result<List<TodoItemEntry>>.Success(items);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAssignedTodoItemsQuery>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetAssignedTodoItems();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(items);
    }

    #endregion

    #region GetTodoItemDetailedView

    [Fact]
    public async Task GetTodoItemDetailedView_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();
        var todoItemId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var result = await _controller.GetTodoItemDetailedView(todoItemId, projectId);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "User ID not found in token" });
    }

    [Fact]
    public async Task GetTodoItemDetailedView_WhenTodoItemNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.TodoItemNotFound, "Not found");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTodoItemDetailedViewQuery>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetTodoItemDetailedView(todoItemId, projectId);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task GetTodoItemDetailedView_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var entry = new TodoItemEntry { Id = todoItemId };
        var expectedResult = Result<TodoItemEntry>.Success(entry);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTodoItemDetailedViewQuery>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetTodoItemDetailedView(todoItemId, projectId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(entry);
    }

    #endregion

    #region UpdateTodoItem

    [Fact]
    public async Task UpdateTodoItem_WhenUserIdIsMissing_ReturnsUnauthorized()
    {
        SetEmptyUserContext();
        var todoItemId = Guid.NewGuid();
        var request = new UpdateTodoItemRequest { ProjectId = Guid.NewGuid() };

        var result = await _controller.UpdateTodoItem(todoItemId, request);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "User ID not found in token" });
    }

    [Fact]
    public async Task UpdateTodoItem_WhenProjectNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateTodoItemRequest { ProjectId = Guid.NewGuid() };
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.ProjectNotFound, "Project not found");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItem(todoItemId, request);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task UpdateTodoItem_WhenTitleError_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateTodoItemRequest { ProjectId = Guid.NewGuid(), Title = "A" }; // invalid title
        var expectedResult = Result<TodoItemEntry>.Failure(ErrorCode.TitleError, "Title error");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItem(todoItemId, request);

        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { Message = expectedResult.ErrorMessage });
    }

    [Fact]
    public async Task UpdateTodoItem_WhenSuccess_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var todoItemId = Guid.NewGuid();
        SetUserContext(userId.ToString());
        var request = new UpdateTodoItemRequest { ProjectId = Guid.NewGuid() };
        var entry = new TodoItemEntry { Id = todoItemId };
        var expectedResult = Result<TodoItemEntry>.Success(entry);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTodoItemCommand>(), default))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UpdateTodoItem(todoItemId, request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(entry);
    }

    #endregion
}
