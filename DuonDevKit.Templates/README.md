# DuonDevKit.Templates

`dotnet new` templates that scaffold a Minimal API project already wired to
[DuonDevKit](https://github.com/Duon0402/DuonDevKit)'s packages, instead of adding each package to
an empty project by hand.

## Installation

```bash
dotnet new install DuonDevKit.Templates
```

## Usage

```bash
dotnet new duondevkit-api -n MyApi
cd MyApi
dotnet run
```

Scaffolds a Minimal API with `DuonDevKit.Core`, `DuonDevKit.EntityFrameworkCore`, and
`DuonDevKit.AspNetCore` wired together: a `TodoItem` entity with audit fields and soft delete, a
repository-backed `GET`/`POST /todos`, and `Result`-to-`ProblemDetails` error responses — see the
generated project's own `README.md` for the exact endpoints.

### Options

```bash
dotnet new duondevkit-api -n MyApi --auth --dapper --validation
```

| Flag | Adds |
| --- | --- |
| `--auth` | `DuonDevKit.Jwt` — access/refresh-token endpoints, `RequireAuthorization()` on the API endpoints. |
| `--dapper` | `DuonDevKit.Dapper` — a `GET /todos/summary` endpoint using raw SQL through the same connection. |
| `--validation` | `DuonDevKit.Validation` (FluentValidation) instead of the built-in DataAnnotations filter for request validation. |

## Updating

```bash
dotnet new uninstall DuonDevKit.Templates
dotnet new install DuonDevKit.Templates
```

(`dotnet new update` also updates it if it was installed as a NuGet package.)
