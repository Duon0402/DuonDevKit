using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuonDevKit.Dapper.DependencyInjection
{
    /// <summary>Registers <see cref="IDapperQueries"/> against a specific <see cref="DbContext"/> type.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IDapperQueries"/> backed by <typeparamref name="TContext"/> (already
        /// registered separately, e.g. via <c>AddDbContext&lt;TContext&gt;</c>). Safe to call alongside
        /// <c>DuonDevKit.EntityFrameworkCore</c>'s <c>AddDuonDevKitEntityFrameworkCore&lt;TContext&gt;</c> —
        /// both map the same <see cref="DbContext"/> registration idempotently.
        /// </summary>
        /// <remarks>
        /// Supports exactly one <see cref="DbContext"/> type per <see cref="IServiceCollection"/> — the
        /// <c>DbContext</c> forwarding registration uses <c>TryAdd</c>, so calling this a second time for a
        /// different <typeparamref name="TContext"/> is a no-op for that forwarding, and every
        /// <see cref="IDapperQueries"/> in the app resolves against whichever context type was registered
        /// first. Not supported: register a separate <see cref="IDapperQueries"/> per context manually if
        /// the app genuinely needs more than one.
        /// </remarks>
        public static IServiceCollection AddDuonDevKitDapper<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
            services.TryAddScoped<IDapperQueries, DapperQueries>();

            return services;
        }
    }
}
