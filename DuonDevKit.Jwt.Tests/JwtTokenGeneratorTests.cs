using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DuonDevKit.Jwt.Tests
{
    public class JwtTokenGeneratorTests
    {
        [Fact]
        public void GenerateAccessToken_CarriesGivenClaimsAndIssuerAudience()
        {
            var settings = TestFactory.CreateSettings();
            var generator = new JwtTokenGenerator(settings);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(ClaimTypes.Name, "Alice") };

            var token = generator.GenerateAccessToken(claims);
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal("test-issuer", parsed.Issuer);
            Assert.Contains("test-audience", parsed.Audiences);
            Assert.Equal("user-1", parsed.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("Alice", parsed.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        }

        [Fact]
        public void GenerateAccessToken_ExpiresAfterConfiguredLifetime()
        {
            var settings = TestFactory.CreateSettings();
            settings = new JwtSettings
            {
                SigningKey = settings.SigningKey,
                Issuer = settings.Issuer,
                Audience = settings.Audience,
                AccessTokenLifetime = TimeSpan.FromMinutes(5),
            };
            var generator = new JwtTokenGenerator(settings);

            var token = generator.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.True((parsed.ValidTo - DateTime.UtcNow).TotalMinutes is > 4 and <= 5);
        }

        [Fact]
        public void GenerateRefreshToken_TwoCalls_ProduceDifferentValues()
        {
            var generator = new JwtTokenGenerator(TestFactory.CreateSettings());

            var first = generator.GenerateRefreshToken();
            var second = generator.GenerateRefreshToken();

            Assert.NotEqual(first, second);
        }
    }
}
