using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Jwt.Tests
{
    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureDuonDevKitRefreshTokens();
        }
    }

    public static class TestFactory
    {
        public static (TestDbContext Context, IRefreshTokenService Service) CreateRefreshTokenService(
            JwtSettings settings,
            IJwtTokenGenerator? tokenGenerator = null,
            string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new TestDbContext(options);
            var repository = new Repository<RefreshToken, string>(context);
            var unitOfWork = new UnitOfWork(context);
            var generator = tokenGenerator ?? new JwtTokenGenerator(settings);

            return (context, new RefreshTokenService(repository, unitOfWork, generator, settings));
        }

        public static JwtSettings CreateSettings(TimeSpan? refreshTokenLifetime = null) => new()
        {
            SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
            Issuer = "test-issuer",
            Audience = "test-audience",
            RefreshTokenLifetime = refreshTokenLifetime ?? TimeSpan.FromDays(7),
        };
    }
}
