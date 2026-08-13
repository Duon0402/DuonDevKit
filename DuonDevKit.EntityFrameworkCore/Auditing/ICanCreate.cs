namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Marks an entity as tracking who created it and when. <see cref="AuditSaveChangesInterceptor"/>
    /// auto-fills <see cref="CreatedAt"/>/<see cref="CreatedBy"/> on creation, and protects them from
    /// being overwritten on any later update (even a disconnected <c>DbSet.Update()</c>).
    /// </summary>
    public interface ICanCreate
    {
        /// <summary>The UTC timestamp the entity was created. Auto-filled if left at <c>default</c>.</summary>
        DateTime CreatedAt { get; set; }

        /// <summary>The acting user's id at creation time, from <see cref="ICurrentUserProvider"/>. Auto-filled if left <c>null</c>.</summary>
        string? CreatedBy { get; set; }
    }
}
