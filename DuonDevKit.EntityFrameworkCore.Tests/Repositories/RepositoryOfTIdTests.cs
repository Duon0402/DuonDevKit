using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests.Repositories
{
    public class RepositoryOfTIdTests
    {
        private static TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TestDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_ByTypedId_ExistingEntity_ReturnsSuccess()
        {
            using var context = CreateContext();
            var entity = new KeyedTestEntity { Id = "abc", Name = "A" };
            context.KeyedTestEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<KeyedTestEntity, string>(context);

            var result = await repository.GetByIdAsync("abc");

            Assert.True(result.IsSuccess);
            Assert.Equal("A", result.Value.Name);
        }

        [Fact]
        public async Task GetByIdAsync_ByTypedId_MissingEntity_ReturnsFailure()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context);

            var result = await repository.GetByIdAsync("missing");

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task Repository_InheritsBaseListAndAddBehavior()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context);
            var entity = new KeyedTestEntity { Id = "abc", Name = "A" };

            var addResult = await repository.AddAsync(entity);
            await context.SaveChangesAsync();
            var listResult = await repository.ListAsync();

            Assert.True(addResult.IsSuccess);
            Assert.True(listResult.IsSuccess);
            Assert.Single(listResult.Value);
            Assert.Equal("A", listResult.Value[0].Name);
        }

        [Fact]
        public async Task AddAsync_NoIdGeneratorRegistered_RequiresCallerToSetIdExplicitly()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context);
            var entity = new KeyedTestEntity { Name = "A" }; // Id left unset, no generator to fill it in

            // EF Core can't track an entity with a null string key at all — without an
            // IEntityIdGenerator<TId>, the caller must set Id themselves before calling AddAsync.
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(entity));
        }

        [Fact]
        public async Task AddAsync_WithIdGenerator_AssignsIdWhenMissing()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context, new GuidStringIdGenerator());
            var entity = new KeyedTestEntity { Name = "A" }; // Id left unset

            await repository.AddAsync(entity);

            Assert.False(string.IsNullOrEmpty(entity.Id));
        }

        [Fact]
        public async Task AddAsync_WithIdGenerator_DoesNotOverwriteAnExplicitlySetId()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context, new GuidStringIdGenerator());
            var entity = new KeyedTestEntity { Id = "explicit-id", Name = "A" };

            await repository.AddAsync(entity);

            Assert.Equal("explicit-id", entity.Id);
        }

        [Fact]
        public async Task AddRangeAsync_WithIdGenerator_AssignsIdToEveryEntityMissingOne()
        {
            using var context = CreateContext();
            var repository = new Repository<KeyedTestEntity, string>(context, new GuidStringIdGenerator());
            var withId = new KeyedTestEntity { Id = "explicit-id", Name = "A" };
            var withoutId = new KeyedTestEntity { Name = "B" };

            await repository.AddRangeAsync([withId, withoutId]);

            Assert.Equal("explicit-id", withId.Id);
            Assert.False(string.IsNullOrEmpty(withoutId.Id));
        }
    }
}
