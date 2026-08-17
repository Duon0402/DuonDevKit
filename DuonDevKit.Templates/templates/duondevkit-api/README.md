# DuonDevKit.ApiTemplate

A Minimal API scaffolded by the `duondevkit-api` template, wired to
[DuonDevKit](https://github.com/Duon0402/DuonDevKit)'s `Core`, `EntityFrameworkCore`, and
`AspNetCore` packages (Result-to-ProblemDetails responses, EF Core repository/unit of work with
audit fields and soft delete).

```bash
dotnet run
```

On startup, the app applies the migrations already included under `Migrations/` (no manual step
needed for a first run).

- `GET /todos` — list todos.
- `POST /todos` — create a todo (`{ "title": "..." }`).
<!--#if (dapper)
- `GET /todos/summary` — per-status counts via `DuonDevKit.Dapper` raw SQL.
#endif-->

<!--#if (auth)
## Authentication

1. `POST /auth/demo` to receive an access token and refresh token.
2. Send `Authorization: Bearer <accessToken>` to call the endpoints above.
3. `POST /auth/refresh` with `{ "refreshToken": "..." }` to rotate the refresh token.
#endif-->
<!--#if (validation)

## Validation

`CreateTodoRequestValidator` (FluentValidation, via `DuonDevKit.Validation`) validates `POST /todos`.
#endif-->

## API docs

In `Development`, the raw OpenAPI document is served at `GET /openapi/v1.json`. Add a UI if you want
one browsable — e.g. `dotnet add package Scalar.AspNetCore` then `app.MapScalarApiReference()`, or
Swagger UI.

## Database migrations

Schema changes go through EF Core migrations, not `EnsureCreated`. After editing the model:

```bash
dotnet ef migrations add <DescriptiveName>
```

The included `Migrations/InitialCreate` already covers the starting schema<!--#if (auth) --> (`Todos`
and, since authentication is enabled, `RefreshTokens`)<!--#endif -->.

## Secrets

This project has a `UserSecretsId` (generated when it was scaffolded), so secrets never need to live
in `appsettings.json`:

```bash
<!--#if (auth)
dotnet user-secrets set "Jwt:SigningKey" "<a real, 32+ byte value>"
#endif-->
dotnet user-secrets set "ConnectionStrings:Default" "<your real connection string>"
```

In production, use environment variables or your host's secret store (Azure Key Vault, AWS Secrets
Manager, etc.) instead of user secrets — user secrets are a local-development-only mechanism, not
encrypted at rest.

The SQLite database file (and, if authentication is enabled, the local Data Protection keys) are for
local development only — point `ConnectionStrings:Default` at a real database before shipping this.
