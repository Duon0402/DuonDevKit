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
    }
}
