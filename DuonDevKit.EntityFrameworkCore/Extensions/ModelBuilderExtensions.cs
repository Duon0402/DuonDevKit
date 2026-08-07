using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace DuonDevKit.EntityFrameworkCore.Extensions
{
    /// <summary>Extension methods for configuring <see cref="ModelBuilder"/> conventions.</summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Applies a global query filter excluding soft-deleted rows to every entity type implementing
        /// <see cref="ISoftDelete"/>, so they are excluded from queries by default. Call once near the
        /// end of <c>OnModelCreating</c>, after entity types are registered. Use
        /// <c>IgnoreQueryFilters()</c> on a query to include soft-deleted rows when needed.
        /// </summary>
        /// <remarks>
        /// EF Core only allows a query filter to be declared on the root entity type of an inheritance
        /// hierarchy (it then applies to every derived type automatically). This method therefore always
        /// builds the filter against the hierarchy's root, whether or not the root itself implements
        /// <see cref="ISoftDelete"/> — e.g. a TPH hierarchy where only some subtypes opt into soft-delete
        /// is filtered correctly, instead of throwing at model-build time.
        /// </remarks>
        public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
        {
            var allTypes = modelBuilder.Model.GetEntityTypes().ToList();
            var handledRoots = new HashSet<IMutableEntityType>();

            foreach (var entityType in allTypes)
            {
                var root = GetRoot(entityType);
                if (!handledRoots.Add(root))
                    continue;

                if (typeof(ISoftDelete).IsAssignableFrom(root.ClrType))
                {
                    ApplyFilter(modelBuilder, root, root.ClrType);
                    continue;
                }

                var softDeleteTypes = allTypes
                    .Where(t => GetRoot(t) == root && typeof(ISoftDelete).IsAssignableFrom(t.ClrType))
                    .ToList();

                if (softDeleteTypes.Count > 0)
                    ApplyMixedHierarchyFilter(modelBuilder, root, softDeleteTypes);
            }
        }

        private static IMutableEntityType GetRoot(IMutableEntityType entityType)
        {
            var current = entityType;
            while (current.BaseType is not null)
                current = current.BaseType;

            return current;
        }

        private static void ApplyFilter(ModelBuilder modelBuilder, IMutableEntityType root, Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(root.ClrType).HasQueryFilter(lambda);
        }

        private static void ApplyMixedHierarchyFilter(ModelBuilder modelBuilder, IMutableEntityType root, IReadOnlyList<IMutableEntityType> softDeleteTypes)
        {
            var parameter = Expression.Parameter(root.ClrType, "e");

            Expression? anyDeleted = null;
            foreach (var softDeleteType in softDeleteTypes)
            {
                var isType = Expression.TypeIs(parameter, softDeleteType.ClrType);
                var cast = Expression.Convert(parameter, softDeleteType.ClrType);
                var isDeleted = Expression.Property(cast, nameof(ISoftDelete.IsDeleted));
                var clause = Expression.AndAlso(isType, isDeleted);
                anyDeleted = anyDeleted is null ? clause : Expression.OrElse(anyDeleted, clause);
            }

            var condition = Expression.Not(anyDeleted!);
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(root.ClrType).HasQueryFilter(lambda);
        }
    }
}
