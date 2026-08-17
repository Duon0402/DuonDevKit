namespace DuonDevKit.Caching
{
    /// <summary>Configuration for <see cref="DependencyInjection.ServiceCollectionExtensions.AddDuonDevKitCaching"/>.</summary>
    public sealed class CachingSettings
    {
        /// <summary>The expiration applied when a cache call doesn't specify one explicitly. Defaults to 5 minutes.</summary>
        public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The Redis connection string for the distributed (L2) cache tier. Leave <c>null</c> to run
        /// memory-only — <see cref="ICacheService"/>'s API is identical either way.
        /// </summary>
        public string? RedisConnectionString { get; init; }
    }
}