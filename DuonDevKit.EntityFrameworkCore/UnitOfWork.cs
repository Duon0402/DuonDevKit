using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Default <see cref="IUnitOfWork"/> implementation backed by a single <see cref="DbContext"/>.</summary>
    public class UnitOfWork(DbContext context) : IUnitOfWork, IAsyncDisposable, IDisposable
    {
        private readonly DbContext _context = context;
        private IDbContextTransaction? _currentTransaction;

        /// <inheritdoc />
        public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Unlike the DbUpdateException branch below, this is ErrorType.Conflict — a type the
                // AspNetCore mapping treats as caller-authored and passes straight through to the HTTP
                // response (only Unexpected gets a generic-message substitution there). So this message
                // must itself stay safe to show a client; it must never be the raw ex.Message, which is an
                // EF-authored string that could change wording/content across EF versions.
                await RollbackActiveTransactionOnFailureAsync(ct);
                return Error.Conflict(ErrorCodes.ConcurrencyConflict, "The record was modified or deleted by another operation since it was loaded. Reload and try again.");
            }
            catch (DbUpdateException ex)
            {
                await RollbackActiveTransactionOnFailureAsync(ct);
                return Error.Unexpected(ErrorCodes.UnexpectedDbError, ex.Message);
            }
        }

        /// <summary>
        /// Automatically rolls back and clears a manually-started transaction (<see cref="BeginTransactionAsync"/>)
        /// when <see cref="SaveChangesAsync"/> fails while one is active — without this, a failed save left the
        /// transaction open until the caller explicitly rolled it back or this <see cref="UnitOfWork"/> was
        /// disposed, silently keeping a half-applied unit of work's lock/transaction alive in the meantime.
        /// A no-op if no manual transaction is active (including inside <see cref="ExecuteInTransactionAsync"/>,
        /// which owns and disposes its own local transaction independently of this field).
        /// </summary>
        private async Task RollbackActiveTransactionOnFailureAsync(CancellationToken ct)
        {
            if (_currentTransaction is null) return;

            try
            {
                await _currentTransaction.RollbackAsync(ct);
            }
            finally
            {
                await ClearCurrentTransactionAsync();
            }
        }

        /// <inheritdoc />
        public bool HasChanges() => _context.ChangeTracker.HasChanges();

        /// <inheritdoc />
        public async Task<Result> BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction is not null)
                return Error.Business(ErrorCodes.TransactionAlreadyActive, "A transaction is already active on this unit of work.");

            try
            {
                _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Unexpected(ErrorCodes.TransactionError, ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<Result> CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction is null)
                return Error.Business(ErrorCodes.NoActiveTransaction, "No active transaction to commit.");

            try
            {
                await _currentTransaction.CommitAsync(ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Unexpected(ErrorCodes.TransactionError, ex.Message);
            }
            finally
            {
                await ClearCurrentTransactionAsync();
            }
        }

        /// <inheritdoc />
        public async Task<Result> RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction is null)
                return Error.Business(ErrorCodes.NoActiveTransaction, "No active transaction to roll back.");

            try
            {
                await _currentTransaction.RollbackAsync(ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Unexpected(ErrorCodes.TransactionError, ex.Message);
            }
            finally
            {
                await ClearCurrentTransactionAsync();
            }
        }

        /// <inheritdoc />
        public async Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken ct = default)
        {
            var result = await ExecuteInTransactionAsync(async innerCt =>
            {
                var opResult = await operation(innerCt);
                return opResult.IsFailure ? Result.Fail<Unit>(opResult.Error) : Result.Success(Unit.Value);
            }, ct);

            return result.IsFailure ? Result.Fail(result.Error) : Result.Success();
        }

        /// <inheritdoc />
        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct = default)
        {
            if (_currentTransaction is not null)
                return Task.FromResult(Result.Fail<T>(Error.Business(ErrorCodes.TransactionAlreadyActive, "A transaction is already active on this unit of work.")));

            var strategy = _context.Database.CreateExecutionStrategy();

            // ct is also passed to ExecuteAsync itself (not just the operations below), so cancellation is
            // observed even while a retrying strategy (e.g. EnableRetryOnFailure) is sleeping between
            // attempts, not just once the next attempt starts and reaches a ct-aware call.
            return strategy.ExecuteAsync(async _ =>
            {
                IDbContextTransaction transaction;
                try
                {
                    transaction = await _context.Database.BeginTransactionAsync(ct);
                }
                catch (Exception ex)
                {
                    return Result.Fail<T>(Error.Unexpected(ErrorCodes.TransactionError, ex.Message));
                }

                await using (transaction)
                {
                    var result = await operation(ct);
                    if (result.IsFailure)
                        return result;

                    var saveResult = await SaveChangesAsync(ct);
                    if (saveResult.IsFailure)
                        return Result.Fail<T>(saveResult.Error);

                    try
                    {
                        await transaction.CommitAsync(ct);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        return Result.Fail<T>(Error.Unexpected(ErrorCodes.TransactionError, ex.Message));
                    }
                }
            }, ct);
        }

        /// <summary>Stand-in for <c>void</c> so <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task{Result}}, CancellationToken)"/> can delegate to the <see cref="Result{T}"/>-returning overload instead of duplicating its retry/transaction/save/commit logic.</summary>
        private readonly struct Unit
        {
            public static readonly Unit Value = default;
        }

        private async Task ClearCurrentTransactionAsync()
        {
            if (_currentTransaction is null) return;

            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        /// <summary>Disposes any transaction left active by <see cref="BeginTransactionAsync"/>. Does not dispose the underlying <see cref="DbContext"/>, which this class does not own.</summary>
        public async ValueTask DisposeAsync()
            => await ClearCurrentTransactionAsync();

        /// <summary>
        /// Synchronous fallback for hosts that dispose their DI scope with <c>using</c> instead of
        /// <c>await using</c> (e.g. a console app or worker service) — without this, disposing such a
        /// scope while a transaction is active throws, because the default <c>ServiceProvider</c> disposal
        /// path only calls <see cref="IDisposable.Dispose"/>, never <see cref="IAsyncDisposable.DisposeAsync"/>,
        /// on a type that implements both. Prefer <c>await using</c> where possible; this exists so a
        /// synchronous scope doesn't throw, not to make blocking on transaction cleanup idiomatic.
        /// </summary>
        public void Dispose()
        {
            if (_currentTransaction is null) return;

            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }
}
