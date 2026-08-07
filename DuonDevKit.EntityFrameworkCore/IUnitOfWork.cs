using DuonDevKit.Core.Results;

namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Wraps <c>DbContext.SaveChangesAsync</c> and transaction management so persistence failures surface as <see cref="Result"/> instead of thrown exceptions.</summary>
    public interface IUnitOfWork
    {
        /// <summary>Persists all pending changes. Fails with <c>Error.Conflict</c> on a concurrency conflict, or <c>Error.Unexpected</c> on any other <c>DbUpdateException</c>.</summary>
        Task<Result> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>Returns <c>true</c> if the underlying context is tracking any pending additions, changes, or removals.</summary>
        bool HasChanges();

        /// <summary>
        /// Starts a database transaction. Fails if a transaction is already active. Prefer
        /// <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task{Result}}, CancellationToken)"/>
        /// when the provider uses a retrying execution strategy (e.g. <c>EnableRetryOnFailure</c>) — manual
        /// Begin/Commit/Rollback bypasses strategy-level retries.
        /// </summary>
        Task<Result> BeginTransactionAsync(CancellationToken ct = default);

        /// <summary>Commits the transaction started by <see cref="BeginTransactionAsync"/>. Fails if there is no active transaction.</summary>
        Task<Result> CommitTransactionAsync(CancellationToken ct = default);

        /// <summary>Rolls back the transaction started by <see cref="BeginTransactionAsync"/>. Fails if there is no active transaction.</summary>
        Task<Result> RollbackTransactionAsync(CancellationToken ct = default);

        /// <summary>
        /// Runs <paramref name="operation"/> and <see cref="SaveChangesAsync"/> inside a single database
        /// transaction, wrapped in the provider's execution strategy so retrying providers (e.g.
        /// <c>EnableRetryOnFailure</c>) retry the whole unit safely. Commits only if both
        /// <paramref name="operation"/> and the save succeed; otherwise the transaction is rolled back and
        /// the failure's <c>Error</c> is returned.
        /// </summary>
        Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken ct = default);

        /// <summary>Value-returning overload of <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task{Result}}, CancellationToken)"/>.</summary>
        Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct = default);
    }
}
