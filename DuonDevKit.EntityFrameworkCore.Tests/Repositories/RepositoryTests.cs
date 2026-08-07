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
        public async Task ListPagedAsync_SecondPage_ReturnsCorrectSliceAndTotalCount()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
                new TestEntity { Name = "C" },
                new TestEntity { Name = "D" },
                new TestEntity { Name = "E" });
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.ListPagedAsync(pageNumber: 2, pageSize: 2, orderBy: q => q.OrderBy(e => e.Name));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.Equal(["C", "D"], result.Value.Items.Select(e => e.Name));
            Assert.Equal(5, result.Value.TotalCount);
            Assert.Equal(3, result.Value.TotalPages);
            Assert.True(result.Value.HasPreviousPage);
            Assert.True(result.Value.HasNextPage);
        }

        [Fact]
        public async Task ListPagedAsync_WithFilter_PagesOverMatchingOnly()
        {
            using var context = CreateContext();
            context.TestEntities.AddRange(
                new TestEntity { Name = "Match A" },
                new TestEntity { Name = "Other" },
                new TestEntity { Name = "Match B" });
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.ListPagedAsync(
                pageNumber: 1,
                pageSize: 10,
                filter: e => e.Name.StartsWith("Match"),
                orderBy: q => q.OrderBy(e => e.Name));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.Equal(2, result.Value.Items.Count);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(-1, 10)]
        public async Task ListPagedAsync_InvalidPageNumberOrSize_ReturnsFailure(int pageNumber, int pageSize)
        {
            using var context = CreateContext();
            var repository = new Repository<TestEntity>(context);

            var result = await repository.ListPagedAsync(pageNumber, pageSize);

            Assert.True(result.IsFailure);
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

        [Fact]
        public async Task Query_DefaultTracking_ChangesPersistOnSave()
        {
            var databaseName = Guid.NewGuid().ToString();
            using var context = CreateContext(databaseName);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var found = repository.Query().Single(e => e.Id == entity.Id);
            found.Name = "Changed";
            await context.SaveChangesAsync();

            using var verifyContext = CreateContext(databaseName);
            Assert.Equal("Changed", (await verifyContext.TestEntities.FindAsync(entity.Id))!.Name);
        }

        [Fact]
        public async Task Query_AsNoTracking_ChangesDoNotPersistOnSave()
        {
            var databaseName = Guid.NewGuid().ToString();
            using var context = CreateContext(databaseName);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var found = repository.Query(asNoTracking: true).Single(e => e.Id == entity.Id);
            found.Name = "Changed";
            await context.SaveChangesAsync(); // no-op for `found` — it was never tracked

            using var verifyContext = CreateContext(databaseName);
            Assert.Equal("A", (await verifyContext.TestEntities.FindAsync(entity.Id))!.Name);
        }

        [Fact]
        public async Task FindOneAsync_MatchingEntity_ReturnsSome()
        {
            using var context = CreateContext();
            context.TestEntities.Add(new TestEntity { Name = "A" });
            await context.SaveChangesAsync();
            var repository = new Repository<TestEntity>(context);

            var option = await repository.FindOneAsync(e => e.Name == "A");

            Assert.True(option.HasValue);
            Assert.Equal("A", option.Value.Name);
        }

        [Fact]
        public async Task FindOneAsync_NoMatch_ReturnsNoneInsteadOfFailure()
        {
            using var context = CreateContext();
            var repository = new Repository<TestEntity>(context);

            var option = await repository.FindOneAsync(e => e.Name == "Missing");

            Assert.False(option.HasValue);
        }

        [Fact]
        public async Task FindOneAsync_WithInclude_EagerLoadsNavigationProperty()
        {
            using var context = CreateContext();
            var post = new BlogPostEntity { Title = "Post A" };
            post.Comments.Add(new CommentEntity { Text = "Nice!" });
            context.BlogPosts.Add(post);
            await context.SaveChangesAsync();
            var repository = new Repository<BlogPostEntity>(context);

            var option = await repository.FindOneAsync(
                p => p.Id == post.Id,
                include: q => q.Include(p => p.Comments));

            Assert.True(option.HasValue);
            Assert.Single(option.Value.Comments);
            Assert.Equal("Nice!", option.Value.Comments[0].Text);
        }

        [Fact]
        public async Task ListAsync_WithInclude_EagerLoadsNavigationProperty()
        {
            using var context = CreateContext();
            var post = new BlogPostEntity { Title = "Post A" };
            post.Comments.Add(new CommentEntity { Text = "Nice!" });
            context.BlogPosts.Add(post);
            await context.SaveChangesAsync();
            var repository = new Repository<BlogPostEntity>(context);

            var result = await repository.ListAsync(include: q => q.Include(p => p.Comments));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value[0].Comments);
        }

        [Fact]
        public async Task ListPagedAsync_WithInclude_EagerLoadsNavigationProperty()
        {
            using var context = CreateContext();
            var post = new BlogPostEntity { Title = "Post A" };
            post.Comments.Add(new CommentEntity { Text = "Nice!" });
            context.BlogPosts.Add(post);
            await context.SaveChangesAsync();
            var repository = new Repository<BlogPostEntity>(context);

            var result = await repository.ListPagedAsync(
                pageNumber: 1,
                pageSize: 10,
                include: q => q.Include(p => p.Comments));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items[0].Comments);
        }
    }
}
