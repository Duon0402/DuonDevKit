using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// EF Core <see cref="SaveChangesInterceptor"/> that auto-fills audit fields on entities
    /// implementing <see cref="ICanCreate"/>, <see cref="ICanUpdate"/>, or <see cref="ISoftDelete"/>
    /// whenever the consuming app didn't already set them explicitly. Register via
    /// <c>optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider))</c>.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor(ICurrentUserProvider currentUserProvider) : SaveChangesInterceptor
    {
        private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

        /// <inheritdoc />
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyAudit(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAudit(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>Scans the change tracker and fills audit fields per entry state, respecting any value the caller already set.</summary>
        private void ApplyAudit(DbContext? context)
        {
            if (context is null) return;

            var now = DateTime.UtcNow;
            var userId = _currentUserProvider.UserId;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added && entry.Entity is ICanCreate creatable)
                {
                    if (creatable.CreatedAt == default) creatable.CreatedAt = now;
                    if (creatable.CreatedBy is null) creatable.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified && entry.Entity is ICanUpdate updatable)
                {
                    var updatedAtProperty = entry.Property(nameof(ICanUpdate.UpdatedAt));
                    if (Equals(updatedAtProperty.OriginalValue, updatedAtProperty.CurrentValue))
                        updatable.UpdatedAt = now;

                    var updatedByProperty = entry.Property(nameof(ICanUpdate.UpdatedBy));
                    if (Equals(updatedByProperty.OriginalValue, updatedByProperty.CurrentValue))
                        updatable.UpdatedBy = userId;
                }

                if (entry.State == EntityState.Modified && entry.Entity is ISoftDelete softDeletable && softDeletable.IsDeleted)
                {
                    if (softDeletable.DeletedAt is null) softDeletable.DeletedAt = now;
                    if (softDeletable.DeletedBy is null) softDeletable.DeletedBy = userId;
                }
            }
        }
    }
}
