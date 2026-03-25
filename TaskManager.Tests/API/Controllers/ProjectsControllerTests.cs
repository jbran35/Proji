using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TaskManager.API.Controllers;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.DTOs.Requests;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.API.Controllers
{
    public class ProjectsControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly ProjectsController _controller;
        private readonly Guid _userId;

        public ProjectsControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new ProjectsController(_mockMediator.Object);
            _userId = Guid.NewGuid();

            var user = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            ], "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new CreateProjectRequest { Title = "Title", Description = "Description" };
            var expectedDto = new ProjectTileDto { Id = Guid.NewGuid(), Title = "Title", OwnerId = _userId };
            
            _mockMediator.Setup(m => m.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result<ProjectTileDto>.Success(expectedDto));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var actionResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            actionResult.ActionName.Should().Be("GetProjectDetailedView");
            actionResult.RouteValues!["projectId"].Should().Be(expectedDto.Id);
            actionResult.Value.Should().BeEquivalentTo(expectedDto);
        }

        [Fact]
        public async Task Create_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CreateProjectRequest { Title = "Title", Description = "Description" };
            
            _mockMediator.Setup(m => m.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result<ProjectTileDto>.Failure(ErrorCode.UserNotFound, "User not found"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Create_WhenDomainRuleViolation_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateProjectRequest { Title = "", Description = "Description" }; // Invalid title
            
            _mockMediator.Setup(m => m.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result<ProjectTileDto>.Failure(ErrorCode.DomainRuleViolation, "Invalid title"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
        
        [Fact]
        public async Task Complete_WhenSuccessful_ReturnsOkResult()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var expectedDto = new ProjectTileDto { Id = projectId, Title = "Title", OwnerId = _userId };
            var expectedResponse = new TaskManager.Application.Projects.DTOs.Responses.CompleteProjectResponse(expectedDto);
            
            _mockMediator.Setup(m => m.Send(It.IsAny<CompleteProjectCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Result<TaskManager.Application.Projects.DTOs.Responses.CompleteProjectResponse>.Success(expectedResponse));

            // Act
            var result = await _controller.Complete(projectId);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().BeEquivalentTo(expectedResponse);
        }
    }
}