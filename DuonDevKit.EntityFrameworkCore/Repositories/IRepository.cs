using System.Linq.Expressions;
using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>Result-based data access over a single entity type, backed by an EF Core <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.</summary>
    public interface IRepository<T> where T : class
    {
        /// <summary>Finds an entity by its key. Fails with <c>Error.NotFound</c> if no matching entity exists.</summary>
        Task<Result<T>> GetByIdAsync(object[] keyValues, CancellationToken ct = default);

        /// <summary>
        /// Escape hatch returning the raw <see cref="IQueryable{T}"/> for this entity type (still subject to
        /// the soft-delete query filter, if any), for queries <see cref="ListAsync"/>/<see cref="ListPagedAsync"/>
        /// can't express — joins, projections, <c>GroupBy</c>, eager loading via <c>.Include()</c>, etc. Not
        /// wrapped in <see cref="Result"/>; the caller composes and executes the query itself.
        /// </summary>
        IQueryable<T> Query(bool asNoTracking = false);

        /// <summary>
        /// Finds the first entity matching <paramref name="predicate"/>, or <see cref="Option{T}.None"/> if
        /// none does — unlike <see cref="GetByIdAsync"/>, "not found" isn't a failure here, just an absent
        /// value. Pass <paramref name="include"/> (e.g. <c>q =&gt; q.Include(x =&gt; x.Customer)</c>) to eager-load
        /// navigation properties.
        /// </summary>
        Task<Option<T>> FindOneAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lists entities matching <paramref name="filter"/>, or all entities when <paramref name="filter"/>
        /// is <c>null</c>. Pass <paramref name="include"/> (e.g. <c>q =&gt; q.Include(x =&gt; x.Customer)</c>) to
        /// eager-load navigation properties.
        /// </summary>
        Task<Result<IReadOnlyList<T>>> ListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lists a single page of entities matching <paramref name="filter"/> (1-based
        /// <paramref name="pageNumber"/>), alongside the total count across every page. Fails with
        /// <c>Error.Validation</c> if <paramref name="pageNumber"/> or <paramref name="pageSize"/> is not
        /// positive. Pass <paramref name="orderBy"/> (e.g. <c>q =&gt; q.OrderBy(x =&gt; x.CreatedAt)</c>) for a
        /// stable page order — without it, page contents are not guaranteed to be consistent across calls.
        /// Pass <paramref name="include"/> to eager-load navigation properties.
        /// </summary>
        Task<Result<PagedResult<T>>> ListPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

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
