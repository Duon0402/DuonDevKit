# DuonDevKit.ApiTemplate

A Minimal API scaffolded by the `duondevkit-api` template, wired to
[DuonDevKit](https://github.com/Duon0402/DuonDevKit)'s `Core`, `EntityFrameworkCore`, and
`AspNetCore` packages (Result-to-ProblemDetails responses, EF Core repository/unit of work with
audit fields and soft delete).

```bash
dotnet run
```

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

`Jwt:SigningKey` in `appsettings.json`/user secrets is a development-only placeholder — replace it
with a secret store value (at least 32 bytes) before deploying.
#endif-->
<!--#if (validation)

## Validation

`CreateTodoRequestValidator` (FluentValidation, via `DuonDevKit.Validation`) validates `POST /todos`.
#endif-->

The SQLite database file (and, if authentication is enabled, the local Data Protection keys) are for
local development only — use EF Core migrations and a real secret store before shipping this.
