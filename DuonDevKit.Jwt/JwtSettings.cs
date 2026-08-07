namespace DuonDevKit.Jwt
{
    /// <summary>Configuration for issuing and validating JWTs. Bind from configuration (e.g. <c>appsettings.json</c>) or construct directly.</summary>
    public sealed class JwtSettings
    {
        /// <summary>The symmetric key used to sign access tokens (HMAC-SHA256) — keep this secret, and use at least 32 bytes/256 bits of entropy.</summary>
        public required string SigningKey { get; init; }

        /// <summary>The <c>iss</c> claim issued tokens carry, and the value validated against on incoming tokens.</summary>
        public required string Issuer { get; init; }

        /// <summary>The <c>aud</c> claim issued tokens carry, and the value validated against on incoming tokens.</summary>
        public required string Audience { get; init; }

        /// <summary>How long an access token stays valid after being issued. Defaults to 15 minutes.</summary>
        public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

        /// <summary>How long a refresh token stays valid after being issued. Defaults to 7 days.</summary>
        public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);
    }
}
