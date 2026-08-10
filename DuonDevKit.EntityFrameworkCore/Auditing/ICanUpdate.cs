namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Marks an entity as tracking who last modified it and when. <see cref="AuditSaveChangesInterceptor"/>
    /// fills <see cref="UpdatedAt"/>/<see cref="UpdatedBy"/> on every save where the caller didn't already
    /// set them explicitly (checked via <c>PropertyEntry.OriginalValue</c>/<c>CurrentValue</c>) — an
    /// explicit value you set yourself is respected, not overwritten, for a <em>tracked</em> entity
    /// mutated in place. For a <em>disconnected</em> entity attached via <c>DbSet.Update()</c>, EF Core
    /// has no real "original" value to compare against and reports every property as unchanged, so this
    /// distinction can't be made there — the interceptor always fills both in that path.
    /// </summary>
    public interface ICanUpdate
    {
        /// <summary>The UTC timestamp of the most recent save.</summary>
        DateTime? UpdatedAt { get; set; }

        /// <summary>The acting user's id at the most recent save, from <see cref="ICurrentUserProvider"/>.</summary>
        string? UpdatedBy { get; set; }
    }
}
