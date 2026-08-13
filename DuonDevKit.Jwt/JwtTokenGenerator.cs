using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DuonDevKit.Jwt
{
    /// <summary>Default <see cref="IJwtTokenGenerator"/>, signing access tokens with <see cref="JwtSettings.SigningKey"/> via HMAC-SHA256.</summary>
    public sealed class JwtTokenGenerator(JwtSettings settings) : IJwtTokenGenerator
    {
        private static readonly JwtSecurityTokenHandler TokenHandler = new();

        /// <summary>Built once per generator instance (registered as a singleton) since <paramref name="settings"/>'s <see cref="JwtSettings.SigningKey"/> never changes.</summary>
        private readonly SigningCredentials _signingCredentials =
            new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256);

        /// <inheritdoc />
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(settings.AccessTokenLifetime),
                signingCredentials: _signingCredentials);

            return TokenHandler.WriteToken(token);
        }

        /// <inheritdoc />
        public string GenerateRefreshToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
