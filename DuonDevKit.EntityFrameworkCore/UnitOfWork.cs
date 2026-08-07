using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Default <see cref="IUnitOfWork"/> implementation backed by a single <see cref="DbContext"/>.</summary>
    public class UnitOfWork(DbContext context) : IUnitOfWork
    {
        private readonly DbContext _context = context;

        /// <inheritdoc />
        public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Error.Conflict(ErrorCodes.ConcurrencyConflict, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return Error.Unexpected(ErrorCodes.UnexpectedDbError, ex.Message);
            }
        }
    }
}
