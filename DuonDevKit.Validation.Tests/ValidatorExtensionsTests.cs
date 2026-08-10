namespace DuonDevKit.Validation.Tests
{
    public class ValidatorExtensionsTests
    {
        [Fact]
        public void Validate_ValidInstance_ReturnsSuccess()
        {
            var validator = new PersonValidator();
            var person = new Person { Name = "Alice", Age = 30 };

            var result = validator.ValidateToResult(person);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Validate_InvalidInstance_ReturnsFailureJoiningEveryPropertyError()
        {
            var validator = new PersonValidator();
            var person = new Person { Name = "", Age = 0 };

            var result = validator.ValidateToResult(person);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCodes.ValidationFailed, result.Error.Code);
            Assert.Contains("Name", result.Error.Message);
            Assert.Contains("Age", result.Error.Message);
        }

        [Fact]
        public async Task ValidateAsync_InvalidInstance_ReturnsFailure()
        {
            var validator = new PersonValidator();
            var person = new Person { Name = "", Age = 200 };

            var result = await validator.ValidateToResultAsync(person);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task ValidateAsync_ValidInstance_ReturnsSuccess()
        {
            var validator = new PersonValidator();
            var person = new Person { Name = "Bob", Age = 40 };

            var result = await validator.ValidateToResultAsync(person);

            Assert.True(result.IsSuccess);
        }
    }
}
