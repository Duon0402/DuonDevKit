using System.Data;
using System.Data.Common;
using Dapper;
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DuonDevKit.Dapper
{
    /// <summary>Default <see cref="IDapperQueries"/> implementation, sharing <paramref name="context"/>'s connection/transaction.</summary>
    public sealed class DapperQueries(DbContext context) : IDapperQueries
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<T>>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
        {
            try
            {
                var results = await RunAsync((connection, command) => connection.QueryAsync<T>(command), sql, parameters, ct);
                return Result.Success<IReadOnlyList<T>>(results.AsList());
            }
            catch (DbException ex)
            {
                return Result.Fail<IReadOnlyList<T>>(Error.Unexpected(ErrorCodes.QueryFailed, ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<Result<Option<T>>> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
        {
            try
            {
                var result = await RunAsync((connection, command) => connection.QueryFirstOrDefaultAsync<T>(command), sql, parameters, ct);
                return Result.Success(result is null ? Option<T>.None : Option<T>.Some(result));
            }
            catch (DbException ex)
            {
                return Result.Fail<Option<T>>(Error.Unexpected(ErrorCodes.QueryFailed, ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<Result<int>> ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default)
        {
            try
            {
                var rowsAffected = await RunAsync((connection, command) => connection.ExecuteAsync(command), sql, parameters, ct);
                return Result.Success(rowsAffected);
            }
            catch (DbException ex)
            {
                return Result.Fail<int>(Error.Unexpected(ErrorCodes.QueryFailed, ex.Message));
            }
        }

        /// <summary>
        /// Opens the context's connection for the duration of <paramref name="operation"/> (a no-op if
        /// already open, e.g. inside an active transaction — <c>Database.Open/CloseConnectionAsync</c> are
        /// ref-counted) and passes along its current transaction, if any, so the Dapper call participates in
        /// it instead of running against a separate, unrelated connection.
        /// </summary>
        private async Task<TResult> RunAsync<TResult>(
            Func<IDbConnection, CommandDefinition, Task<TResult>> operation,
            string sql,
            object? parameters,
            CancellationToken ct)
        {
            await context.Database.OpenConnectionAsync(ct);
            try
            {
                var connection = context.Database.GetDbConnection();
                var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: ct);

                return await operation(connection, command);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
