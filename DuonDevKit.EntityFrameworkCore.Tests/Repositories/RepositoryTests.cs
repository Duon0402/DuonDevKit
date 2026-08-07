using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests.Repositories
{
    public class RepositoryTests
    {
        private static TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TestDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingEntity_ReturnsSuccess()
        {
            using var context = CreateContext();
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.GetByIdAsync([entity.Id]);

            Assert.True(result.IsSuccess);
            Assert.Equal("A", result.Value.Name);
        }

        [Fact]
        public async Task GetByIdAsync_MissingEntity_ReturnsFailure()
        {
            using var context = CreateContext();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.GetByIdAsync([999]);

            Assert.True(result.IsFailure);
            Assert.NotEqual(default, result.Error);
        }

        [Fact]
        public async Task ListAsync_NoFilter_ReturnsAllNonDeleted()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
                new TestEntity { Name = "C", IsDeleted = true });
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.ListAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public async Task ListAsync_WithFilter_ReturnsMatchingOnly()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "Match" },
                new TestEntity { Name = "Other" });
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.ListAsync(e => e.Name == "Match");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("Match", result.Value[0].Name);
        }

        [Fact]
        public async Task AddAsync_ValidEntity_ReturnsSuccessAndPersists()
        {
            using var context = CreateContext();
            var repository = new Repository<TestEntity>(context);
            var entity = new TestEntity { Name = "New" };

            var result = await repository.AddAsync(entity);
            await context.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(context.TestEntities.ToList());
        }

        [Fact]
        public async Task Remove_EntityImplementingISoftDelete_SetsIsDeletedInsteadOfHardDelete()
        {
            using var context = CreateContext();
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = repository.Remove(entity);
            await context.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            var stillThere = context.TestEntities.IgnoreQueryFilters().Single(e => e.Id == entity.Id);
            Assert.True(stillThere.IsDeleted);
        }

        [Fact]
        public async Task Remove_PlainEntityWithoutISoftDelete_HardDeletes()
        {
            using var context = CreateContext();
            var entity = new PlainEntity { Name = "A" };
            context.PlainEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<PlainEntity>(context);

            var result = repository.Remove(entity);
            await context.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            Assert.Null(await context.PlainEntities.FindAsync(entity.Id));
        }
    }
}
