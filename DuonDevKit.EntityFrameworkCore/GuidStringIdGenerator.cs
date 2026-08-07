namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Ready-made <see cref="IEntityIdGenerator{TId}"/> for <see cref="BaseEntity"/> (<c>string</c> id), generating a new GUID per entity.</summary>
    public sealed class GuidStringIdGenerator : IEntityIdGenerator<string>
    {
        /// <inheritdoc />
        public string NewId() => Guid.NewGuid().ToString();
    }
}
