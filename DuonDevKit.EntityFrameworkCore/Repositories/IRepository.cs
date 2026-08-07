using System.Linq.Expressions;
using DuonDevKit.Core.Results;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>Result-based data access over a single entity type, backed by an EF Core <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.</summary>
    public interface IRepository<T> where T : class
    {
        /// <summary>Finds an entity by its key. Fails with <c>Error.NotFound</c> if no matching entity exists.</summary>
        Task<Result<T>> GetByIdAsync(object[] keyValues, CancellationToken ct = default);

        /// <summary>Lists entities matching <paramref name="filter"/>, or all entities when <paramref name="filter"/> is <c>null</c>.</summary>
        Task<Result<IReadOnlyList<T>>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);

        /// <summary>Begins tracking <paramref name="entity"/> as a new row. Not yet persisted until <c>IUnitOfWork.SaveChangesAsync</c> is called.</summary>
        Task<Result<T>> AddAsync(T entity, CancellationToken ct = default);

        /// <summary>Begins tracking every entity in <paramref name="entities"/> as a new row. Not yet persisted until <c>IUnitOfWork.SaveChangesAsync</c> is called.</summary>
        Task<Result<IReadOnlyList<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

        /// <summary>
        /// Attaches <paramref name="entity"/> if not already tracked and marks every scalar property as
        /// modified, so the whole entity is persisted on the next <c>IUnitOfWork.SaveChangesAsync</c> call.
        /// Use for disconnected/detached entities (e.g. a full entity received from a client) where you
        /// don't want to fetch-then-mutate first.
        /// </summary>
        Result Update(T entity);

        /// <summary>Bulk version of <see cref="Update"/>.</summary>
        Result UpdateRange(IEnumerable<T> entities);

        /// <summary>
        /// Removes <paramref name="entity"/> — soft-deletes (sets <c>IsDeleted = true</c>) if it implements
        /// <see cref="Auditing.ISoftDelete"/>, otherwise hard-deletes. <paramref name="entity"/> is attached
        /// if not already tracked by the underlying context (e.g. fetched via <see cref="GetByIdAsync"/>).
        /// Not yet persisted until <c>IUnitOfWork.SaveChangesAsync</c> is called.
        /// </summary>
        Result Remove(T entity);

        /// <summary>Bulk version of <see cref="Remove"/>.</summary>
        Result RemoveRange(IEnumerable<T> entities);
    }
}
