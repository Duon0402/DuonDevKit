using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.EntityFrameworkCore.Tests.DependencyInjection
{
    public class ServiceCollectionExtensionsTests
    {
        private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            configure?.Invoke(services);
            services.AddDuonDevKitEntityFrameworkCore<TestDbContext>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task AddDuonDevKitEntityFrameworkCore_RegistersUnitOfWork()
        {
            // UnitOfWork only implements IAsyncDisposable (see UnitOfWork.DisposeAsync), so the DI scope
            // must be disposed asynchronously too.
            await using var scope = BuildProvider().CreateAsyncScope();

            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

            Assert.IsType<UnitOfWork>(unitOfWork);
        }

        [Fact]
        public async Task AddDuonDevKitEntityFrameworkCore_RegistersUntypedRepository()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            var repository = scope.ServiceProvider.GetService<IRepository<TestEntity>>();

            Assert.IsType<Repository<TestEntity>>(repository);
        }

        [Fact]
        public async Task AddDuonDevKitEntityFrameworkCore_RegistersTypedKeyRepository()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            var repository = scope.ServiceProvider.GetService<IRepository<KeyedTestEntity, string>>();

            Assert.IsType<Repository<KeyedTestEntity, string>>(repository);
        }

        [Fact]
        public async Task AddDuonDevKitEntityFrameworkCore_NoCurrentUserProviderRegistered_FallsBackToNullCurrentUserProvider()
        {
            await using var scope = BuildProvider().CreateAsyncScope();

            var currentUserProvider = scope.ServiceProvider.GetService<ICurrentUserProvider>();

            Assert.IsType<NullCurrentUserProvider>(currentUserProvider);
        }

        [Fact]
        public async Task AddDuonDevKitEntityFrameworkCore_ExistingCurrentUserProviderRegistered_DoesNotOverrideIt()
        {
            await using var scope = BuildProvider(services =>
                services.AddScoped<ICurrentUserProvider>(_ => new StubCurrentUserProvider { UserId = "alice" }))
                .CreateAsyncScope();

            var currentUserProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();

            Assert.Equal("alice", currentUserProvider.UserId);
        }
    }
}
