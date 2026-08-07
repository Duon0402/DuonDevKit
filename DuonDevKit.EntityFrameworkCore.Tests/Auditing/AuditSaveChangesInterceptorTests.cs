using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests.Auditing
{
    public class AuditSaveChangesInterceptorTests
    {
        private static TestDbContext CreateContext(StubCurrentUserProvider currentUserProvider)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider))
                .Options;

            return new TestDbContext(options);
        }

        [Fact]
        public async Task AddingEntity_WithDefaultCreatedAt_FillsCreatedAtAndCreatedBy()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new TestEntity { Name = "A" };

            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            Assert.NotEqual(default, entity.CreatedAt);
            Assert.Equal("alice", entity.CreatedBy);
        }

        [Fact]
        public async Task AddingEntity_WithCreatedAtAlreadySet_DoesNotOverwrite()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var customCreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entity = new TestEntity { Name = "A", CreatedAt = customCreatedAt, CreatedBy = "migration-script" };

            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            Assert.Equal(customCreatedAt, entity.CreatedAt);
            Assert.Equal("migration-script", entity.CreatedBy);
        }

        [Fact]
        public async Task UpdatingEntity_WithNoExplicitUpdatedFields_FillsUpdatedAtAndUpdatedBy()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            entity.Name = "B";
            await context.SaveChangesAsync();

            Assert.NotNull(entity.UpdatedAt);
            Assert.True((DateTime.UtcNow - entity.UpdatedAt!.Value).TotalSeconds < 5);
            Assert.Equal("alice", entity.UpdatedBy);

            user.UserId = "bob";
            entity.Name = "C";
            await context.SaveChangesAsync();

            Assert.True((DateTime.UtcNow - entity.UpdatedAt!.Value).TotalSeconds < 5);
            Assert.Equal("bob", entity.UpdatedBy);
        }

        [Fact]
        public async Task UpdatingEntity_WithExplicitUpdatedBy_DoesNotOverwrite()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            entity.Name = "B";
            entity.UpdatedBy = "manual-override";
            await context.SaveChangesAsync();

            Assert.Equal("manual-override", entity.UpdatedBy);
        }

        [Fact]
        public async Task SoftDeletingEntity_WithNoExplicitDeletedFields_FillsDeletedAtAndDeletedBy()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            entity.IsDeleted = true;
            await context.SaveChangesAsync();

            Assert.NotNull(entity.DeletedAt);
            Assert.Equal("alice", entity.DeletedBy);
        }

        [Fact]
        public async Task SoftDeletingEntity_WithExplicitDeletedBy_DoesNotOverwrite()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            entity.IsDeleted = true;
            entity.DeletedBy = "manual-override";
            await context.SaveChangesAsync();

            Assert.Equal("manual-override", entity.DeletedBy);
        }

        [Fact]
        public async Task AddingPlainEntity_WithNoAuditInterfaces_DoesNotThrow()
        {
            var user = new StubCurrentUserProvider { UserId = "alice" };
            using var context = CreateContext(user);
            var entity = new PlainEntity { Name = "A" };

            context.PlainEntities.Add(entity);
            var exception = await Record.ExceptionAsync(() => context.SaveChangesAsync());

            Assert.Null(exception);
        }
    }
}
