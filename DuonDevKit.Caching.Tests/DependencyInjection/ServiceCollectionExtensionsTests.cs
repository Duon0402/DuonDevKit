using DuonDevKit.Caching.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Caching.Tests.DependencyInjection
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDuonDevKitCaching_NoSettings_RegistersMemoryOnlyICacheService()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitCaching();

            using var provider = services.BuildServiceProvider();

            Assert.IsType<HybridCacheService>(provider.GetService<ICacheService>());
            Assert.Null(provider.GetService<IDistributedCache>());
        }

        [Fact]
        public void AddDuonDevKitCaching_RedisConnectionStringConfigured_RegistersDistributedCache()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitCaching(new CachingSettings { RedisConnectionString = "localhost:6379" });

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IDistributedCache>());
        }

        [Fact]
        public void AddDuonDevKitCaching_CalledTwice_RegistersICacheServiceOnlyOnce()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitCaching();
            services.AddDuonDevKitCaching();

            Assert.Single(services, d => d.ServiceType == typeof(ICacheService));
        }
    }
}
