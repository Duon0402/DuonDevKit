using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

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

        [Fact]
        public async Task SetAsync_NullValue_ThenGetAsync_ReturnsNoneWithoutThrowing()
        {
            var cache = BuildCacheService();

            var setResult = await cache.SetAsync<string?>("key", null);
            var getResult = await cache.GetAsync<string?>("key");

            Assert.True(setResult.IsSuccess);
            Assert.True(getResult.IsSuccess);
            Assert.False(getResult.Value.HasValue);
        }

        // Regression for the Critical finding: HybridCache's anti-stampede feature joins concurrent
        // calls for the same key into one in-flight operation. GetAsync's synthetic factory always
        // throws CacheMissException — if a concurrent GetOrCreateAsync call gets joined into that same
        // operation, it must still return its own factory's result rather than surfacing the
        // GetAsync-side exception as a spurious CacheUnavailable failure.
        [Fact]
        public async Task GetAsync_ConcurrentWith_GetOrCreateAsync_NeverCorruptsGetOrCreateResult()
        {
            var cache = BuildCacheService();
            const int Iterations = 200;

            var raceTasks = Enumerable.Range(0, Iterations)
                .Select(i => RaceGetAndGetOrCreateAsync(cache, $"stampede-{Guid.NewGuid()}-{i}"))
                .ToArray();

            await Task.WhenAll(raceTasks);
        }

        private static async Task RaceGetAndGetOrCreateAsync(ICacheService cache, string key)
        {
            var getTask = cache.GetAsync<string>(key);
            var getOrCreateTask = cache.GetOrCreateAsync(key, _ => Task.FromResult(Result.Success("value")));

            await Task.WhenAll(getTask, getOrCreateTask);

            var getOrCreateResult = await getOrCreateTask;
            Assert.True(getOrCreateResult.IsSuccess, $"key '{key}': expected success, got {getOrCreateResult.Error.Code}");
            Assert.Equal("value", getOrCreateResult.Value);
        }

        [Fact]
        public async Task GetAsync_HybridCacheThrows_ReturnsCacheUnavailableFailure()
        {
            var cache = new HybridCacheService(new ThrowingHybridCache());

            var result = await cache.GetAsync<string>("key");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCodes.CacheUnavailable, result.Error.Code);
        }

        [Fact]
        public async Task SetAsync_HybridCacheThrows_ReturnsCacheUnavailableFailure()
        {
            var cache = new HybridCacheService(new ThrowingHybridCache());

            var result = await cache.SetAsync("key", "value");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCodes.CacheUnavailable, result.Error.Code);
        }

        [Fact]
        public async Task RemoveAsync_HybridCacheThrows_ReturnsCacheUnavailableFailure()
        {
            var cache = new HybridCacheService(new ThrowingHybridCache());

            var result = await cache.RemoveAsync("key");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCodes.CacheUnavailable, result.Error.Code);
        }

        [Fact]
        public async Task GetOrCreateAsync_HybridCacheThrows_ReturnsCacheUnavailableFailure()
        {
            var cache = new HybridCacheService(new ThrowingHybridCache());

            var result = await cache.GetOrCreateAsync("key", _ => Task.FromResult(Result.Success("value")));

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCodes.CacheUnavailable, result.Error.Code);
        }

        // Minimal HybridCache double that forces every member HybridCacheService actually calls to
        // throw, so infrastructure-exception catch paths (untested until this finding) get exercised.
        private sealed class ThrowingHybridCache : HybridCache
        {
            public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("boom");

            public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("boom");

            public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("boom");

            public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }
    }
}