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

    /// <summary>Dedicated model for the non-blocking-severity test, kept separate from <see cref="Person"/> so its validator doesn't collide with <see cref="PersonValidator"/> in the whole-assembly-scan tests.</summary>
    public class Note
    {
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>Validator whose rule is non-blocking, used to test that <c>Severity.Warning</c>/<c>Info</c> don't fail the resulting <c>Result</c>.</summary>
    public class NoteValidator : AbstractValidator<Note>
    {
        public NoteValidator()
        {
            RuleFor(n => n.Text).MinimumLength(3).WithSeverity(Severity.Warning);
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

    /// <summary>Open (unbound) generic validator, used to test that the scan skips generic type definitions.</summary>
    public class GenericValidator<T> : AbstractValidator<T>
    {
    }
}
