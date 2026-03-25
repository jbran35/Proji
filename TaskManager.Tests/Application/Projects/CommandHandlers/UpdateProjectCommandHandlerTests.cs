using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Projects.CommandHandlers;
using TaskManager.Application.Projects.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Projects.CommandHandlers
{
    public class UpdateProjectCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly UpdateProjectCommandHandler _handler;

        public UpdateProjectCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);
            _mockUserManager = MockUserManager.GetMockUserManager();
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<UpdateProjectCommandHandler>>();

            _handler = new UpdateProjectCommandHandler(
                _mockUnitOfWork.Object,
                _mockUserManager.Object,
                _mockCache.Object,
                mockLogger.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var command = new UpdateProjectCommand(userId, Guid.NewGuid(), Title.Create("New").Value, Description.Create("New").Value);
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
            var command = new UpdateProjectCommand(userId, projectId, Title.Create("New").Value, Description.Create("New").Value);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

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
            var command = new UpdateProjectCommand(userId, projectId, Title.Create("New").Value, Description.Create("New").Value);
            var project = Project.Create(Title.Create("Old").Value, Description.Create("Old").Value, ownerId).Value;

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesProjectAndInvalidatesCache()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var newTitle = Title.Create("New Title").Value;
            var newDesc = Description.Create("New Desc").Value;
            var command = new UpdateProjectCommand(userId, projectId, newTitle, newDesc);
            var project = Project.Create(Title.Create("Old").Value, Description.Create("Old").Value, userId).Value;
            var assigneeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(new User { Id = userId, FirstName = "A", LastName = "B" });
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
            _mockProjectRepository.Setup(r => r.GetProjectIncompleteTodoItemAssigneeIds(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(assigneeIds);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            project.Title.Should().Be(newTitle);
            project.Description.Should().Be(newDesc);
            _mockProjectRepository.Verify(r => r.Update(project), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
