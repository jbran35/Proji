using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Projects.CommandHandlers;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.Events;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Projects.CommandHandlers
{
    public class CompleteProjectCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMediator> _mockMediator;
        private readonly CompleteProjectCommandHandler _handler;

        public CompleteProjectCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockUserManager = MockUserManager.GetMockUserManager();
            _mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<CompleteProjectCommandHandler>>();
            
            _handler = new CompleteProjectCommandHandler(
                _mockUnitOfWork.Object, 
                _mockUserManager.Object, 
                mockLogger.Object, 
                _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var command = new CompleteProjectCommand(userId, Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var command = new CompleteProjectCommand(userId, projectId);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var command = new CompleteProjectCommand(userId, projectId);
            var project = Project.Create(Title.Create("Title").Value, Description.Create("Description").Value, ownerId).Value;
            
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidRequest_CompletesProjectAndPublishesEvent()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var command = new CompleteProjectCommand(userId, projectId);
            var project = Project.Create(Title.Create("Title").Value, Description.Create("Description").Value, userId).Value;
            
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(Status.Complete);
            _mockProjectRepository.Verify(r => r.Update(project), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ProjectCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
