using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Guards;

namespace DuonDevKit.Core.Tests.Guards
{
    public class GuardTests
    {
        [Fact]
        public void Null_WithNullValue_ReturnsFailure()
        {
            var result = Guard.Against.Null(null, "customerName");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Error.Type);
        }

        [Fact]
        public void Null_WithNonNullValue_ReturnsSuccess()
        {
            var result = Guard.Against.Null("value", "customerName");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void NullOrEmpty_WithNull_ReturnsFailure()
        {
            var result = Guard.Against.NullOrEmpty(null, "customerName");

            Assert.True(result.IsFailure);
        }

        [Fact]
        public void NullOrEmpty_WithEmptyOrWhitespace_ReturnsFailure()
        {
            Assert.True(Guard.Against.NullOrEmpty("", "customerName").IsFailure);
            Assert.True(Guard.Against.NullOrEmpty("   ", "customerName").IsFailure);
        }

        [Fact]
        public void NullOrEmpty_WithNonEmptyValue_ReturnsSuccess()
        {
            var result = Guard.Against.NullOrEmpty("Alice", "customerName");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void NegativeOrZero_WithZeroOrNegative_ReturnsFailure()
        {
            Assert.True(Guard.Against.NegativeOrZero(0, "quantity").IsFailure);
            Assert.True(Guard.Against.NegativeOrZero(-1, "quantity").IsFailure);
        }

        [Fact]
        public void NegativeOrZero_WithPositiveValue_ReturnsSuccess()
        {
            var result = Guard.Against.NegativeOrZero(1, "quantity");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Negative_WithNegativeValue_ReturnsFailure()
        {
            var result = Guard.Against.Negative(-1, "quantity");

            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Negative_WithZeroOrPositiveValue_ReturnsSuccess()
        {
            Assert.True(Guard.Against.Negative(0, "quantity").IsSuccess);
            Assert.True(Guard.Against.Negative(1, "quantity").IsSuccess);
        }
    }
}
