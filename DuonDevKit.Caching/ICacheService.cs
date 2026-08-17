using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Caching
{
    /// <summary>
    /// A cache abstraction consistent with the rest of DuonDevKit: infrastructure failures (e.g.
    /// Redis unavailable) surface as <see cref="Result"/>/<see cref="Result{T}"/> failures instead
    /// of thrown exceptions, and a missing key is <see cref="Option{T}.None"/> rather than a failure.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Returns the cached value for <paramref name="key"/>, or <see cref="Option{T}.None"/> if absent. Never creates an entry.</summary>
        Task<Result<Option<T>>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>, overwriting any existing entry.</summary>
        Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>Removes the entry for <paramref name="key"/> if present. Succeeds even when the key doesn't exist.</summary>
        Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the cached value for <paramref name="key"/> if present; otherwise invokes
        /// <paramref name="factory"/>, caches its value on success, and returns that. A
        /// <see cref="Result{T}"/> failure returned by <paramref name="factory"/> is never cached —
        /// every call re-invokes the factory until it succeeds.
        /// </summary>
        Task<Result<T>> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<Result<T>>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    }
}