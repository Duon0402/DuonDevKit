using System.ComponentModel.DataAnnotations;
using DuonDevKit.Core.Validation;

namespace DuonDevKit.Core.Tests.Validation
{
    public class DataAnnotationsValidatorTests
    {
        private class Person
        {
            [Required, MaxLength(10)]
            public string? Name { get; set; }

            [Range(1, 120)]
            public int Age { get; set; }
        }

        private class RangeCheckedPeriod : IValidatableObject
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext)
            {
                if (End <= Start)
                    yield return new System.ComponentModel.DataAnnotations.ValidationResult("End must be after Start.", [nameof(End)]);
            }
        }

        [Fact]
        public void Validate_ValidInstance_ReturnsSuccess()
        {
            var person = new Person { Name = "Alice", Age = 30 };

            var result = DataAnnotationsValidator.Validate(person);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Validate_MissingRequiredProperty_ReturnsFailure()
        {
            var person = new Person { Name = null, Age = 30 };

            var result = DataAnnotationsValidator.Validate(person);

            Assert.True(result.IsFailure);
            Assert.Equal(ValidationErrorCodes.Invalid, result.Error.Code);
            Assert.Contains("Name", result.Error.Message);
        }

        [Fact]
        public void Validate_ValueOutsideRange_ReturnsFailure()
        {
            var person = new Person { Name = "Bob", Age = 999 };

            var result = DataAnnotationsValidator.Validate(person);

            Assert.True(result.IsFailure);
            Assert.Contains("Age", result.Error.Message);
        }

        [Fact]
        public void Validate_MultipleViolations_JoinsEveryOneIntoTheMessage()
        {
            var person = new Person { Name = null, Age = 999 };

            var result = DataAnnotationsValidator.Validate(person);

            Assert.Contains("Name", result.Error.Message);
            Assert.Contains("Age", result.Error.Message);
        }

        [Fact]
        public void Validate_ImplementsIValidatableObject_RunsCustomValidateToo()
        {
            var period = new RangeCheckedPeriod { Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(-1) };

            var result = DataAnnotationsValidator.Validate(period);

            Assert.True(result.IsFailure);
            Assert.Contains("End must be after Start.", result.Error.Message);
        }

        [Fact]
        public void Validate_NullInstance_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DataAnnotationsValidator.Validate(null!));
        }
    }
}
