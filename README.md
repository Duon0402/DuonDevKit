# DuonDevKit

A lightweight .NET toolkit providing a Result pattern (Railway-Oriented Programming) and common
extension methods, built for .NET 8.

## Projects

- **DuonDevKit.Core** — core library: `Result`/`Result<T>`, `Error`, and extension methods.
- **DuonDevKit.Core.Tests** — xUnit test suite for `DuonDevKit.Core`.
- **DuonDevKit.EntityFrameworkCore** — Result-based Repository/UnitOfWork pattern for EF Core, plus
  automatic audit-field population (created/updated/soft-deleted by + at).
- **DuonDevKit.EntityFrameworkCore.Tests** — xUnit test suite using EF Core's InMemory provider.

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

### EntityFrameworkCore (Repository/UnitOfWork + audit)

`Repository<T>`/`UnitOfWork` wrap EF Core in the same `Result` pattern — no exceptions for expected
failure paths, and no base `Entity<TId>` requirement:

```csharp
using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Repositories;

var repository = new Repository<Order>(dbContext);
var unitOfWork = new UnitOfWork(dbContext);

Result<Order> order = await repository.GetByIdAsync([orderId]);
Result<IReadOnlyList<Order>> pending = await repository.ListAsync(o => o.Status == "Pending");

var added = await repository.AddAsync(new Order { /* ... */ });
Result saveResult = await unitOfWork.SaveChangesAsync(); // DbUpdateException -> Result.Fail, never throws
```

Entities inheriting `BaseEntity<TId>` (or the non-generic `BaseEntity` for `string` ids) get a typed
`GetByIdAsync(TId id)` via `Repository<T, TId>`, alongside the untyped `object[] keyValues` overload
on `Repository<T>`.

#### Audit fields

Opt an entity into automatic audit tracking by implementing one or more marker interfaces:

```csharp
public class Order : ICanCreate, ICanUpdate, ISoftDelete
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

Register `AuditSaveChangesInterceptor` on your `DbContext` (it never touches `HttpContext` directly —
implement `ICurrentUserProvider` to supply the acting user's id from wherever your app tracks it):

```csharp
optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider));
```

On every save, the interceptor fills `CreatedAt`/`CreatedBy` on new entities (only if still at their
default value), and refreshes `UpdatedAt`/`UpdatedBy`/`DeletedAt`/`DeletedBy` on modified entities
(only if the caller didn't already set them explicitly). `Repository<T>.Remove(entity)` automatically
soft-deletes (`IsDeleted = true`) instead of hard-deleting when `entity` implements `ISoftDelete`.

Call `modelBuilder.ApplySoftDeleteQueryFilter()` once in your `DbContext.OnModelCreating` to exclude
soft-deleted rows from queries by default (`.IgnoreQueryFilters()` includes them when needed).

## Getting started

Requires the .NET 8 SDK.

```bash
dotnet build DuonDevKit.slnx
dotnet test DuonDevKit.slnx
```

## Contributing

Changes are expected to keep the build warning-free and to include unit tests in
`DuonDevKit.Core.Tests` for new or changed behavior.
