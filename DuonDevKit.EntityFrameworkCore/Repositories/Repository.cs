using DuonDevKit.Core.Errors;
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
        public async Task<Result<IReadOnlyList<T>>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
        {
            IQueryable<T> query = _context.Set<T>();
            if (filter is not null)
                query = query.Where(filter);

            var entities = await query.ToListAsync(ct);
            return Result.Success<IReadOnlyList<T>>(entities);
        }

        /// <inheritdoc />
        public async Task<Result<T>> AddAsync(T entity, CancellationToken ct = default)
        {
            await _context.Set<T>().AddAsync(entity, ct);
            return Result.Success(entity);
        }

        /// <inheritdoc />
        public Result Remove(T entity)
        {
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
    }
}
