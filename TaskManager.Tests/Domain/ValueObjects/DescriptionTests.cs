using FluentAssertions;
using TaskManager.Domain.Enums;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Tests.Domain.ValueObjects
{
    public class DescriptionTests
    {
        [Fact]
        public void Create_WithValidString_ReturnsSuccessResult()
        {
            // Arrange
            var validString = "Valid Description";

            // Act
            var result = Description.Create(validString);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(validString);
        }

        [Fact]
        public void Create_WithEmptyString_ReturnsSuccessResult()
        {
            // Act
            var result = Description.Create("");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("");
        }

        [Fact]
        public void Create_WithWhitespaceString_ReturnsSuccessResult()
        {
            // Act
            var result = Description.Create(" ");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(" ");
        }

        [Fact]
        public void Create_WithTooLongString_ReturnsFailureResult()
        {
            // Arrange
            var tooLongString = new string('a', 2001);

            // Act
            var result = Description.Create(tooLongString);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DescriptionError);
        }

        [Fact]
        public void ImplicitOperator_ReturnsStringValue()
        {
            // Arrange
            var validString = "Valid Description";
            var description = Description.Create(validString).Value;

            // Act
            string result = description;

            // Assert
            result.Should().Be(validString);
        }
    }
}