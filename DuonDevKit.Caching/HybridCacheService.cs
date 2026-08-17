using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;
using Microsoft.Extensions.Caching.Hybrid;

namespace DuonDevKit.Caching
{
    /// <summary>
    /// <see cref="ICacheService"/> backed by <see cref="HybridCache"/>. Infrastructure exceptions
    /// (Redis connectivity, serialization) are caught and surfaced as <see cref="Result"/> failures.
    /// </summary>
    public sealed class HybridCacheService : ICacheService
    {
        private readonly HybridCache _hybridCache;

        public HybridCacheService(HybridCache hybridCache)
        {
            ArgumentNullException.ThrowIfNull(hybridCache);
            _hybridCache = hybridCache;
        }

        public async Task<Result<Option<T>>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            // HybridCache has no plain "get without creating" method by design — GetOrCreateAsync's
            // factory only runs on a genuine miss, so a factory that throws signals "absent" without
            // ever caching anything for this call.
            try
            {
                var value = await _hybridCache.GetOrCreateAsync<T>(
                    key,
                    _ => throw new CacheMissException(),
                    options: null,
                    tags: null,
                    cancellationToken: cancellationToken);

                return Result.Success(Option<T>.Some(value!));
            }
            catch (CacheMissException)
            {
                return Result.Success(Option<T>.None);
            }
            catch (Exception ex)
            {
                return Result.Fail<Option<T>>(Error.Unexpected(ErrorCodes.CacheUnavailable, ex.Message));
            }
        }

        public async Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = expiration.HasValue ? new HybridCacheEntryOptions { Expiration = expiration } : null;
                await _hybridCache.SetAsync(key, value, options, tags: null, cancellationToken: cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail(Error.Unexpected(ErrorCodes.CacheUnavailable, ex.Message));
            }
        }

        public async Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hybridCache.RemoveAsync(key, cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail(Error.Unexpected(ErrorCodes.CacheUnavailable, ex.Message));
            }
        }

        /// <summary>Signals a genuine cache miss from <see cref="GetAsync{T}"/>'s synthetic factory, without creating an entry.</summary>
        private sealed class CacheMissException : Exception
        {
        }
    }
}