namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Marks an entity as soft-deletable. Repositories built on this library set
    /// <see cref="IsDeleted"/> instead of physically removing the row, and a global query filter
    /// (see <c>ModelBuilderExtensions.ApplySoftDeleteQueryFilter</c>) excludes deleted rows by
    /// default. <see cref="DeletedAt"/>/<see cref="DeletedBy"/> are auto-filled by
    /// <see cref="AuditSaveChangesInterceptor"/> the moment <see cref="IsDeleted"/> transitions to
    /// <c>true</c>.
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>Whether this entity is (soft-)deleted. Excluded from default queries when <c>true</c>.</summary>
        bool IsDeleted { get; set; }

        /// <summary>The UTC timestamp <see cref="IsDeleted"/> was set to <c>true</c>. Auto-filled.</summary>
        DateTime? DeletedAt { get; set; }

        /// <summary>The acting user's id when <see cref="IsDeleted"/> was set to <c>true</c>, from <see cref="ICurrentUserProvider"/>. Auto-filled.</summary>
        string? DeletedBy { get; set; }
    }
}
