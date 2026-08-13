using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
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
        private static readonly MethodInfo QueryFirstOrDefaultAsNullableDefinition = typeof(DapperQueries)
            .GetMethod(nameof(QueryFirstOrDefaultAsNullableAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly ConcurrentDictionary<Type, MethodInfo> QueryFirstOrDefaultAsNullableCache = new();

        /// <inheritdoc />
        public Task<Result<IReadOnlyList<T>>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
            => TryRunAsync(async () =>
            {
                var results = await RunAsync((connection, command) => connection.QueryAsync<T>(command), sql, parameters, ct);
                return (IReadOnlyList<T>)results.AsList();
            });

        /// <inheritdoc />
        public Task<Result<Option<T>>> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
        {
            // Non-nullable value types need the Nullable<T> detour below to tell "no rows" apart from a row whose value is default(T).
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null)
            {
                var method = QueryFirstOrDefaultAsNullableCache.GetOrAdd(typeof(T), t => QueryFirstOrDefaultAsNullableDefinition.MakeGenericMethod(t));
                return (Task<Result<Option<T>>>)method.Invoke(this, [sql, parameters, ct])!;
            }

            return QueryFirstOrDefaultReferenceOrNullableAsync<T>(sql, parameters, ct);
        }

        private Task<Result<Option<T>>> QueryFirstOrDefaultReferenceOrNullableAsync<T>(string sql, object? parameters, CancellationToken ct)
            => TryRunAsync(async () =>
            {
                var result = await RunAsync((connection, command) => connection.QueryFirstOrDefaultAsync<T>(command), sql, parameters, ct);
                return result is null ? Option<T>.None : Option<T>.Some(result);
            });

        private Task<Result<Option<TStruct>>> QueryFirstOrDefaultAsNullableAsync<TStruct>(string sql, object? parameters, CancellationToken ct)
            where TStruct : struct
            => TryRunAsync(async () =>
            {
                var result = await RunAsync((connection, command) => connection.QueryFirstOrDefaultAsync<TStruct?>(command), sql, parameters, ct);
                return result.HasValue ? Option<TStruct>.Some(result.Value) : Option<TStruct>.None;
            });

        /// <inheritdoc />
        public Task<Result<int>> ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default)
            => TryRunAsync(() => RunAsync((connection, command) => connection.ExecuteAsync(command), sql, parameters, ct));

        /// <summary>Runs <paramref name="operation"/>, converting a thrown <see cref="DbException"/> into a failed <see cref="Result{T}"/> instead of letting it propagate.</summary>
        private static async Task<Result<TResult>> TryRunAsync<TResult>(Func<Task<TResult>> operation)
        {
            try
            {
                return Result.Success(await operation());
            }
            catch (DbException ex)
            {
                return Result.Fail<TResult>(Error.Unexpected(ErrorCodes.QueryFailed, ex.Message));
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
