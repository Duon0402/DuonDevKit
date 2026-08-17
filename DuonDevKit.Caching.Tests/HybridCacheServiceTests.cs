using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
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

        [Fact]
        public async Task GetOrCreateAsync_Miss_InvokesFactoryOnceThenServesFromCache()
        {
            var cache = BuildCacheService();
            var callCount = 0;

            var first = await cache.GetOrCreateAsync("key", _ =>
            {
                callCount++;
                return Task.FromResult(Result.Success("value"));
            });
            var second = await cache.GetOrCreateAsync("key", _ =>
            {
                callCount++;
                return Task.FromResult(Result.Success("value"));
            });

            Assert.True(first.IsSuccess);
            Assert.Equal("value", first.Value);
            Assert.True(second.IsSuccess);
            Assert.Equal("value", second.Value);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetOrCreateAsync_FactoryFails_DoesNotCacheAndReturnsFailure()
        {
            var cache = BuildCacheService();
            var error = Error.Unexpected("TEST001", "boom");
            var callCount = 0;

            var first = await cache.GetOrCreateAsync<string>("key", _ =>
            {
                callCount++;
                return Task.FromResult(Result.Fail<string>(error));
            });
            var second = await cache.GetOrCreateAsync<string>("key", _ =>
            {
                callCount++;
                return Task.FromResult(Result.Fail<string>(error));
            });

            Assert.True(first.IsFailure);
            Assert.Equal(error, first.Error);
            Assert.True(second.IsFailure);
            Assert.Equal(error, second.Error);
            Assert.Equal(2, callCount); // re-invoked both times — a failure was never cached
        }
    }
}