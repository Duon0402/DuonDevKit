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
    return result.ToActionResult(HttpContext);
}

app.MapGet("/orders/{id}", async (string id, IRepository<Order> repository) =>
{
    Result<Order> result = await repository.GetByIdAsync([id]);
    return result.ToApiResult();
});
```

A successful `Result<T>` maps to `200 OK` with its value; a successful non-generic `Result` maps to `204 No Content`. Failures map to `ProblemDetails`, using `Error.ToHttpStatusCode()`, with the error code exposed as the `errorCode` extension.

Pass `HttpContext` to `ToActionResult()` (as above) so any `ProblemDetailsOptions.CustomizeProblemDetails` you registered via `AddProblemDetails()` applies to MVC responses the same way it already does for `ToApiResult()`. Omit it and the response still works, just without that customization.

## Unhandled exceptions

Register the middleware early in the pipeline for matching `ProblemDetails` responses to unexpected exceptions:

```csharp
var app = builder.Build();
app.UseDuonDevKitExceptionHandling();
```

The middleware logs the original exception and sends a 500 response with `Error.Unexpected`.

## Automatic request validation (Minimal APIs)

`WithDuonDevKitValidation<T>()` validates a bound parameter against its
`System.ComponentModel.DataAnnotations` attributes before the handler runs, short-circuiting with a
`400` field-level `ValidationProblem` if invalid — no dependency beyond the base class library.

```csharp
using DuonDevKit.AspNetCore.Validation;

public class CreateOrderRequest
{
    [Required, MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; set; }
}

app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok())
   .WithDuonDevKitValidation<CreateOrderRequest>();
```

An invalid request never reaches the handler; the response body is a standard
`{ "errors": { "Quantity": ["..."] }, "errorCode": "VALIDATION001" }` shape. For rules that need to
be conditional, compare properties against each other, or call out to a database/service, use
`DuonDevKit.Validation`'s FluentValidation integration directly in the handler instead — it composes
with the same `Result`-to-HTTP mapping shown above.
