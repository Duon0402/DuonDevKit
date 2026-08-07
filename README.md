# DuonDevKit

A lightweight .NET toolkit providing a Result pattern (Railway-Oriented Programming) and common
extension methods, built for .NET 8.

## Projects

- **DuonDevKit.Core** — core library: `Result`/`Result<T>`, `Error`, `Option<T>`, `Guard`,
  AutoMapper-free object mapping, and extension methods.
- **DuonDevKit.Core.Tests** — xUnit test suite for `DuonDevKit.Core`.
- **DuonDevKit.EntityFrameworkCore** — Result-based Repository/UnitOfWork pattern for EF Core, plus
  automatic audit-field population (created/updated/soft-deleted by + at) and DI registration helpers.
- **DuonDevKit.EntityFrameworkCore.Tests** — xUnit test suite using EF Core's InMemory provider.
- **DuonDevKit.AspNetCore** — maps `Result`/`Result<T>` to `IActionResult` (MVC) and `IResult`
  (Minimal APIs), so a controller/endpoint never has to switch on `Error.Type` itself.
- **DuonDevKit.AspNetCore.Tests** — xUnit test suite for `DuonDevKit.AspNetCore`.

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

#### Ensure

`Ensure` turns a successful result into a failure when a predicate doesn't hold, without needing
an `if`/`return` for it:

```csharp
Result<int> quantity = Parse(input)
    .Ensure(value => value > 0, Error.Validation("VAL003", "Quantity must be positive."));
```

`EnsureAsync` (in `ResultAsyncExtensions`) does the same across an async chain, on both a sync and
an async predicate:

```csharp
Result<Order> order = await GetOrderAsync(orderId)
    .EnsureAsync(o => o.Status == "Pending", Error.Business("ORD001", "Order is not pending."))
    .EnsureAsync(o => IsWithinCancellationWindowAsync(o), Error.Business("ORD002", "Cancellation window has passed."));
```

Both skip the predicate (and propagate the error unchanged) on an already-failed result.

#### Combine

`Result.Combine` returns the first failure among several results, or success if all of them
succeeded — useful for running independent validations and reporting the first one that failed:

```csharp
Result validation = Result.Combine(
    Guard.Against.NullOrEmpty(name, nameof(name)),
    Guard.Against.NegativeOrZero(quantity, nameof(quantity)));
```

### Guard clauses

`Guard.Against` returns a failed `Result` (instead of throwing) when the checked condition doesn't
hold:

```csharp
using DuonDevKit.Core.Guards;

Result Validate(string? name, int quantity)
    => Result.Combine(
        Guard.Against.Null(name, nameof(name)),
        Guard.Against.NullOrEmpty(name, nameof(name)),
        Guard.Against.NegativeOrZero(quantity, nameof(quantity)),
        Guard.Against.Negative(quantity, nameof(quantity)));
```

### Option\<T\>

`Option<T>` represents the presence or absence of a value with no reason attached — use it instead
of `Result<T>` when "not found" doesn't need an explanation:

```csharp
using DuonDevKit.Core.Options;

Option<User> FindUser(string id) =>
    _users.TryGetValue(id, out var user) ? user : Option<User>.None; // implicit operator from T

string greeting = FindUser(id).Match(
    onSome: user => $"Hello, {user.Name}!",
    onNone: () => "User not found.");

Option<string> email = FindUser(id).Map(u => u.Email);

Result<User> result = FindUser(id).ToResult(Error.NotFound("USER001", "User not found."));
```

### Mapping

Object-to-object mapping via plain, hand-written classes — no reflection/convention magic like
AutoMapper's, so renamed fields, combined fields, and computed values just work with no extra
configuration:

```csharp
using DuonDevKit.Core.Mapping;

public class OrderToOrderDtoMapper : IMapper<Order, OrderDto>
{
    public OrderDto Map(Order source) => new()
    {
        Name = source.Name,
        Total = source.Total,
    };
}

public class UpdateOrderRequestToOrderMapper : IUpdateMapper<UpdateOrderRequest, Order>
{
    public void Map(UpdateOrderRequest source, Order destination) => destination.Name = source.Name;
}
```

Register every mapper in an assembly in one call (fails fast at startup if two classes implement the
same type pair, instead of silently picking whichever one happened to be scanned last):

```csharp
services.AddDuonDevKitMappers(typeof(Program).Assembly);
```

Inject `IMapper<Order, OrderDto>`/`IUpdateMapper<UpdateOrderRequest, Order>` directly when the type
pair is known at compile time — cheapest, and the most explicit about what a class depends on. When
the type pair varies at the call site (e.g. a generic CRUD service), inject `IObjectMapper` instead:

```csharp
public class OrderService(IObjectMapper mapper)
{
    public OrderDto ToDto(Order order) => mapper.Map<Order, OrderDto>(order);
    // or, with extension-method sugar: order.MapTo<Order, OrderDto>(mapper);
}
```

`IObjectMapper` caches the resolved mapper per type pair, so only the first call for a given pair
pays the DI lookup.

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

#### Querying

`FindOneAsync` looks up a single entity by an arbitrary predicate, returning `Option<T>` — unlike
`GetByIdAsync`, "not found" isn't a failure here, just an absent value:

```csharp
Option<Order> match = await repository.FindOneAsync(o => o.ExternalRef == externalRef);
```

`ListAsync`/`ListPagedAsync`/`FindOneAsync` all accept an `include` delegate to eager-load navigation
properties:

```csharp
Result<IReadOnlyList<Order>> orders = await repository.ListAsync(
    filter: o => o.Status == "Pending",
    include: q => q.Include(o => o.Customer));
```

