# DuonDevKit.Jwt

JWT access-token creation and validation, persisted refresh tokens with rotation, and an `ICurrentUserProvider` implementation for EF Core auditing. Requires `DuonDevKit.EntityFrameworkCore`.

## Installation

```bash
dotnet add package DuonDevKit.Jwt
```

## Setup

Register the EF Core integration first, then JWT support.

```csharp
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.Jwt;
using DuonDevKit.Jwt.DependencyInjection;

services.AddDuonDevKitEntityFrameworkCore<AppDbContext>();
services.AddDuonDevKitJwt(new JwtSettings
{
    SigningKey = builder.Configuration["Jwt:SigningKey"]!, // must be >= 32 bytes — AddDuonDevKitJwt throws otherwise
    Issuer = "MyApp",
    Audience = "MyApp",
});
services.AddAuthorization();
```

Add the persisted refresh-token entity to your context and configure its indexes:

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ConfigureDuonDevKitRefreshTokens();
}
```

Enable JWT validation in the HTTP pipeline:

```csharp
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

## Issuing and rotating tokens

```csharp
string accessToken = tokenGenerator.GenerateAccessToken(
    [new Claim(ClaimTypes.NameIdentifier, user.Id)]);

Result<string> refreshToken = await refreshTokenService.IssueAsync(user.Id);
Result<RefreshTokenRotationResult> rotated =
    await refreshTokenService.RotateAsync(incomingRefreshToken);
```

`AddDuonDevKitJwt` configures JWT bearer authentication and registers `HttpContextCurrentUserProvider`. It replaces only the EntityFrameworkCore null-provider fallback, preserving an application-supplied `ICurrentUserProvider`; audit fields can therefore use the authenticated user's JWT claims.

Token validation uses `ClockSkew = TimeSpan.Zero` (stricter than the JWT library's usual 5-minute default), so make sure the issuing and validating instances' clocks are kept in sync (NTP) — otherwise tokens near their expiry can be rejected slightly early.
