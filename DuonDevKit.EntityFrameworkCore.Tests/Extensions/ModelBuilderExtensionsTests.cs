using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests.Extensions
{
    public class ModelBuilderExtensionsTests
    {
        private static TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TestDbContext(options);
        }

        [Fact]
        public async Task Query_ExcludesSoftDeletedEntities_ByDefault()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "Active" },
                new TestEntity { Name = "Deleted", IsDeleted = true });
            await context.SaveChangesAsync();

            var results = context.TestEntities.ToList();

            Assert.Single(results);
            Assert.Equal("Active", results[0].Name);
        }

        [Fact]
        public async Task Query_WithIgnoreQueryFilters_IncludesSoftDeletedEntities()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "Active" },
                new TestEntity { Name = "Deleted", IsDeleted = true });
            await context.SaveChangesAsync();

            var results = context.TestEntities.IgnoreQueryFilters().ToList();

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task Query_OnEntityWithoutISoftDelete_IsUnaffected()
        {
            using var context = CreateContext();
            context.PlainEntities.AddRange(
                new PlainEntity { Name = "One" },
                new PlainEntity { Name = "Two" });
            await context.SaveChangesAsync();

            var results = context.PlainEntities.ToList();

            Assert.Equal(2, results.Count);
        }
    }
}