For anything the fixed shape of those methods can't express — joins, projections, `GroupBy`, or just
a query you want full control over — `Query(asNoTracking)` returns the raw `IQueryable<T>` (still
subject to the soft-delete filter) for you to compose and execute yourself:

```csharp
var topCustomers = await repository.Query(asNoTracking: true)
    .GroupBy(o => o.CustomerId)
    .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.Total) })
    .OrderByDescending(x => x.Total)
    .Take(10)
    .ToListAsync();
```

#### Id generation

`Repository<T, TId>.AddAsync`/`AddRangeAsync` assign a new id via an injected `IEntityIdGenerator<TId>`
when an entity's `Id` is still at its default value — opt-in, for apps that generate ids client-side
(e.g. a GUID string) instead of relying on the database:

```csharp
services.AddScoped<IEntityIdGenerator<string>, GuidStringIdGenerator>(); // ready-made GUID-string generator
```

Leave it unregistered for entities whose id is database-generated (e.g. an auto-increment `int`) —
`Id` is left exactly as the caller set it.

#### Dependency injection setup

Instead of `new`-ing everything by hand, register it once and let each repository/unit of work be
resolved per request:

```csharp
using DuonDevKit.EntityFrameworkCore.DependencyInjection;

services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(connectionString).AddDuonDevKitAuditing(sp)); // wires AuditSaveChangesInterceptor

services.AddDuonDevKitEntityFrameworkCore<AppDbContext>(); // IUnitOfWork, IRepository<T>, IRepository<T, TId>
```

If the app registers its own `ICurrentUserProvider` (e.g. wrapping `IHttpContextAccessor`), it's
picked up automatically; otherwise a `NullCurrentUserProvider` (`UserId` always `null`) is used so
setup still works without one.

#### Bulk operations

`Repository<T>` also has range versions of `AddAsync`/`Remove`, plus `Update`/`UpdateRange` for
disconnected entities (e.g. a full entity deserialized from a client request) that don't need a
fetch-then-mutate round trip:

```csharp
await repository.AddRangeAsync([order1, order2]);

repository.Update(detachedOrder);           // attaches + marks every property modified
repository.UpdateRange([order1, order2]);

repository.Remove(order);                   // soft- or hard-deletes depending on ISoftDelete
repository.RemoveRange([order1, order2]);   // same, batched; attaches any detached entity first

await unitOfWork.SaveChangesAsync();
```

#### Pagination

`ListPagedAsync` returns a single page plus the total count, instead of loading every matching row:

```csharp
Result<PagedResult<Order>> page = await repository.ListPagedAsync(
    pageNumber: 1,
    pageSize: 20,
    filter: o => o.Status == "Pending",
    orderBy: q => q.OrderByDescending(o => o.CreatedAt)); // pass an orderBy for a stable page order

if (page.IsSuccess)
{
    IReadOnlyList<Order> items = page.Value.Items;
    int totalPages = page.Value.TotalPages;
    bool hasNext = page.Value.HasNextPage;
}
```

Fails with `Error.Validation` if `pageNumber`/`pageSize` isn't positive.

#### Transactions

`UnitOfWork` also manages transactions. Prefer `ExecuteInTransactionAsync` — it wraps the operation
and the save in a single transaction via the provider's execution strategy, so retrying providers
(e.g. `EnableRetryOnFailure`) retry the whole unit safely, and it commits only if both succeed:

```csharp
Result<Order> result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    var added = await repository.AddAsync(new Order { /* ... */ }, ct);
    return added;
});
```

For finer-grained manual control (bypasses execution-strategy retries):

```csharp
await unitOfWork.BeginTransactionAsync();
// ... repository calls, unitOfWork.SaveChangesAsync() ...
await unitOfWork.CommitTransactionAsync();   // or RollbackTransactionAsync()
```

`unitOfWork.HasChanges()` reports whether the underlying context is tracking any pending work.

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

### AspNetCore (Result → HTTP response)

`ToActionResult`/`ToApiResult` map a `Result`/`Result<T>` straight to a response — a controller or
Minimal API endpoint never has to switch on `Error.Type` itself. A failure becomes a
`ProblemDetails` body, using `Error.ToHttpStatusCode()` for the status code and exposing
`Error.Code` as an `errorCode` extension:

```csharp
using DuonDevKit.AspNetCore;

// MVC controller
[HttpGet("{id}")]
public async Task<IActionResult> GetById(string id)
{
    Result<Order> result = await repository.GetByIdAsync([id]);
    return result.ToActionResult(); // 200 + Order, or a ProblemDetails with the right status code
}

// Minimal API
app.MapGet("/orders/{id}", async (string id, IRepository<Order> repository) =>
{
    Result<Order> result = await repository.GetByIdAsync([id]);
    return result.ToApiResult();
});
```

A successful `Result` (no value) maps to `204 No Content`; a successful `Result<T>` maps to
`200 OK` with `Value` as the body.

#### Unhandled exceptions

`UseDuonDevKitExceptionHandling()` catches anything that reaches the middleware pipeline unhandled
and responds with the same `ProblemDetails` shape as a failed `Result` (`500`,
`Error.Unexpected`) — a bug behaves the same way an expected failure would instead of leaking the
framework's default error response. It logs the original exception via `ILoggerFactory` first.
Register it early in the pipeline, before routing/MVC:

```csharp
var app = builder.Build();
app.UseDuonDevKitExceptionHandling();
// ... app.UseRouting(), app.MapControllers(), etc.
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
