using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Repositories
{
    /// <summary>Default <see cref="IRepository{T, TId}"/> implementation, extending <see cref="Repository{T}"/> with a typed-key lookup and optional id generation.</summary>
    public class Repository<T, TId>(DbContext context, IEntityIdGenerator<TId>? idGenerator = null)
        : Repository<T>(context), IRepository<T, TId> where T : BaseEntity<TId>
    {
        /// <inheritdoc />
        public async Task<Result<T>> GetByIdAsync(TId id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync([id!], ct);
            return ToFoundResult(entity);
        }

        /// <inheritdoc />
        public override Task<Result<T>> AddAsync(T entity, CancellationToken ct = default)
        {
            AssignIdIfMissing(entity);
            return base.AddAsync(entity, ct);
        }

        /// <inheritdoc />
        public override Task<Result<IReadOnlyList<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            var list = entities as IReadOnlyList<T> ?? entities.ToList();

            foreach (var entity in list)
                AssignIdIfMissing(entity);

            return base.AddRangeAsync(list, ct);
        }

        private void AssignIdIfMissing(T entity)
        {
            if (idGenerator is not null && EqualityComparer<TId>.Default.Equals(entity.Id, default!))
                entity.Id = idGenerator.NewId();
        }
    }
}
