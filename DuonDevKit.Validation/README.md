# DuonDevKit.Validation

FluentValidation integration for `DuonDevKit.Core`'s `Result` pattern — run an `IValidator<T>` and get
back a `Result` instead of a raw `ValidationResult`, plus assembly-scanning DI registration for
validators. Requires `DuonDevKit.Core`.

For simple attribute-based checks that shouldn't need a third-party dependency, see
`DuonDevKit.Core.Validation.DataAnnotationsValidator` instead — it ships in Core itself, with no
FluentValidation reference. Reach for this package when rules need to be conditional, compare
properties against each other, or call out to a database/service.

## Installation

```bash
dotnet add package DuonDevKit.Validation
```

## Writing and registering a validator

```csharp
using FluentValidation;

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000);
    }
}
```

```csharp
using DuonDevKit.Validation.DependencyInjection;

services.AddDuonDevKitValidators(typeof(Program).Assembly);
```

The scan fails at startup when more than one validator handles the same type.

## Validating and converting to Result

```csharp
using DuonDevKit.Validation;

Result validation = validator.ValidateToResult(request);
if (validation.IsFailure)
    return validation.ToApiResult(); // DuonDevKit.AspNetCore

// or, in an async handler:
Result validation = await validator.ValidateToResultAsync(request, ct);
```

`ValidateToResult`/`ValidateToResultAsync` are named that way — not `Validate`/`ValidateAsync` — so they
don't shadow `IValidator<T>`'s own methods of those names (an extension method never wins overload
resolution against an instance method).

The resulting `Error` joins every failure into one message (`"PropertyName: ErrorMessage; ..."`) —
`Error` has no field-level structure to preserve a per-property error list. For an HTTP endpoint that
needs a field-level `{ "PropertyName": ["message"] }` response body, validate directly against
`IValidator<T>`/`ValidationResult` in the handler, or use `DuonDevKit.AspNetCore`'s
DataAnnotations-based `WithDuonDevKitValidation<T>()` Minimal API filter (see its README) if
attribute-based rules are enough for that endpoint.
