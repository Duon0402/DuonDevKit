namespace DuonDevKit.EntityFrameworkCore
{
    /// <summary>Error codes used by <see cref="Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>An entity was not found by <see cref="Repositories.Repository{T}.GetByIdAsync"/> or <see cref="Repositories.Repository{T, TId}.GetByIdAsync(TId, CancellationToken)"/>.</summary>
        public const string EntityNotFound = "ENTITY001";

        /// <summary>A <c>DbUpdateConcurrencyException</c> occurred while saving via <see cref="UnitOfWork"/>.</summary>
        public const string ConcurrencyConflict = "DB001";

        /// <summary>An unexpected <c>DbUpdateException</c> occurred while saving via <see cref="UnitOfWork"/>.</summary>
        public const string UnexpectedDbError = "DB002";
    }
}
