using DuonDevKit.Dapper.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Dapper.Tests.DependencyInjection
{
    public class ServiceCollectionExtensionsTests
    {
        private static IServiceCollection BuildServices()
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options => options.UseSqlite("DataSource=:memory:"));
            return services;
        }

        [Fact]
        public async Task AddDuonDevKitDapper_RegistersIDapperQueries()
        {
            var services = BuildServices();
            services.AddDuonDevKitDapper<TestDbContext>();

            await using var scope = services.BuildServiceProvider().CreateAsyncScope();
            var queries = scope.ServiceProvider.GetService<IDapperQueries>();

            Assert.IsType<DapperQueries>(queries);
        }

        [Fact]
        public void AddDuonDevKitDapper_CalledTwiceForSameContext_RegistersOnlyOnce()
        {
            var services = BuildServices();
            services.AddDuonDevKitDapper<TestDbContext>();
            services.AddDuonDevKitDapper<TestDbContext>();

            Assert.Single(services, d => d.ServiceType == typeof(IDapperQueries));
        }
    }
}
