using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.Jwt.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DuonDevKit.Jwt.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private class StubCurrentUserProvider : ICurrentUserProvider
        {
            public string? UserId => "stub-user";
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection>? beforeJwt = null, Action<IServiceCollection>? afterJwt = null)
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddDuonDevKitEntityFrameworkCore<TestDbContext>();
            beforeJwt?.Invoke(services);
            services.AddDuonDevKitJwt(TestFactory.CreateSettings());
            afterJwt?.Invoke(services);

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task AddDuonDevKitJwt_RegistersJwtTokenGenerator()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            Assert.IsType<JwtTokenGenerator>(scope.ServiceProvider.GetService<IJwtTokenGenerator>());
        }

        [Fact]
        public async Task AddDuonDevKitJwt_RegistersRefreshTokenService()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            Assert.IsType<RefreshTokenService>(scope.ServiceProvider.GetService<IRefreshTokenService>());
        }

        [Fact]
        public async Task AddDuonDevKitJwt_CalledAfterEntityFrameworkCore_TakesOverFromNullCurrentUserProvider()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            Assert.IsType<HttpContextCurrentUserProvider>(scope.ServiceProvider.GetService<ICurrentUserProvider>());
        }

        [Fact]
        public async Task AddDuonDevKitJwt_AppRegisteredOwnProviderBeforeCall_LeavesItInPlace()
        {
            await using var scope = BuildProvider(
                beforeJwt: services => services.AddScoped<ICurrentUserProvider, StubCurrentUserProvider>())
                .CreateAsyncScope();

            Assert.IsType<StubCurrentUserProvider>(scope.ServiceProvider.GetService<ICurrentUserProvider>());
        }

        [Fact]
        public async Task AddDuonDevKitJwt_AppRegisteredOwnProviderAfterCall_OverridesHttpContextCurrentUserProvider()
        {
            await using var scope = BuildProvider(
                afterJwt: services => services.AddScoped<ICurrentUserProvider, StubCurrentUserProvider>())
                .CreateAsyncScope();

            Assert.IsType<StubCurrentUserProvider>(scope.ServiceProvider.GetService<ICurrentUserProvider>());
        }

        [Fact]
        public void JwtSettings_SigningKeyShorterThan32Bytes_ThrowsOnConstruction()
        {
            Assert.Throws<ArgumentException>(() => new JwtSettings
            {
                SigningKey = "too-short",
                Issuer = "test-issuer",
                Audience = "test-audience",
            });
        }

        [Fact]
        public async Task AddDuonDevKitJwt_ConfiguresJwtBearerAuthenticationScheme()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.Equal("test-issuer", options.TokenValidationParameters.ValidIssuer);
            Assert.Equal("test-audience", options.TokenValidationParameters.ValidAudience);
            Assert.Equal([SecurityAlgorithms.HmacSha256], options.TokenValidationParameters.ValidAlgorithms);
        }
    }
}
