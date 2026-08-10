namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>
    /// Generates a primary key for an entity being inserted through <see cref="Repositories.Repository{T, TId}"/>,
    /// used when the entity's <see cref="BaseEntity{TId}.Id"/> is still at its default value — opt-in for
    /// apps that generate ids client-side (e.g. a GUID/ULID string) instead of relying on the database to
    /// assign one. Register an implementation in DI (e.g. <c>services.AddScoped&lt;IEntityIdGenerator&lt;string&gt;, GuidStringIdGenerator&gt;()</c>);
    /// leave it unregistered for entities whose id is database-generated.
    /// </summary>
    /// <remarks>
    /// "Still at its default value" means <c>EqualityComparer&lt;TId&gt;.Default.Equals(entity.Id, default!)</c>
    /// — fine for a reference type or <see cref="string"/> (nothing uses <c>null</c>/<c>""</c> as a real id),
    /// but for a value-type <typeparamref name="TId"/> (e.g. <see cref="int"/>, <see cref="Guid"/>) this
    /// can't distinguish "caller hasn't set an id yet" from "caller explicitly chose the type's zero value"
    /// (<c>0</c>, <see cref="Guid.Empty"/>) as a real business key — that value would be silently
    /// overwritten. Only register a generator for a value-type <typeparamref name="TId"/> if <c>default</c>
    /// is never a legitimate id in your domain.
    /// </remarks>
    public interface IEntityIdGenerator<TId>
    {
        /// <summary>Returns a new id to assign to an entity being inserted.</summary>
        TId NewId();
    }
}
