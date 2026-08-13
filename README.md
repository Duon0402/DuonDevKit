# DuonDevKit

DuonDevKit is a collection of lightweight .NET libraries (targeting `net8.0` and `net9.0`) for explicit error handling, data access, ASP.NET Core responses, JWT authentication, and validation.

## Packages

| Package | Description | Documentation |
| --- | --- | --- |
| `DuonDevKit.Core` | Result/Error, Option, guards, mapping, security, DataAnnotations validation, and extensions. | [README](DuonDevKit.Core/README.md) |
| `DuonDevKit.EntityFrameworkCore` | Result-based Repository/UnitOfWork with auditing and soft deletion. | [README](DuonDevKit.EntityFrameworkCore/README.md) |
| `DuonDevKit.AspNetCore` | Converts Results into MVC and Minimal API responses; automatic request validation. | [README](DuonDevKit.AspNetCore/README.md) |
| `DuonDevKit.Dapper` | Executes Dapper SQL through an EF Core context connection and transaction. | [README](DuonDevKit.Dapper/README.md) |
| `DuonDevKit.Jwt` | JWT access/refresh token support and audit-user integration. | [README](DuonDevKit.Jwt/README.md) |
| `DuonDevKit.Validation` | FluentValidation integration for the Result pattern. | [README](DuonDevKit.Validation/README.md) |
| `DuonDevKit.Templates` | `dotnet new` templates that scaffold a new project already wired to these packages. | [README](DuonDevKit.Templates/README.md) |

## Getting started

Requires the .NET 8 SDK, the .NET 9 SDK, or both (building/testing the full solution locally builds both `net8.0` and `net9.0` targets).

Install only the packages your application needs. Packages that depend on another DuonDevKit package bring that dependency in automatically.

```bash
dotnet add package DuonDevKit.Core
dotnet add package DuonDevKit.EntityFrameworkCore
dotnet add package DuonDevKit.AspNetCore
dotnet add package DuonDevKit.Dapper
dotnet add package DuonDevKit.Jwt
dotnet add package DuonDevKit.Validation
```

Each package README includes its required registration and first-use example.

To start a new project with everything already wired together instead of adding each package by
hand, install the project template:

```bash
dotnet new install DuonDevKit.Templates
dotnet new duondevkit-api -n MyApi --auth --dapper --validation
```

See [DuonDevKit.Templates](DuonDevKit.Templates/README.md) for what each flag adds.

```bash
dotnet build DuonDevKit.slnx
dotnet test DuonDevKit.slnx
```

## Contributing

Keep builds warning-free and include unit tests for changed behavior.
