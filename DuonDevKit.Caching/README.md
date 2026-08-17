# DuonDevKit.Caching

A cache abstraction wrapped in `DuonDevKit.Core`'s `Result`/`Option` pattern, backed by
[`HybridCache`](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) — memory-only
by default, or memory+Redis when a connection string is configured. Application code always talks
to `ICacheService`; it never needs to know which mode is active.

## Installation

```bash
dotnet add package DuonDevKit.Caching
```

## Setup

```csharp
using DuonDevKit.Caching.DependencyInjection;

// Memory-only:
services.AddDuonDevKitCaching();

// Memory + Redis:
services.AddDuonDevKitCaching(new CachingSettings
{
    DefaultExpiration = TimeSpan.FromMinutes(10),
    RedisConnectionString = builder.Configuration.GetConnectionString("Redis"),
});
```

Inject `ICacheService` into the service that needs caching.

## Usage

```csharp
Result<Product> product = await cacheService.GetOrCreateAsync(
    $"product:{id}",
    async ct => await productRepository.GetByIdAsync(id, ct),
    expiration: TimeSpan.FromMinutes(30));

Result<Option<Product>> cached = await cacheService.GetAsync<Product>($"product:{id}");

Result stored = await cacheService.SetAsync($"product:{id}", product, TimeSpan.FromMinutes(30));

Result removed = await cacheService.RemoveAsync($"product:{id}");
```

A factory passed to `GetOrCreateAsync` that returns a failed `Result<T>` is never cached — the
factory runs again on the next call. Infrastructure errors (e.g. Redis unavailable) are returned as
failed `Result`/`Result<T>` values instead of thrown exceptions.