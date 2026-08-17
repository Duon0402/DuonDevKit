using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Caching.Tests
{
    public class HybridCacheServiceTests
    {
        // No Redis registered anywhere in these tests — HybridCache runs memory-only, which is
        // enough to exercise ICacheService's contract without needing a live Redis server.
        private static ICacheService BuildCacheService()
        {
            var services = new ServiceCollection();
            services.AddHybridCache();
            var provider = services.BuildServiceProvider();
            return new HybridCacheService(provider.GetRequiredService<HybridCache>());
        }

        [Fact]
        public async Task SetAsync_ThenGetAsync_ReturnsSome()
        {
            var cache = BuildCacheService();

            var setResult = await cache.SetAsync("key", "value");
            var getResult = await cache.GetAsync<string>("key");

            Assert.True(setResult.IsSuccess);
            Assert.True(getResult.IsSuccess);
            Assert.True(getResult.Value.HasValue);
            Assert.Equal("value", getResult.Value.Value);
        }

        [Fact]
        public async Task GetAsync_MissingKey_ReturnsNoneInsteadOfFailure()
        {
            var cache = BuildCacheService();

            var result = await cache.GetAsync<string>("missing");

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.HasValue);
        }

        [Fact]
        public async Task RemoveAsync_ExistingKey_RemovesIt()
        {
            var cache = BuildCacheService();
            await cache.SetAsync("key", "value");

            var removeResult = await cache.RemoveAsync("key");
            var getResult = await cache.GetAsync<string>("key");

            Assert.True(removeResult.IsSuccess);
            Assert.False(getResult.Value.HasValue);
        }

        [Fact]
        public async Task RemoveAsync_MissingKey_StillReturnsSuccess()
        {
            var cache = BuildCacheService();

            var result = await cache.RemoveAsync("missing");

            Assert.True(result.IsSuccess);
        }
    }
}