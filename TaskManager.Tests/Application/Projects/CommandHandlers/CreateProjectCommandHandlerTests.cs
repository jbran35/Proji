using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Projects.CommandHandlers;
using TaskManager.Application.Projects.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;
using TaskManager.Tests.Application.Mocks;

namespace TaskManager.Tests.Application.Projects.CommandHandlers
{
    public class CreateProjectCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly CreateProjectCommandHandler _handler;

        public CreateProjectCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepository.Object);

            _mockUserManager = MockUserManager.GetMockUserManager();
            var mockLogger = new Mock<ILogger<CreateProjectCommandHandler>>();

            _handler = new CreateProjectCommandHandler(_mockUnitOfWork.Object, _mockUserManager.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var command = new CreateProjectCommand(Guid.NewGuid(), "Title", "Description");
            _mockUserManager.Setup(m => m.FindByIdAsync(command.UserId.ToString())).ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task Handle_InvalidTitle_ReturnsFailureResult()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var command = new CreateProjectCommand(user.Id, "", "Description"); // Invalid title
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResultAndSavesProject()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
            var command = new CreateProjectCommand(user.Id, "Valid Title", "Valid Description");
            
            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(command.Title);
            
            _mockProjectRepository.Verify(r => r.Add(It.IsAny<Project>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}