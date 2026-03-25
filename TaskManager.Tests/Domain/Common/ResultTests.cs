using FluentAssertions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Domain.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_ReturnsResultWithIsSuccessTrue()
        {
            // Act
            var result = Result.Success("Test Success");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.SuccessMessage.Should().Be("Test Success");
            result.ErrorMessage.Should().BeNull();
            result.ErrorCode.Should().Be(ErrorCode.None);
        }

        [Fact]
        public void Failure_ReturnsResultWithIsSuccessFalse()
        {
            // Act
            var result = Result.Failure(ErrorCode.DomainRuleViolation, "Test Error");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.SuccessMessage.Should().BeNull();
            result.ErrorMessage.Should().Be("Test Error");
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }
        
        [Fact]
        public void GenericSuccess_ReturnsResultWithIsSuccessTrueAndValue()
        {
            // Act
            var result = Result<int>.Success(42, "Test Success");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.SuccessMessage.Should().Be("Test Success");
            result.ErrorMessage.Should().BeEmpty();
            result.ErrorCode.Should().Be(ErrorCode.None);
            result.Value.Should().Be(42);
        }

        [Fact]
        public void GenericFailure_ReturnsResultWithIsSuccessFalse()
        {
            // Act
            var result = Result<int>.Failure(ErrorCode.DomainRuleViolation, "Test Error");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.SuccessMessage.Should().BeEmpty();
            result.ErrorMessage.Should().Be("Test Error");
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }

        [Fact]
        public void GenericValue_WhenFailure_ThrowsInvalidOperationException()
        {
            // Arrange
            var result = Result<int>.Failure(ErrorCode.DomainRuleViolation, "Test Error");

            // Act
            Action act = () => { var val = result.Value; };

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The value of a failure result can't be accessed.");
        }
    }
}