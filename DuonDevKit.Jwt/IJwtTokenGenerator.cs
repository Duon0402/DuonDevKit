using System.Security.Claims;

namespace DuonDevKit.Jwt
{
    /// <summary>Issues access and refresh tokens.</summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>Creates a signed JWT access token carrying <paramref name="claims"/>, valid for <see cref="JwtSettings.AccessTokenLifetime"/>.</summary>
        string GenerateAccessToken(IEnumerable<Claim> claims);

        /// <summary>Creates a new opaque refresh token (not a JWT) — a cryptographically random string, meant to be persisted via <see cref="IRefreshTokenService"/>.</summary>
        string GenerateRefreshToken();
    }
}
