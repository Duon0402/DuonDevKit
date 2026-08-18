using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Extensions
{
    /// <summary>
    /// Extension methods that let <see cref="Result{T}"/> chaining continue across async steps
    /// (e.g. database or HTTP calls) without manually awaiting between each step.
    /// </summary>
    public static class ResultAsyncExtensions
    {
        /// <summary>Awaits <paramref name="resultTask"/>, then transforms the value of a successful result using <paramref name="mapper"/>; propagates the error unchanged otherwise.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="mapper"/> runs; does not cancel <paramref name="resultTask"/> itself.</param>
        public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, TOut> mapper, CancellationToken ct = default)
        {
            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            return result.Map(mapper);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then transforms the value of a successful result using the async <paramref name="mapper"/>; propagates the error unchanged otherwise, without invoking <paramref name="mapper"/>.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="mapper"/> runs; does not cancel <paramref name="resultTask"/> itself. The token is not passed into <paramref name="mapper"/> — capture it in the delegate if <paramref name="mapper"/> needs to observe it.</param>
        public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<TOut>> mapper, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Error);

            var mappedValue = await mapper(result.Value);
            return Result.Success(mappedValue);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then chains another result-returning operation using <paramref name="binder"/> when successful; propagates the error unchanged otherwise.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="binder"/> runs; does not cancel <paramref name="resultTask"/> itself.</param>
        public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Result<TOut>> binder, CancellationToken ct = default)
        {
            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            return result.Bind(binder);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then chains another async result-returning operation using <paramref name="binder"/> when successful; propagates the error unchanged otherwise, without invoking <paramref name="binder"/> or double-wrapping its result.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="binder"/> runs; does not cancel <paramref name="resultTask"/> itself. The token is not passed into <paramref name="binder"/> — capture it in the delegate if <paramref name="binder"/> needs to observe it.</param>
        public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<Result<TOut>>> binder, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(binder);

            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Error);

            return await binder(result.Value);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then applies <see cref="Result{T}.Ensure"/> using <paramref name="predicate"/>; propagates the error unchanged on an already-failed result, without invoking <paramref name="predicate"/>.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="predicate"/> runs; does not cancel <paramref name="resultTask"/> itself.</param>
        public static async Task<Result<T>> EnsureAsync<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, Error error, CancellationToken ct = default)
        {
            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            return result.Ensure(predicate, error);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then converts a successful result into a failure carrying <paramref name="error"/> when the async <paramref name="predicate"/> returns <c>false</c>; propagates the error unchanged on an already-failed result, without invoking <paramref name="predicate"/>.</summary>
        /// <param name="ct">Checked after <paramref name="resultTask"/> completes and before <paramref name="predicate"/> runs; does not cancel <paramref name="resultTask"/> itself. The token is not passed into <paramref name="predicate"/> — capture it in the delegate if <paramref name="predicate"/> needs to observe it.</param>
        public static async Task<Result<T>> EnsureAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<bool>> predicate, Error error, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(error);

            var result = await resultTask;
            ct.ThrowIfCancellationRequested();
            if (result.IsFailure) return result;

            return await predicate(result.Value) ? result : Result.Fail<T>(error);
        }
    }
}
