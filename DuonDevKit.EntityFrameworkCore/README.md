# DuonDevKit.EntityFrameworkCore

Result-based EF Core repositories and unit of work for .NET (`net8.0`/`net9.0`), with auditing, soft deletion, pagination, and dependency-injection helpers. Requires `DuonDevKit.Core`.

## Installation

```bash
dotnet add package DuonDevKit.EntityFrameworkCore
```

## Setup

```csharp
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(connectionString).AddDuonDevKitAuditing(sp));
services.AddDuonDevKitEntityFrameworkCore<AppDbContext>();
```

This registers `IUnitOfWork`, `IRepository<T>`, and `IRepository<T, TId>`. An app-provided `ICurrentUserProvider` is used for auditing; otherwise a null-provider fallback is registered.

Configure your context's model once to enable soft-delete filtering:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteQueryFilter();
}
```

## Repositories

```csharp
using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;
using DuonDevKit.EntityFrameworkCore.Repositories;

Result<Order> order = await repository.GetByIdAsync([orderId]);
Option<Order> match = await repository.FindOneAsync(o => o.ExternalRef == externalRef);
Result<IReadOnlyList<Order>> pending = await repository.ListAsync(
    filter: o => o.Status == "Pending",
    include: q => q.Include(o => o.Customer));

await repository.AddAsync(order);
Result saved = await unitOfWork.SaveChangesAsync();
```

`FindOneAsync`/`ListAsync`/`ListPagedAsync` all take an `asNoTracking` parameter for read-only reads you won't `Update`. Use `Query(asNoTracking: true)` for projections and other fully custom EF queries. `ListPagedAsync` returns a `PagedResult<T>` with items, count, and page metadata.

## Specifications

For a query shape you reuse across call sites, name it as a `Specification<T>` instead of repeating the same `filter`/`include`/`orderBy` lambdas everywhere:

```csharp
using DuonDevKit.EntityFrameworkCore.Specifications;

public class PendingOrdersSpec : Specification<Order>
{
    public PendingOrdersSpec()
    {
        AddCriteria(o => o.Status == "Pending");
        AddInclude(q => q.Include(o => o.Customer));
        ApplyOrderBy(q => q.OrderBy(o => o.CreatedAt));
    }
}

Result<IReadOnlyList<Order>> pending = await repository.ListAsync(new PendingOrdersSpec());
Result<PagedResult<Order>> page = await repository.ListPagedAsync(new PendingOrdersSpec(), pageNumber: 1, pageSize: 20);
Option<Order> first = await repository.FindOneAsync(new PendingOrdersSpec());
```

These are additional overloads — `FindOneAsync`/`ListAsync`/`ListPagedAsync` still accept `filter`/`include`/`orderBy` directly for one-off queries.

## Transactions

Prefer `ExecuteInTransactionAsync` for an operation and its save in one execution-strategy-aware transaction.

```csharp
Result<Order> result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
    await repository.AddAsync(order, ct));
```

Manual `BeginTransactionAsync`, `CommitTransactionAsync`, and `RollbackTransactionAsync` are also available.

## Auditing and soft deletion

Implement marker interfaces such as `ICanCreate`, `ICanUpdate`, and `ISoftDelete` on entities. The audit interceptor fills the corresponding created, updated, and deleted fields, using `ICurrentUserProvider` for user identifiers.

Call `modelBuilder.ApplySoftDeleteQueryFilter()` in `OnModelCreating` to filter soft-deleted rows. `Repository<T>.Remove` soft-deletes entities implementing `ISoftDelete`.

For typed IDs, inherit from `BaseEntity<TId>` and use `IRepository<T, TId>`. Register an `IEntityIdGenerator<TId>` when IDs should be generated before persistence.
