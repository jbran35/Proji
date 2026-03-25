using FluentAssertions;
using TaskManager.Domain.Enums;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Domain.ValueObjects
{
    public class TitleTests
    {
        [Fact]
        public void Create_WithValidString_ReturnsSuccessResult()
        {
            // Arrange
            var validString = "Valid Title";

            // Act
            var result = Title.Create(validString);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(validString);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullString_ReturnsFailureResult(string? invalidString)
        {
            // Act
            if (invalidString != null)
            {
                var result = Title.Create(invalidString);

                // Assert
                result.IsFailure.Should().BeTrue();
                result.ErrorCode.Should().Be(ErrorCode.TitleError);
                result.ErrorMessage.Should().Be("Title cannot be empty.");
            }
        }

        [Fact]
        public void Create_WithTooLongString_ReturnsFailureResult()
        {
            // Arrange
            var tooLongString = new string('a', 201);

            // Act
            var result = Title.Create(tooLongString);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.TitleError);
            result.ErrorMessage.Should().Be("Title cannot exceed 200 characters.");
        }

        [Fact]
        public void ImplicitOperator_ReturnsStringValue()
        {
            // Arrange
            var validString = "Valid Title";
            var title = Title.Create(validString).Value;

            // Act
            string result = title;

            // Assert
            result.Should().Be(validString);
        }
    }
}