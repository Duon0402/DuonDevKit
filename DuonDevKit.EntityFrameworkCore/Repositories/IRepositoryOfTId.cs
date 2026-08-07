using DuonDevKit.Core.Results;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>
    /// <see cref="IRepository{T}"/> convenience overload for entities inheriting <see cref="BaseEntity{TId}"/>,
    /// adding a <see cref="GetByIdAsync(TId, CancellationToken)"/> that takes the key directly instead of
    /// an untyped <c>object[]</c>.
    /// </summary>
    public interface IRepository<T, TId> : IRepository<T> where T : BaseEntity<TId>
    {
        /// <summary>Finds an entity by its typed key. Fails with <c>Error.NotFound</c> if no matching entity exists.</summary>
        Task<Result<T>> GetByIdAsync(TId id, CancellationToken ct = default);
    }
}
