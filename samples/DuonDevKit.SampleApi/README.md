# DuonDevKit Sample API

A Minimal API showing the packages working together: Result-to-ProblemDetails responses,
EF Core repository/unit of work with audit and soft delete, Dapper on the same database connection,
and JWT access/refresh-token flow.

```bash
dotnet run --project samples/DuonDevKit.SampleApi
```

1. `POST /auth/demo` to receive an access token and refresh token.
2. Send `Authorization: Bearer <accessToken>` to call `GET /todos`, `POST /todos`, and
   `GET /todos/summary`.
3. `POST /auth/refresh` with `{ "refreshToken": "..." }` to rotate the refresh token.

The SQLite database, local Data Protection keys, and signing key are for demonstration only. Use
migrations and a secret store before adapting this sample for a real service.
