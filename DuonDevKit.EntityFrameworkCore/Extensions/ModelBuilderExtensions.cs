using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DuonDevKit.EntityFrameworkCore.Extensions
{
    /// <summary>Extension methods for configuring <see cref="ModelBuilder"/> conventions.</summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Applies a global query filter (<c>e => !e.IsDeleted</c>) to every entity type implementing
        /// <see cref="ISoftDelete"/>, so soft-deleted rows are excluded from queries by default. Call
        /// once near the end of <c>OnModelCreating</c>, after entity types are registered. Use
        /// <c>IgnoreQueryFilters()</c> on a query to include soft-deleted rows when needed.
        /// </summary>
        public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                    continue;

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
