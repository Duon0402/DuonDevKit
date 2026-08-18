using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests
{
    public class UnitOfWorkTests
    {
        /// <summary>Test-only context that always throws <see cref="DbUpdateException"/> on save, to deterministically exercise <see cref="UnitOfWork"/>'s catch path without depending on a specific provider's constraint-violation behavior.</summary>
        private class ThrowingDbContext : TestDbContext
        {
            public ThrowingDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new DbUpdateException("Simulated failure.");
        }

        private static TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TestDbContext(options);
        }

        /// <summary>Test-only context marking <see cref="TestEntity.Name"/> as a concurrency token, so a stale write against it triggers a real <see cref="DbUpdateConcurrencyException"/> (the InMemory provider enforces concurrency tokens the same way a relational provider would).</summary>
        private class ConcurrencyTestDbContext : TestDbContext
        {
            public ConcurrencyTestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<TestEntity>().Property(e => e.Name).IsConcurrencyToken();
            }
        }

        /// <summary>
        /// Backs a <see cref="TestDbContext"/> with a real SQLite database (in-memory, via a kept-open
        /// connection) instead of the EF Core InMemory provider, since InMemory treats transactions as a
        /// no-op and cannot exercise real commit/rollback semantics.
        /// </summary>
        private sealed class SqliteFixture : IDisposable
        {
            private readonly SqliteConnection _connection;
            public TestDbContext Context { get; }

            public SqliteFixture()
            {
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(_connection)
                    .Options;

                Context = new TestDbContext(options);
                Context.Database.EnsureCreated();
            }

            public void Dispose()
            {
                Context.Dispose();
                _connection.Dispose();
            }
        }

        [Fact]
        public async Task SaveChangesAsync_NoConflict_ReturnsSuccess()
        {
            using var context = CreateContext();
            context.TestEntities.Add(new TestEntity { Name = "A" });
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.SaveChangesAsync();

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SaveChangesAsync_OnDbUpdateException_ReturnsFailureInsteadOfThrowing()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ThrowingDbContext(options);
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.SaveChangesAsync();

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task SaveChangesAsync_OnDbUpdateConcurrencyException_ReturnsConflictErrorWithSafeMessage()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(dbName).Options;

            using (var seed = new ConcurrencyTestDbContext(options))
            {
                seed.TestEntities.Add(new TestEntity { Id = 1, Name = "A" });
                seed.SaveChanges();
            }

            using var context1 = new ConcurrencyTestDbContext(options);
            using var context2 = new ConcurrencyTestDbContext(options);
            var entity1 = await context1.TestEntities.FindAsync(1);
            var entity2 = await context2.TestEntities.FindAsync(1);
            entity1!.Name = "B";
            await context1.SaveChangesAsync();

            entity2!.Name = "C";
            var unitOfWork = new UnitOfWork(context2);
            var result = await unitOfWork.SaveChangesAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal(ErrorCodes.ConcurrencyConflict, result.Error.Code);
            Assert.False(string.IsNullOrWhiteSpace(result.Error.Message));
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException", result.Error.Message);
        }

        [Fact]
        public void HasChanges_NoPendingChanges_ReturnsFalse()
        {
            using var context = CreateContext();
            var unitOfWork = new UnitOfWork(context);

            Assert.False(unitOfWork.HasChanges());
        }

        [Fact]
        public void HasChanges_WithPendingAdd_ReturnsTrue()
        {
            using var context = CreateContext();
            context.TestEntities.Add(new TestEntity { Name = "A" });
            var unitOfWork = new UnitOfWork(context);

            Assert.True(unitOfWork.HasChanges());
        }

        [Fact]
        public async Task BeginTransactionAsync_WhileAlreadyActive_ReturnsFailure()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);
            await unitOfWork.BeginTransactionAsync();

            var result = await unitOfWork.BeginTransactionAsync();

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task CommitTransactionAsync_WithNoActiveTransaction_ReturnsFailure()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            var result = await unitOfWork.CommitTransactionAsync();

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task RollbackTransactionAsync_WithNoActiveTransaction_ReturnsFailure()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            var result = await unitOfWork.RollbackTransactionAsync();

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task BeginThenCommitTransactionAsync_PersistsChangesAndAllowsANewTransactionAfterward()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            var beginResult = await unitOfWork.BeginTransactionAsync();
            fixture.Context.TestEntities.Add(new TestEntity { Name = "A" });
            await unitOfWork.SaveChangesAsync();
            var commitResult = await unitOfWork.CommitTransactionAsync();
            var secondBeginResult = await unitOfWork.BeginTransactionAsync();

            Assert.True(beginResult.IsSuccess);
            Assert.True(commitResult.IsSuccess);
            Assert.True(secondBeginResult.IsSuccess);
            Assert.Single(fixture.Context.TestEntities.ToList());
        }

        [Fact]
        public async Task BeginThenRollbackTransactionAsync_DiscardsChanges()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            await unitOfWork.BeginTransactionAsync();
            fixture.Context.TestEntities.Add(new TestEntity { Name = "A" });
            await unitOfWork.SaveChangesAsync();
            var rollbackResult = await unitOfWork.RollbackTransactionAsync();

            Assert.True(rollbackResult.IsSuccess);
            Assert.Empty(fixture.Context.TestEntities.ToList());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_SuccessfulOperation_CommitsAndReturnsValue()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            var result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await fixture.Context.TestEntities.AddAsync(new TestEntity { Name = "A" }, ct);
                return Result.Success(42);
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
            Assert.Single(fixture.Context.TestEntities.ToList());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_FailingOperation_RollsBackAndPropagatesError()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);
            var error = Error.Business("TEST001", "Simulated business failure.");

            var result = await unitOfWork.ExecuteInTransactionAsync<int>(async ct =>
            {
                await fixture.Context.TestEntities.AddAsync(new TestEntity { Name = "A" }, ct);
                return Result.Fail<int>(error);
            });

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
            Assert.Empty(fixture.Context.TestEntities.ToList());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_NonGenericOverload_SuccessfulOperation_CommitsChanges()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);

            var result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await fixture.Context.TestEntities.AddAsync(new TestEntity { Name = "A" }, ct);
                return Result.Success();
            });

            Assert.True(result.IsSuccess);
            Assert.Single(fixture.Context.TestEntities.ToList());
        }

        [Fact]
        public async Task SaveChangesAsync_FailsWhileManualTransactionActive_RollsBackAndAllowsANewTransactionAfterward()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
            using var context = new ThrowingDbContext(options);
            context.Database.EnsureCreated();
            var unitOfWork = new UnitOfWork(context);
            await unitOfWork.BeginTransactionAsync();

            var saveResult = await unitOfWork.SaveChangesAsync();

            Assert.True(saveResult.IsFailure);

            // Only possible if the failed save above rolled back and cleared the transaction the manual
            // API left active — otherwise this would fail with TransactionAlreadyActive.
            var secondBeginResult = await unitOfWork.BeginTransactionAsync();
            Assert.True(secondBeginResult.IsSuccess);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhileManualTransactionActive_ReturnsFailureInsteadOfThrowing()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);
            await unitOfWork.BeginTransactionAsync();

            var result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await fixture.Context.TestEntities.AddAsync(new TestEntity { Name = "A" }, ct);
                return Result.Success();
            });

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task DisposeAsync_WithActiveTransaction_DisposesItWithoutThrowing()
        {
            using var fixture = new SqliteFixture();
            var unitOfWork = new UnitOfWork(fixture.Context);
            await unitOfWork.BeginTransactionAsync();

            var exception = await Record.ExceptionAsync(() => unitOfWork.DisposeAsync().AsTask());

            Assert.Null(exception);
        }
    }
}
