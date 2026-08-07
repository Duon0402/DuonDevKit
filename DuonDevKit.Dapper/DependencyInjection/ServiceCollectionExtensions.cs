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
        public static IServiceCollection AddDuonDevKitDapper<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
            services.AddScoped<IDapperQueries, DapperQueries>();

            return services;
        }
    }
}
