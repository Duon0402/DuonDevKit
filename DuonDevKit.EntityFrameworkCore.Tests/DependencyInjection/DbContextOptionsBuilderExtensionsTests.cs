using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.EntityFrameworkCore.Tests.DependencyInjection
{
    public class DbContextOptionsBuilderExtensionsTests
    {
        [Fact]
        public async Task AddDuonDevKitAuditing_CurrentUserProviderRegistered_StampsAuditFieldsFromIt()
        {
            var services = new ServiceCollection();
            services.AddScoped<ICurrentUserProvider>(_ => new StubCurrentUserProvider { UserId = "alice" });
            services.AddDbContext<TestDbContext>((sp, options) =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()).AddDuonDevKitAuditing(sp));
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = new TestEntity { Name = "A" };

            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            Assert.Equal("alice", entity.CreatedBy);
        }

        [Fact]
        public async Task AddDuonDevKitAuditing_NoCurrentUserProviderRegistered_FallsBackToNullUser()
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>((sp, options) =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()).AddDuonDevKitAuditing(sp));
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = new TestEntity { Name = "A" };
            context.TestEntities.Add(entity);

            var exception = await Record.ExceptionAsync(() => context.SaveChangesAsync());

            Assert.Null(exception);
            Assert.NotEqual(default, entity.CreatedAt);
            Assert.Null(entity.CreatedBy);
        }
    }
}
