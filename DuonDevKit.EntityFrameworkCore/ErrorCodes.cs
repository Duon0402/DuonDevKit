namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Error codes used by <see cref="Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>An entity was not found by <see cref="Repositories.Repository{T}.GetByIdAsync"/> or <see cref="Repositories.Repository{T, TId}.GetByIdAsync(TId, CancellationToken)"/>.</summary>
        public const string EntityNotFound = "ENTITY001";

        /// <summary>
        /// <see cref="Repositories.Repository{T}.Update"/>/<see cref="Repositories.Repository{T}.Remove"/>
        /// (or their range variants) were passed a detached entity whose key is already tracked by a
        /// <em>different</em> instance in the same <c>DbContext</c> — EF Core can't attach a second
        /// instance for the same row.
        /// </summary>
        public const string EntityAlreadyTracked = "ENTITY002";

        /// <summary><see cref="Repositories.Repository{T}.ListPagedAsync"/> was called with a <c>pageSize</c> above the library's enforced upper bound.</summary>
        public const string PageSizeTooLarge = "ENTITY003";

        /// <summary>A <c>DbUpdateConcurrencyException</c> occurred while saving via <see cref="UnitOfWork"/>.</summary>
        public const string ConcurrencyConflict = "DB001";

        /// <summary>An unexpected <c>DbUpdateException</c> occurred while saving via <see cref="UnitOfWork"/>.</summary>
        public const string UnexpectedDbError = "DB002";

        /// <summary><see cref="UnitOfWork.BeginTransactionAsync"/> was called while a transaction was already active.</summary>
        public const string TransactionAlreadyActive = "DB003";

        /// <summary><see cref="UnitOfWork.CommitTransactionAsync"/> or <see cref="UnitOfWork.RollbackTransactionAsync"/> was called with no active transaction.</summary>
        public const string NoActiveTransaction = "DB004";

        /// <summary>Beginning, committing, or rolling back a transaction failed.</summary>
        public const string TransactionError = "DB005";
    }
}
