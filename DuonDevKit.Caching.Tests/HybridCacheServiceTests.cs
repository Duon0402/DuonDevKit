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

        // Regression: GetAsync only caught its own synthetic CacheMissException, not
        // CacheFactoryFailedException — if GetAsync's call is joined (via HybridCache's stampede-join)
        // into a concurrent GetOrCreateAsync call whose factory fails, that failure belongs to the other
        // call, not this one; it must not surface as a spurious CacheUnavailable infrastructure failure.
        //
        // The join is forced deterministically rather than raced blindly: GetOrCreateAsync's factory
        // blocks after starting (which is after HybridCache has already registered it as the in-flight
        // leader for this key), so GetAsync is guaranteed to be issued while that operation is still
        // pending and therefore joins it.
        [Fact]
        public async Task GetAsync_ConcurrentWith_FailingGetOrCreateAsync_NeverSurfacesTheOtherCallsFactoryFailure()
        {
            var cache = BuildCacheService();
            var key = $"stampede-fail-{Guid.NewGuid()}";
            var error = Error.Unexpected("TEST-STAMPEDE", "boom");
            var factoryStarted = new TaskCompletionSource();
            var releaseFactory = new TaskCompletionSource();

            var getOrCreateTask = cache.GetOrCreateAsync<string>(key, async _ =>
            {
                factoryStarted.SetResult();
                await releaseFactory.Task;
                return Result.Fail<string>(error);
            });

            await factoryStarted.Task;
            var getTask = cache.GetAsync<string>(key);
            releaseFactory.SetResult();

            var getResult = await getTask;
            Assert.True(getResult.IsSuccess, $"GetAsync must never fail due to a concurrent call's factory failure, got {(getResult.IsFailure ? getResult.Error.Code : "")}");
            Assert.False(getResult.Value.HasValue);

            var getOrCreateResult = await getOrCreateTask;
            Assert.True(getOrCreateResult.IsFailure);
            Assert.Equal(error, getOrCreateResult.Error);
        }

        // Regression: when GetOrCreateAsync loses HybridCache's stampede-join (a concurrent GetAsync call
        // on the same key won it) and falls back to invoking the factory directly, it must still persist
        // the computed value — otherwise the entry stays empty and the factory keeps being re-invoked on
        // every subsequent racing call, silently degrading the cache to near-zero hit rate for that key.
        //
        // The join is forced deterministically: this test drives HybridCache directly (via
        // InternalsVisibleTo) with a blocked factory throwing the same CacheMissException GetAsync's own
        // synthetic factory throws, simulating a slow-to-miss GetAsync call that wins the leader role for
        // this key. GetOrCreateAsync is then guaranteed to be issued while that "GetAsync" is still
        // pending, so it joins it and observes the miss.
        [Fact]
        public async Task GetOrCreateAsync_LosesStampedeJoinToAGetAsyncLeader_StillPersistsItsOwnComputedValue()
        {
            var (cache, hybridCache) = BuildCacheServiceWithHybridCache();
            var key = $"stampede-persist-{Guid.NewGuid()}";
            var leaderStarted = new TaskCompletionSource();
            var releaseLeader = new TaskCompletionSource();

            // Matches HybridCacheService.GetAsync's own call shape exactly (single-type-param overload,
            // not the explicit TState one) — HybridCache tracks a per-key type signature and rejects
            // mixing shapes for the same key with a CACHE001 error.
            var leaderTask = hybridCache.GetOrCreateAsync<string>(key, async _ =>
            {
                leaderStarted.SetResult();
                await releaseLeader.Task;
                throw new HybridCacheService.CacheMissException();
            }).AsTask();

            await leaderStarted.Task;
            var getOrCreateTask = cache.GetOrCreateAsync(key, _ => Task.FromResult(Result.Success("value")));
            releaseLeader.SetResult();

            await Assert.ThrowsAsync<HybridCacheService.CacheMissException>(() => leaderTask);

            var getOrCreateResult = await getOrCreateTask;
            Assert.True(getOrCreateResult.IsSuccess, getOrCreateResult.IsFailure ? getOrCreateResult.Error.Code : "");
            Assert.Equal("value", getOrCreateResult.Value);

            var verifyResult = await cache.GetAsync<string>(key);
            Assert.True(verifyResult.IsSuccess, verifyResult.IsFailure ? verifyResult.Error.Code : "");
            Assert.True(verifyResult.Value.HasValue, "expected the joiner's computed value to have been persisted, but the cache is empty");
            Assert.Equal("value", verifyResult.Value.Value);
        }

        private static (ICacheService Cache, HybridCache HybridCache) BuildCacheServiceWithHybridCache()
        {
            var services = new ServiceCollection();
            services.AddHybridCache();
            var provider = services.BuildServiceProvider();
            var hybridCache = provider.GetRequiredService<HybridCache>();
            return (new HybridCacheService(hybridCache), hybridCache);
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