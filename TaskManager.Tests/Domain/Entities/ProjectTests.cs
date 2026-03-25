using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Domain.Entities
{
    public class ProjectTests
    {
        private readonly Title _validTitle;
        private readonly Description _validDescription;
        private readonly Guid _validOwnerId;

        public ProjectTests()
        {
            _validTitle = Title.Create("Test Title").Value;
            _validDescription = Description.Create("Test Description").Value;
            _validOwnerId = Guid.NewGuid();
        }

        [Fact]
        public void Create_WithValidParameters_ReturnsSuccessResult()
        {
            // Act
            var result = Project.Create(_validTitle, _validDescription, _validOwnerId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(_validTitle);
            result.Value.Description.Should().Be(_validDescription);
            result.Value.OwnerId.Should().Be(_validOwnerId);
            result.Value.Status.Should().Be(Status.Incomplete);
        }

        [Fact]
        public void Create_WithEmptyOwnerId_ReturnsFailureResult()
        {
            // Act
            var result = Project.Create(_validTitle, _validDescription, Guid.Empty);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public void MarkAsComplete_WhenIncomplete_SetsStatusToComplete()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;

            // Act
            var result = project.MarkAsComplete();

            // Assert
            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(Status.Complete);
        }

        [Fact]
        public void MarkAsComplete_WhenDeleted_ReturnsFailureResult()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;
            project.MarkAsDeleted();

            // Act
            var result = project.MarkAsComplete();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.ProjectNotFound);
        }

        [Fact]
        public void AddTodoItem_WithValidItem_ReturnsSuccessResult()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, project.Id, null, Priority.None, null).Value;

            // Act
            var result = project.AddTodoItem(todoItem);

            // Assert
            result.IsSuccess.Should().BeTrue();
            project.TodoItems.Should().Contain(todoItem);
        }

        [Fact]
        public void AddTodoItem_WithNullItem_ReturnsFailureResult()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;

            // Act
            var result = project.AddTodoItem(null);

            // Assert
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void AddTodoItem_WithDifferentOwner_ReturnsFailureResult()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;
            var todoItem = TodoItem.Create(_validTitle, _validDescription, Guid.NewGuid(), project.Id, null, Priority.None, null).Value;

            // Act
            var result = project.AddTodoItem(todoItem);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        }
        
        [Fact]
        public void DeleteTodoItem_WithValidItem_ReturnsSuccessResult()
        {
            // Arrange
            var project = Project.Create(_validTitle, _validDescription, _validOwnerId).Value;
            var todoItem = TodoItem.Create(_validTitle, _validDescription, _validOwnerId, project.Id, null, Priority.None, null).Value;
            project.AddTodoItem(todoItem);

            // Act
            var result = project.DeleteTodoItem(todoItem);

            // Assert
            result.IsSuccess.Should().BeTrue();
            project.TodoItems.Should().NotContain(todoItem);
            todoItem.Status.Should().Be(Status.Deleted);
        }
    }
}