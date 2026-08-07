using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests.Repositories
{
    public class RepositoryTests
    {
        private static TestDbContext CreateContext()
            => CreateContext(Guid.NewGuid().ToString());

        private static TestDbContext CreateContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName)
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

        [Fact]
        public async Task Remove_DetachedSoftDeleteEntity_AttachesAndPersistsInsteadOfSilentlyNoOp()
        {
            var databaseName = Guid.NewGuid().ToString();
            int id;
            using (var context = CreateContext(databaseName))
            {
                var entity = new TestEntity { Name = "A" };
                context.TestEntities.Add(entity);
                await context.SaveChangesAsync();
                id = entity.Id;
            }

            using var removeContext = CreateContext(databaseName);
            var repository = new Repository<TestEntity>(removeContext);
            var detached = new TestEntity { Id = id };

            var result = repository.Remove(detached);
            await removeContext.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            using var verifyContext = CreateContext(databaseName);
            var stillThere = await verifyContext.TestEntities.IgnoreQueryFilters().SingleAsync(e => e.Id == id);
            Assert.True(stillThere.IsDeleted);
        }

        [Fact]
        public async Task AddRangeAsync_ValidEntities_ReturnsSuccessAndPersistsAll()
        {
            using var context = CreateContext();
            var repository = new Repository<TestEntity>(context);
            var entities = new[] { new TestEntity { Name = "A" }, new TestEntity { Name = "B" } };

            var result = await repository.AddRangeAsync(entities);
            await context.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(2, context.TestEntities.Count());
        }

        [Fact]
        public async Task Update_DetachedEntity_AttachesAndPersistsChanges()
        {
            var databaseName = Guid.NewGuid().ToString();
            int id;
            using (var context = CreateContext(databaseName))
            {
                var entity = new TestEntity { Name = "A" };
                context.TestEntities.Add(entity);
                await context.SaveChangesAsync();
                id = entity.Id;
            }

            using var updateContext = CreateContext(databaseName);
            var repository = new Repository<TestEntity>(updateContext);
            var detached = new TestEntity { Id = id, Name = "A-changed" };

            var result = repository.Update(detached);
            await updateContext.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            using var verifyContext = CreateContext(databaseName);
            var reloaded = await verifyContext.TestEntities.FindAsync(id);
            Assert.Equal("A-changed", reloaded!.Name);
        }

        [Fact]
        public async Task UpdateRange_DetachedEntities_AttachesAndPersistsAllChanges()
        {
            var databaseName = Guid.NewGuid().ToString();
            int firstId, secondId;
            using (var context = CreateContext(databaseName))
            {
                var first = new TestEntity { Name = "A" };
                var second = new TestEntity { Name = "B" };
                context.TestEntities.AddRange(first, second);
                await context.SaveChangesAsync();
                firstId = first.Id;
                secondId = second.Id;
            }

            using var updateContext = CreateContext(databaseName);
            var repository = new Repository<TestEntity>(updateContext);
            var detached = new[]
            {
                new TestEntity { Id = firstId, Name = "A-changed" },
                new TestEntity { Id = secondId, Name = "B-changed" },
            };

            var result = repository.UpdateRange(detached);
            await updateContext.SaveChangesAsync();

            Assert.True(result.IsSuccess);
            using var verifyContext = CreateContext(databaseName);
            Assert.Equal("A-changed", (await verifyContext.TestEntities.FindAsync(firstId))!.Name);
            Assert.Equal("B-changed", (await verifyContext.TestEntities.FindAsync(secondId))!.Name);
        }

        [Fact]
        public async Task RemoveRange_MixOfSoftAndHardDeleteEntities_AppliesEachEntitysOwnDeleteStrategy()
        {
            using var context = CreateContext();
            var softDeletable = new TestEntity { Name = "A" };
            var plain = new PlainEntity { Name = "B" };
            context.TestEntities.Add(softDeletable);
            context.PlainEntities.Add(plain);
            await context.SaveChangesAsync();
            var softDeleteRepo = new Repository<TestEntity>(context);
            var plainRepo = new Repository<PlainEntity>(context);

            var softResult = softDeleteRepo.RemoveRange([softDeletable]);
            var plainResult = plainRepo.RemoveRange([plain]);
            await context.SaveChangesAsync();

            Assert.True(softResult.IsSuccess);
            Assert.True(plainResult.IsSuccess);
            Assert.True(context.TestEntities.IgnoreQueryFilters().Single(e => e.Id == softDeletable.Id).IsDeleted);
            Assert.Null(await context.PlainEntities.FindAsync(plain.Id));
        }
    }
}
