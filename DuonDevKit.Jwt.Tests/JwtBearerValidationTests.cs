using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DuonDevKit.Jwt.Tests
{
    /// <summary>
    /// Exercises the actual token-validation path (the same <see cref="TokenValidationParameters"/> shape
    /// <c>ServiceCollectionExtensions.AddDuonDevKitJwt</c> configures for the JwtBearer handler) instead of
    /// only asserting that the configuration object holds the expected values. A generated token is fed
    /// straight through <see cref="JwtSecurityTokenHandler.ValidateToken(string, TokenValidationParameters, out SecurityToken)"/>
    /// to prove a forged/expired/mismatched token is actually rejected, not just configured-to-be-rejected.
    /// </summary>
    public class JwtBearerValidationTests
    {
        private static readonly JwtSecurityTokenHandler Handler = new();

        private static TokenValidationParameters ValidationParametersFor(JwtSettings settings) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        [Fact]
        public void ValidateToken_ValidAccessToken_Succeeds()
        {
            var settings = TestFactory.CreateSettings();
            var generator = new JwtTokenGenerator(settings);
            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);

            var principal = Handler.ValidateToken(token, ValidationParametersFor(settings), out _);

            Assert.Equal("user-1", principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [Fact]
        public void ValidateToken_ExpiredAccessToken_ThrowsSecurityTokenExpiredException()
        {
            var baseSettings = TestFactory.CreateSettings();
            var settings = new JwtSettings
            {
                SigningKey = baseSettings.SigningKey,
                Issuer = baseSettings.Issuer,
                Audience = baseSettings.Audience,
                AccessTokenLifetime = TimeSpan.FromMinutes(-5),
            };
            var generator = new JwtTokenGenerator(settings);
            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);

            Assert.Throws<SecurityTokenExpiredException>(
                () => Handler.ValidateToken(token, ValidationParametersFor(settings), out _));
        }

        [Fact]
        public void ValidateToken_WrongSigningKey_ThrowsSecurityTokenSignatureKeyNotFoundException()
        {
            var settings = TestFactory.CreateSettings();
            var generator = new JwtTokenGenerator(settings);
            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            var wrongKeySettings = new JwtSettings
            {
                SigningKey = "a-completely-different-test-signing-key-1234567890ab",
                Issuer = settings.Issuer,
                Audience = settings.Audience,
            };

            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
                () => Handler.ValidateToken(token, ValidationParametersFor(wrongKeySettings), out _));
        }

        [Fact]
        public void ValidateToken_WrongIssuer_ThrowsSecurityTokenInvalidIssuerException()
        {
            var settings = TestFactory.CreateSettings();
            var generator = new JwtTokenGenerator(settings);
            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            var wrongIssuerSettings = new JwtSettings
            {
                SigningKey = settings.SigningKey,
                Issuer = "someone-elses-issuer",
                Audience = settings.Audience,
            };

            Assert.Throws<SecurityTokenInvalidIssuerException>(
                () => Handler.ValidateToken(token, ValidationParametersFor(wrongIssuerSettings), out _));
        }

        [Fact]
        public void ValidateToken_WrongAudience_ThrowsSecurityTokenInvalidAudienceException()
        {
            var settings = TestFactory.CreateSettings();
            var generator = new JwtTokenGenerator(settings);
            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            var wrongAudienceSettings = new JwtSettings
            {
                SigningKey = settings.SigningKey,
                Issuer = settings.Issuer,
                Audience = "someone-elses-audience",
            };

            Assert.Throws<SecurityTokenInvalidAudienceException>(
                () => Handler.ValidateToken(token, ValidationParametersFor(wrongAudienceSettings), out _));
        }
    }
}
