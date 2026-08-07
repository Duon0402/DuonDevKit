using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>Default <see cref="IRepository{T, TId}"/> implementation, extending <see cref="Repository{T}"/> with a typed-key lookup.</summary>
    public class Repository<T, TId>(DbContext context) : Repository<T>(context), IRepository<T, TId> where T : BaseEntity<TId>
    {
        /// <inheritdoc />
        public async Task<Result<T>> GetByIdAsync(TId id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync([id!], ct);
            return entity is null
                ? Result.Fail<T>(Error.NotFound(ErrorCodes.EntityNotFound, $"{typeof(T).Name} not found."))
                : Result.Success(entity);
        }
    }
}
