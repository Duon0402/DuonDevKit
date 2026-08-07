namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>
    /// Optional convenience base class providing just an <see cref="Id"/> of type <typeparamref name="TId"/>.
    /// Deliberately carries no audit behavior — an entity must still implement
    /// <c>ICanCreate</c>/<c>ICanUpdate</c>/<c>ISoftDelete</c> explicitly to get auditing, independent
    /// of whether it also inherits from this class.
    /// </summary>
    public abstract class BaseEntity<TId>
    {
        /// <summary>The entity's primary key.</summary>
        public TId Id { get; set; } = default!;
    }

    /// <summary>Convenience non-generic <see cref="BaseEntity{TId}"/> for the common case of a <see cref="string"/> id.</summary>
    public abstract class BaseEntity : BaseEntity<string>
    {
    }
}
