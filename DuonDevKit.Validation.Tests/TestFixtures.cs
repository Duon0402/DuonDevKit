using FluentValidation;

namespace DuonDevKit.Validation.Tests
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(p => p.Name).NotEmpty();
            RuleFor(p => p.Age).InclusiveBetween(1, 120);
        }
    }

    public class Product
    {
        public string Sku { get; set; } = string.Empty;
    }

    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(p => p.Sku).NotEmpty();
        }
    }

    /// <summary>A second validator for <see cref="Product"/>, used to test the duplicate-registration guard.</summary>
    public class DuplicateProductValidator : AbstractValidator<Product>
    {
        public DuplicateProductValidator()
        {
            RuleFor(p => p.Sku).MinimumLength(3);
        }
    }
}
