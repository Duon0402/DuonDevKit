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
        /// fallback <see cref="ICurrentUserProvider"/> if the app hasn't supplied its own.
        /// </summary>
        public static IServiceCollection AddDuonDevKitEntityFrameworkCore<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.TryAddScoped<ICurrentUserProvider, NullCurrentUserProvider>();

            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            return services;
        }
    }
}
