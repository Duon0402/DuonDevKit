# DuonDevKit.Dapper

Dapper queries wrapped in `DuonDevKit.Core` results, using the connection and current transaction of an EF Core `DbContext`. This keeps raw SQL atomic inside the same unit-of-work transaction.

## Installation

```bash
dotnet add package DuonDevKit.Dapper
```

## Setup

```csharp
using DuonDevKit.Dapper.DependencyInjection;

services.AddDuonDevKitDapper<AppDbContext>();
```

It can be registered alongside `AddDuonDevKitEntityFrameworkCore<AppDbContext>()`.

Inject `IDapperQueries` into the service or endpoint that runs SQL; do not create an independent database connection when the query must share the EF Core transaction.

## Queries and commands

```csharp
Result<IReadOnlyList<OrderReportRow>> report = await dapperQueries.QueryAsync<OrderReportRow>(
    "SELECT o.Id, o.Total FROM Orders o WHERE o.Status = @Status",
    new { Status = "Pending" });

Result<Option<Order>> order = await dapperQueries.QueryFirstOrDefaultAsync<Order>(
    "SELECT * FROM Orders WHERE Id = @Id", new { Id = orderId });

Result<int> updated = await dapperQueries.ExecuteAsync(
    "UPDATE Orders SET Status = @Status WHERE Id = @Id",
    new { Status = "Shipped", Id = orderId });
```

Database errors are returned as failed `Result` values. A query with no matching row returns `Option<T>.None`.
