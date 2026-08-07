using DuonDevKit.Core.Results;

namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Wraps <c>DbContext.SaveChangesAsync</c> so persistence failures surface as <see cref="Result"/> instead of thrown exceptions.</summary>
    public interface IUnitOfWork
    {
        /// <summary>Persists all pending changes. Fails with <c>Error.Conflict</c> on a concurrency conflict, or <c>Error.Unexpected</c> on any other <c>DbUpdateException</c>.</summary>
        Task<Result> SaveChangesAsync(CancellationToken ct = default);
    }
}
