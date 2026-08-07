# DuonDevKit

A lightweight .NET toolkit providing a Result pattern (Railway-Oriented Programming) and common
extension methods, built for .NET 8.

## Projects

- **DuonDevKit.Core** — core library: `Result`/`Result<T>`, `Error`, and extension methods.
- **DuonDevKit.Core.Tests** — xUnit test suite for `DuonDevKit.Core`.

## Features

### Result pattern

Represent the outcome of an operation explicitly, without relying on exceptions for expected
failure paths.

```csharp
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

// Non-generic Result — success/failure, no return value
Result Validate(string input)
{
    if (input.IsEmpty())
        return Error.Validation("VAL001", "Input is required."); // implicit operator

    return Result.Success();
}

// Result<T> — success carries a value, failure carries an Error
Result<int> Parse(string input)
{
    if (!int.TryParse(input, out var value))
        return Result.Fail<int>(Error.Validation("VAL002", "Not a valid number."));

    return Result.Success(value); // T inferred from the argument
}

// Pattern matching
string message = Parse("42").Match(
    onSuccess: value => $"Parsed: {value}",
    onFailure: error => $"Failed: {error.Message}");

// Functional chaining
Result<string> result = Parse("42")
    .Map(value => value * 2)
    .Bind(doubled => doubled > 0
        ? Result.Success(doubled.ToString())
        : Result.Fail<string>(Error.Business("BIZ001", "Value must be positive.")));
```

`Result<T>.Value` throws `InvalidOperationException` if accessed while the result is a failure —
always check `IsSuccess`/`IsFailure` (or use `Match`/`Map`/`Bind`) instead of accessing `Value`
directly.

#### Async chaining

`MapAsync`/`BindAsync` extend the pattern to `Task<Result<T>>`, so multiple async steps (DB calls,
HTTP calls, ...) can be chained without manually `await`-ing between each one:

```csharp
Result<decimal> total = await GetOrderAsync(orderId)         // Task<Result<Order>>
    .BindAsync(o => ValidateOrderAsync(o))                   // async step that can itself fail
    .MapAsync(o => o.Total);                                 // sync transform on the final value
```

Both `MapAsync` and `BindAsync` short-circuit on failure — if any step in the chain fails, the
remaining steps are skipped and the original error propagates.

### Error

`Error` is a record with static factory helpers for each `ErrorType`:

```csharp
Error.None
Error.Validation(code, message)
Error.Business(code, message)
Error.NotFound(code, message)
Error.Conflict(code, message)
Error.Unauthorized(code, message)
Error.Forbidden(code, message)
Error.Unexpected(code, message)
```

`Error.ToHttpStatusCode()` maps an error to its corresponding `System.Net.HttpStatusCode`
(e.g. `Validation` → `BadRequest`, `NotFound` → `NotFound`, `Unexpected` → `InternalServerError`),
useful when converting a `Result` into an HTTP response:

```csharp
HttpStatusCode status = error.ToHttpStatusCode();
```

### Extensions

`StringExtensions` adds common string checks:

```csharp
"".IsEmpty();                       // true
"a@b.com".IsEmail();                // true
"ABC".EqualsIgnoreCase("abc");      // true
```

`EnumerableExtensions` adds common collection checks and membership tests:

```csharp
((IEnumerable<int>?)null).IsEmpty();    // true
new List<int> { 1, 2, 3 }.IsNotEmpty(); // true
2.In(1, 2, 3);                          // true
4.NotIn(1, 2, 3);                       // true
```

## Getting started

Requires the .NET 8 SDK.

```bash
dotnet build DuonDevKit.slnx
dotnet test DuonDevKit.slnx
```

## Contributing

Changes are expected to keep the build warning-free and to include unit tests in
`DuonDevKit.Core.Tests` for new or changed behavior.
