using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuonDevKit.Caching.DependencyInjection
{
    /// <summary>Registers <see cref="ICacheService"/>.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ICacheService"/> backed by <see cref="HybridCache"/>. When
        /// <paramref name="settings"/>.<see cref="CachingSettings.RedisConnectionString"/> is set, also
        /// registers Redis (via <c>AddStackExchangeRedisCache</c>) as the distributed (L2) tier;
        /// otherwise <see cref="HybridCache"/> runs memory-only. Safe to call more than once.
        /// </summary>
        public static IServiceCollection AddDuonDevKitCaching(this IServiceCollection services, CachingSettings? settings = null)
        {
            settings ??= new CachingSettings();

            if (!string.IsNullOrEmpty(settings.RedisConnectionString))
            {
                services.AddStackExchangeRedisCache(redis => redis.Configuration = settings.RedisConnectionString);
            }

            services.AddHybridCache(hybrid =>
            {
                hybrid.DefaultEntryOptions = new HybridCacheEntryOptions { Expiration = settings.DefaultExpiration };
            });

            services.TryAddSingleton<ICacheService, HybridCacheService>();

            return services;
        }
    }
}
