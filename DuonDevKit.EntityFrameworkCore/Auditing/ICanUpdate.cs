namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Marks an entity as tracking who last modified it and when. Unlike <see cref="ICanCreate"/>,
    /// <see cref="UpdatedAt"/>/<see cref="UpdatedBy"/> are refreshed by
    /// <see cref="AuditSaveChangesInterceptor"/> on every save, not just when unset.
    /// </summary>
    public interface ICanUpdate
    {
        /// <summary>The UTC timestamp of the most recent save. Always refreshed on every update.</summary>
        DateTime? UpdatedAt { get; set; }

        /// <summary>The acting user's id at the most recent save, from <see cref="ICurrentUserProvider"/>. Always refreshed on every update.</summary>
        string? UpdatedBy { get; set; }
    }
}
