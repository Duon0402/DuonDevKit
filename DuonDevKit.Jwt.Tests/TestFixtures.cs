using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
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

    /// <summary>Wraps a real <see cref="IUnitOfWork"/> but makes every <see cref="SaveChangesAsync"/> call fail with a fixed error, to test failure-propagation paths without needing a real save failure.</summary>
    public sealed class AlwaysFailingSaveUnitOfWork(IUnitOfWork inner, Error error) : IUnitOfWork
    {
        public Task<Result> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(Result.Fail(error));
        public bool HasChanges() => inner.HasChanges();
        public Task<Result> BeginTransactionAsync(CancellationToken ct = default) => inner.BeginTransactionAsync(ct);
        public Task<Result> CommitTransactionAsync(CancellationToken ct = default) => inner.CommitTransactionAsync(ct);
        public Task<Result> RollbackTransactionAsync(CancellationToken ct = default) => inner.RollbackTransactionAsync(ct);
        public Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken ct = default) => inner.ExecuteInTransactionAsync(operation, ct);
        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct = default) => inner.ExecuteInTransactionAsync(operation, ct);
    }

    /// <summary>
    /// Wraps a real <see cref="IUnitOfWork"/> and runs <paramref name="beforeSave"/> before every
    /// <see cref="SaveChangesAsync"/> call delegated to <paramref name="inner"/> — used to count how many
    /// separate saves an operation performs, independent of the InMemory provider's own (non-atomic,
    /// and therefore misleading for concurrency testing) SaveChanges semantics.
    /// </summary>
    public sealed class InstrumentedUnitOfWork(IUnitOfWork inner, Action beforeSave) : IUnitOfWork
    {
        public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
        {
            beforeSave();
            return await inner.SaveChangesAsync(ct);
        }

        public bool HasChanges() => inner.HasChanges();
        public Task<Result> BeginTransactionAsync(CancellationToken ct = default) => inner.BeginTransactionAsync(ct);
        public Task<Result> CommitTransactionAsync(CancellationToken ct = default) => inner.CommitTransactionAsync(ct);
        public Task<Result> RollbackTransactionAsync(CancellationToken ct = default) => inner.RollbackTransactionAsync(ct);
        public Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken ct = default) => inner.ExecuteInTransactionAsync(operation, ct);
        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct = default) => inner.ExecuteInTransactionAsync(operation, ct);
    }
}
