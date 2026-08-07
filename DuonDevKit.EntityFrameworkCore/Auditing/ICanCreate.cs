namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Marks an entity as tracking who created it and when. When left at their default values,
    /// <see cref="CreatedAt"/>/<see cref="CreatedBy"/> are auto-filled by
    /// <see cref="AuditSaveChangesInterceptor"/> on the entity's first save.
    /// </summary>
    public interface ICanCreate
    {
        /// <summary>The UTC timestamp the entity was created. Auto-filled if left at <c>default</c>.</summary>
        DateTime CreatedAt { get; set; }

        /// <summary>The acting user's id at creation time, from <see cref="ICurrentUserProvider"/>. Auto-filled if left <c>null</c>.</summary>
        string? CreatedBy { get; set; }
    }
}
