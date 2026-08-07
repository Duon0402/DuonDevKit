using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DuonDevKit.Jwt.Tests
{
    public class HttpContextCurrentUserProviderTests
    {
        [Fact]
        public void UserId_AuthenticatedRequest_ReturnsNameIdentifierClaim()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "TestAuth");
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            };
            var provider = new HttpContextCurrentUserProvider(accessor);

            Assert.Equal("user-1", provider.UserId);
        }

        [Fact]
        public void UserId_UnauthenticatedRequest_ReturnsNull()
        {
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var provider = new HttpContextCurrentUserProvider(accessor);

            Assert.Null(provider.UserId);
        }

        [Fact]
        public void UserId_NoHttpContext_ReturnsNull()
        {
            var accessor = new HttpContextAccessor { HttpContext = null };
            var provider = new HttpContextCurrentUserProvider(accessor);

            Assert.Null(provider.UserId);
        }
    }
}
