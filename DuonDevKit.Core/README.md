# DuonDevKit.Core

Core primitives for explicit, railway-oriented error handling in .NET (`net8.0`/`net9.0`): `Result`, `Error`, `Option`, guard clauses, object mapping, password hashing, and common extensions.

## Installation

```bash
dotnet add package DuonDevKit.Core
```

## Result and Error

```csharp
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

Result<int> Parse(string input)
{
    if (!int.TryParse(input, out var value))
        return Error.Validation("VAL001", "Not a valid number.");

    return Result.Success(value);
}

string message = Parse("42").Match(
    onSuccess: value => $"Parsed: {value}",
    onFailure: error => $"Failed: {error.Message}");
```

Use `Match`, `Map`, and `Bind` to avoid accessing `Result<T>.Value` until success is known. `MapAsync`, `BindAsync`, and `EnsureAsync` support the same flow for `Task<Result<T>>` and short-circuit on failure.

```csharp
Result<string> result = Parse("42")
    .Ensure(value => value > 0, Error.Validation("VAL002", "Must be positive."))
    .Map(value => value.ToString());
```

`Error` has factory helpers including `Validation`, `Business`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, and `Unexpected`. Call `error.ToHttpStatusCode()` when an HTTP status code is needed.

## Dependency injection setup

Most Core types are used directly and need no service registration. Register mapping implementations only when using the mapping APIs:

```csharp
using DuonDevKit.Core.Mapping;

services.AddDuonDevKitMappers(typeof(Program).Assembly);
```

The scan fails at startup when more than one mapper handles the same source/destination pair.

## Option

Use `Option<T>` when a value may be absent but absence is not an error.

```csharp
Option<User> user = users.TryGetValue(id, out var value) ? value : Option<User>.None;
Result<User> requiredUser = user.ToResult(Error.NotFound("USER001", "User not found."));
```

## Guards

Guards return a failed `Result` rather than throw for expected validation failures.

```csharp
Result validation = Result.Combine(
    Guard.Against.NullOrEmpty(name, nameof(name)),
    Guard.Against.NegativeOrZero(quantity, nameof(quantity)));
```

## Mapping and security

Create explicit `IMapper<TSource, TDestination>` or `IUpdateMapper<TSource, TDestination>` implementations and register an assembly with `services.AddDuonDevKitMappers(...)`.

`IPasswordHasher` and `Pbkdf2PasswordHasher` provide PBKDF2-HMAC-SHA256 password hashing with a random salt.

```csharp
IPasswordHasher hasher = new Pbkdf2PasswordHasher();
string hash = hasher.Hash(password);
bool valid = hasher.Verify(password, hash);
```

## DateTime/DateTimeOffset extensions

`StartOfX`/`EndOfX` helpers (`Day`, `Week`, `Month`, `Year`) for `DateTime` and `DateTimeOffset`. `StartOfX` returns midnight of the first day in the period; `EndOfX` returns the last tick of the last day (`23:59:59.9999999`), so a range check like `date >= StartOfMonth && date <= EndOfMonth` includes the entire last day.

```csharp
using DuonDevKit.Core.Extensions;

DateTime today = DateTime.UtcNow;
DateTime firstOfMonth = today.StartOfMonth();
DateTime lastInstantOfMonth = today.EndOfMonth();

DateTime mondayOfThisWeek = today.StartOfWeek(); // defaults to Monday
```

Comparison helpers: `IsBetween` (bounds given in either order, `inclusive` defaults to `true`), `IsSameDay`, `IsWeekend`/`IsWeekday`, and the "now"-relative `IsToday`/`IsInPast`/`IsInFuture` (Kind-aware for `DateTime`; always unambiguous for `DateTimeOffset`, since it carries its own UTC instant).

```csharp
bool inRange = order.PlacedAt.IsBetween(promotion.StartsAt, promotion.EndsAt);
bool overdue = invoice.DueAt.IsInPast();
```

Time zone conversion: prefer the `DateTimeOffset` overload of `ToTimeZone` — it's always unambiguous. The `DateTime` overload requires a known `Kind` (throws for `Unspecified`); `ToUtcFrom` treats a bare `DateTime` as wall-clock time in a given zone and throws instead of guessing through a DST gap or overlap.

```csharp
TimeZoneInfo newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

DateTimeOffset localTime = DateTimeOffset.UtcNow.ToTimeZone(newYork);
DateTime utcFromWallClock = businessOpeningTime.ToUtcFrom(newYork); // throws on a DST gap/overlap
```

`DateTime` overloads preserve `Kind`; `DateTimeOffset` overloads preserve `Offset`.
