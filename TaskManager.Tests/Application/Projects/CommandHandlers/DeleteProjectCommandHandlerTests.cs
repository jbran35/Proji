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
    public class DeleteProjectCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly DeleteProjectCommandHandler _handler;

        public DeleteProjectCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);

            _mockUserManager = MockUserManager.GetMockUserManager();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<DeleteProjectCommandHandler>>();

            _handler = new DeleteProjectCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockCache.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var command = new DeleteProjectCommand(Guid.NewGuid(), Guid.NewGuid());
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var command = new DeleteProjectCommand(user.Id, Guid.NewGuid());
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((Project?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public async Task Handle_UserNotProjectOwner_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var otherUserId = Guid.NewGuid();
            var title = Title.Create("Title").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, otherUserId).Value;
            var command = new DeleteProjectCommand(user.Id, project.Id);
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(project);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResultAndDeletesProject()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var title = Title.Create("Title").Value;
            var desc = Description.Create("Desc").Value;
            var project = Project.Create(title, desc, user.Id).Value;
            var command = new DeleteProjectCommand(user.Id, project.Id);
            var assigneeIds = new List<Guid>();

            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockProjectRepository.Setup(r => r.GetProjectWithoutTasksAsync(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(project);
            _mockProjectRepository.Setup(r => r.GetProjectIncompleteTodoItemAssigneeIds(command.ProjectId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(assigneeIds);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockProjectRepository.Verify(r => r.Delete(project), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}