using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Domain.Entities
{
    public class TodoItemTests
    {
        private readonly Title _validTitle;
        private readonly Description _validDescription;
        private readonly Guid _validOwnerId;
        private readonly Guid _validProjectId;

        public TodoItemTests()
        {
            _validTitle = Title.Create("Test Title").Value;
            _validDescription = Description.Create("Test Description").Value;
            _validOwnerId = Guid.NewGuid();
            _validProjectId = Guid.NewGuid();
        }

        [Fact]
        public void Create_WithValidParameters_ReturnsSuccessResult()
        {
            // Act
            var result = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, _validProjectId, null, Priority.High, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(_validTitle);
            result.Value.Description.Should().Be(_validDescription);
            result.Value.OwnerId.Should().Be(_validOwnerId);
            result.Value.ProjectId.Should().Be(_validProjectId);
            result.Value.Priority.Should().Be(Priority.High);
            result.Value.Status.Should().Be(Status.Incomplete);
        }

        [Fact]
        public void Create_WithEmptyProjectId_ReturnsFailureResult()
        {
            // Act
            var result = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, Guid.Empty, null, Priority.None, null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public void UpdateProjectAssignment_WithValidId_ReturnsSuccessResult()
        {
            // Arrange
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, _validProjectId, null, Priority.None, null).Value;
            var newProjectId = Guid.NewGuid();

            // Act
            var result = todoItem.UpdateProjectAssignment(newProjectId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.ProjectId.Should().Be(newProjectId);
        }

        [Fact]
        public void AssignToUser_WithValidId_ReturnsSuccessResult()
        {
            // Arrange
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, _validProjectId, null, Priority.None, null).Value;
            var userId = Guid.NewGuid();

            // Act
            var result = todoItem.AssignToUser(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.AssigneeId.Should().Be(userId);
        }

        [Fact]
        public void Unassign_ReturnsSuccessResult()
        {
            // Arrange
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, _validProjectId, Guid.NewGuid(), Priority.None, null).Value;

            // Act
            var result = todoItem.Unassign();

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.AssigneeId.Should().BeNull();
            todoItem.Assignee.Should().BeNull();
        }

        [Fact]
        public void UpdatePriority_WithValidPriority_ReturnsSuccessResult()
        {
            // Arrange
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, _validProjectId, null, Priority.None, null).Value;

            // Act
            var result = todoItem.UpdatePriority(Priority.High);

            // Assert
            result.IsSuccess.Should().BeTrue();
            todoItem.Priority.Should().Be(Priority.High);
        }
    }
}