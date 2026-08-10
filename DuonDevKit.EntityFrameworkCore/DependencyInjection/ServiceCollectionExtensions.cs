using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuonDevKit.EntityFrameworkCore.DependencyInjection
{
    /// <summary>Registers this library's building blocks against a specific <see cref="DbContext"/> type.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IUnitOfWork"/>, <see cref="IRepository{T}"/>, and <see cref="IRepository{T, TId}"/>
        /// backed by <typeparamref name="TContext"/> (already registered separately, e.g. via
        /// <c>AddDbContext&lt;TContext&gt;</c>). Also registers <see cref="NullCurrentUserProvider"/> as a
        /// fallback <see cref="ICurrentUserProvider"/> if the app hasn't supplied its own. Safe to call
        /// alongside <c>DuonDevKit.Dapper</c>'s <c>AddDuonDevKitDapper&lt;TContext&gt;</c> — both map the
        /// same <see cref="DbContext"/> registration idempotently.
        /// </summary>
        /// <remarks>
        /// Supports exactly one <see cref="DbContext"/> type per <see cref="IServiceCollection"/> — the
        /// <c>DbContext</c> forwarding registration uses <c>TryAdd</c>, so calling this a second time for a
        /// different <typeparamref name="TContext"/> is a no-op for that forwarding, and every
        /// <see cref="IUnitOfWork"/>/<see cref="IRepository{T}"/> in the app resolves against whichever
        /// context type was registered first. Not supported: register a separate <see cref="IUnitOfWork"/>/
        /// repository set per context manually if the app genuinely needs more than one.
        /// </remarks>
        public static IServiceCollection AddDuonDevKitEntityFrameworkCore<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.TryAddScoped<ICurrentUserProvider, NullCurrentUserProvider>();

            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            return services;
        }
    }
}
