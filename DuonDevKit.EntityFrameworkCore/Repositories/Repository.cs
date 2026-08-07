using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Guards;
using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;
using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>Default <see cref="IRepository{T}"/> implementation backed by a single <see cref="DbContext"/>.</summary>
    /// <remarks>Creates a repository backed by <paramref name="context"/>.</remarks>
    public class Repository<T>(DbContext context) : IRepository<T> where T : class
    {
        /// <summary>The underlying <see cref="DbContext"/>. Exposed as <c>protected</c> so <see cref="Repository{T, TId}"/> can reuse it.</summary>
        protected readonly DbContext _context = context;

        /// <inheritdoc />
        public async Task<Result<T>> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(keyValues, ct);
            return entity is null
                ? Result.Fail<T>(Error.NotFound(ErrorCodes.EntityNotFound, $"{typeof(T).Name} not found."))
                : Result.Success(entity);
        }

        /// <inheritdoc />
        public IQueryable<T> Query(bool asNoTracking = false)
            => asNoTracking ? _context.Set<T>().AsNoTracking() : _context.Set<T>();

        /// <inheritdoc />
        public async Task<Option<T>> FindOneAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            IQueryable<T> query = _context.Set<T>().Where(predicate);
            if (include is not null)
                query = include(query);

            var entity = await query.FirstOrDefaultAsync(ct);
            return entity is null ? Option<T>.None : Option<T>.Some(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<T>>> ListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            IQueryable<T> query = _context.Set<T>();
            if (filter is not null)
                query = query.Where(filter);
            if (include is not null)
                query = include(query);

            var entities = await query.ToListAsync(ct);
            return Result.Success<IReadOnlyList<T>>(entities);
        }

        /// <inheritdoc />
        public async Task<Result<PagedResult<T>>> ListPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default)
        {
            var validation = Result.Combine(
                Guard.Against.NegativeOrZero(pageNumber, nameof(pageNumber)),
                Guard.Against.NegativeOrZero(pageSize, nameof(pageSize)));

            if (validation.IsFailure)
                return Result.Fail<PagedResult<T>>(validation.Error);

            IQueryable<T> query = _context.Set<T>();
            if (filter is not null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync(ct);

            if (orderBy is not null)
                query = orderBy(query);
            if (include is not null)
                query = include(query);

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return Result.Success(new PagedResult<T>(items, pageNumber, pageSize, totalCount));
        }

        /// <inheritdoc />
        public virtual async Task<Result<T>> AddAsync(T entity, CancellationToken ct = default)
        {
            await _context.Set<T>().AddAsync(entity, ct);
            return Result.Success(entity);
        }

        /// <inheritdoc />
        public virtual async Task<Result<IReadOnlyList<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            var list = entities as IReadOnlyList<T> ?? entities.ToList();
            await _context.Set<T>().AddRangeAsync(list, ct);
            return Result.Success<IReadOnlyList<T>>(list);
        }

        /// <inheritdoc />
        public Result Update(T entity)
        {
            _context.Set<T>().Update(entity);
            return Result.Success();
        }

        /// <inheritdoc />
        public Result UpdateRange(IEnumerable<T> entities)
        {
            _context.Set<T>().UpdateRange(entities);
            return Result.Success();
        }

        /// <inheritdoc />
        public Result Remove(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
                _context.Attach(entity);

            if (entity is ISoftDelete softDeletable)
            {
                softDeletable.IsDeleted = true;
            }
            else
            {
                _context.Set<T>().Remove(entity);
            }

            return Result.Success();
        }

        /// <inheritdoc />
        public Result RemoveRange(IEnumerable<T> entities)
        {
            var list = entities as IReadOnlyList<T> ?? entities.ToList();

            var detached = list.Where(e => _context.Entry(e).State == EntityState.Detached).ToList();
            if (detached.Count > 0)
                _context.AttachRange(detached);

            var hardDelete = new List<T>(list.Count);
            foreach (var entity in list)
            {
                if (entity is ISoftDelete softDeletable)
                    softDeletable.IsDeleted = true;
                else
                    hardDelete.Add(entity);
            }

            if (hardDelete.Count > 0)
                _context.Set<T>().RemoveRange(hardDelete);

            return Result.Success();
        }
    }
}
