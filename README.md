# DuonDevKit

DuonDevKit is a collection of lightweight .NET libraries (targeting `net8.0` and `net9.0`) for explicit error handling, data access, ASP.NET Core responses, and JWT authentication.

## Packages

| Package | Description | Documentation |
| --- | --- | --- |
| `DuonDevKit.Core` | Result/Error, Option, guards, mapping, security, and extensions. | [README](DuonDevKit.Core/README.md) |
| `DuonDevKit.EntityFrameworkCore` | Result-based Repository/UnitOfWork with auditing and soft deletion. | [README](DuonDevKit.EntityFrameworkCore/README.md) |
| `DuonDevKit.AspNetCore` | Converts Results into MVC and Minimal API responses. | [README](DuonDevKit.AspNetCore/README.md) |
| `DuonDevKit.Dapper` | Executes Dapper SQL through an EF Core context connection and transaction. | [README](DuonDevKit.Dapper/README.md) |
| `DuonDevKit.Jwt` | JWT access/refresh token support and audit-user integration. | [README](DuonDevKit.Jwt/README.md) |

## Getting started

Requires the .NET 8 SDK, the .NET 9 SDK, or both (building/testing the full solution locally builds both `net8.0` and `net9.0` targets).

Install only the packages your application needs. Packages that depend on another DuonDevKit package bring that dependency in automatically.

```bash
dotnet add package DuonDevKit.Core
dotnet add package DuonDevKit.EntityFrameworkCore
dotnet add package DuonDevKit.AspNetCore
dotnet add package DuonDevKit.Dapper
dotnet add package DuonDevKit.Jwt
```

Each package README includes its required registration and first-use example.

For an end-to-end Minimal API using all five packages, see
[samples/DuonDevKit.SampleApi](samples/DuonDevKit.SampleApi/README.md).

```bash
dotnet build DuonDevKit.slnx
dotnet test DuonDevKit.slnx
```

## Contributing

Keep builds warning-free and include unit tests for changed behavior.
