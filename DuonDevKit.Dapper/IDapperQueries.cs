using DuonDevKit.Core.Options;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Dapper
{
    /// <summary>
    /// Runs raw SQL through the same <c>DbContext</c> connection (and its current transaction, if any) as
    /// EF Core, for queries a LINQ-based repository can't express cleanly — wrapped in
    /// <see cref="Result"/>/<see cref="Result{T}"/> so a <c>DbException</c> surfaces the same way an
    /// EF Core failure does, instead of throwing.
    /// </summary>
    public interface IDapperQueries
    {
        /// <summary>Runs <paramref name="sql"/> and maps every row to <typeparamref name="T"/>.</summary>
        Task<Result<IReadOnlyList<T>>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default);

        /// <summary>Runs <paramref name="sql"/> and maps the first row to <typeparamref name="T"/>, or <see cref="Option{T}.None"/> if it returns none — "no rows" isn't a failure here, just an absent value.</summary>
        Task<Result<Option<T>>> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken ct = default);

        /// <summary>Runs <paramref name="sql"/> as a non-query command (e.g. <c>UPDATE</c>/<c>DELETE</c>) and returns the number of rows affected.</summary>
        Task<Result<int>> ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default);
    }
}
