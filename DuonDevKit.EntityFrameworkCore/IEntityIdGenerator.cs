namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>
    /// Generates a primary key for an entity being inserted through <see cref="Repositories.Repository{T, TId}"/>,
    /// used when the entity's <see cref="BaseEntity{TId}.Id"/> is still at its default value — opt-in for
    /// apps that generate ids client-side (e.g. a GUID/ULID string) instead of relying on the database to
    /// assign one. Register an implementation in DI (e.g. <c>services.AddScoped&lt;IEntityIdGenerator&lt;string&gt;, GuidStringIdGenerator&gt;()</c>);
    /// leave it unregistered for entities whose id is database-generated.
    /// </summary>
    public interface IEntityIdGenerator<TId>
    {
        /// <summary>Returns a new id to assign to an entity being inserted.</summary>
        TId NewId();
    }
}
