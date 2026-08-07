using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Default <see cref="IUnitOfWork"/> implementation backed by a single <see cref="DbContext"/>.</summary>
    public class UnitOfWork(DbContext context) : IUnitOfWork, IAsyncDisposable
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
            catch (DbUpdateConcurrencyException ex)
            {
                return Error.Conflict(ErrorCodes.ConcurrencyConflict, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return Error.Unexpected(ErrorCodes.UnexpectedDbError, ex.Message);
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
        public Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                var result = await operation(ct);
                if (result.IsFailure)
                    return result;

                var saveResult = await SaveChangesAsync(ct);
                if (saveResult.IsFailure)
                    return saveResult;

                await transaction.CommitAsync(ct);
                return Result.Success();
            });
        }

        /// <inheritdoc />
        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                var result = await operation(ct);
                if (result.IsFailure)
                    return result;

                var saveResult = await SaveChangesAsync(ct);
                if (saveResult.IsFailure)
                    return Result.Fail<T>(saveResult.Error);

                await transaction.CommitAsync(ct);
                return result;
            });
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
    }
}
