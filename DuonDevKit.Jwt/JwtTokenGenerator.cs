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
        /// <inheritdoc />
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(settings.AccessTokenLifetime),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc />
        public string GenerateRefreshToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
