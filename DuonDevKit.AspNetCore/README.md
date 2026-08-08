# DuonDevKit.AspNetCore

ASP.NET Core integration for `DuonDevKit.Core` results. It converts `Result` and `Result<T>` into consistent MVC or Minimal API HTTP responses.

## Installation

```bash
dotnet add package DuonDevKit.AspNetCore
```

## Convert Results to HTTP responses

No service registration is required. Add `using DuonDevKit.AspNetCore;` where a controller or endpoint returns a Core `Result`.

```csharp
using DuonDevKit.AspNetCore;

[HttpGet("{id}")]
public async Task<IActionResult> GetById(string id)
{
    Result<Order> result = await repository.GetByIdAsync([id]);
    return result.ToActionResult();
}

app.MapGet("/orders/{id}", async (string id, IRepository<Order> repository) =>
{
    Result<Order> result = await repository.GetByIdAsync([id]);
    return result.ToApiResult();
});
```

A successful `Result<T>` maps to `200 OK` with its value; a successful non-generic `Result` maps to `204 No Content`. Failures map to `ProblemDetails`, using `Error.ToHttpStatusCode()`, with the error code exposed as the `errorCode` extension.

## Unhandled exceptions

Register the middleware early in the pipeline for matching `ProblemDetails` responses to unexpected exceptions:

```csharp
var app = builder.Build();
app.UseDuonDevKitExceptionHandling();
```

The middleware logs the original exception and sends a 500 response with `Error.Unexpected`.
