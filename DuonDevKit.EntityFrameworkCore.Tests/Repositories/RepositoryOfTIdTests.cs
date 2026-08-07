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
    }
}
