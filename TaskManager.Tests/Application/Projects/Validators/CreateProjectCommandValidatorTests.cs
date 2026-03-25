using FluentAssertions;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.Validators;

namespace TaskManager.Tests.Application.Projects.Validators
{
    public class CreateProjectCommandValidatorTests
    {
        private readonly CreateProjectCommandValidator _validator = new();

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
        {
            // Arrange
            var command = new CreateProjectCommand(Guid.NewGuid(), "Valid Title", "Valid Description");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldHaveError()
        {
            // Arrange
            var command = new CreateProjectCommand(Guid.Empty, "Valid Title", "Valid Description");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "UserId" && e.ErrorMessage == "Your ID Is Required To Create A Project");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_WhenTitleIsNullOrEmpty_ShouldHaveError(string? invalidTitle)
        {
            // Arrange
            if (invalidTitle != null)
            {
                var command = new CreateProjectCommand(Guid.NewGuid(), invalidTitle, "Valid Description");

                // Act
                var result = _validator.Validate(command);

                // Assert
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "A Title Is Required To Create A Project");
            }
        }
    }
}