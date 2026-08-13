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
        public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, TOut> mapper)
        {
            var result = await resultTask;
            return result.Map(mapper);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then transforms the value of a successful result using the async <paramref name="mapper"/>; propagates the error unchanged otherwise, without invoking <paramref name="mapper"/>.</summary>
        public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<TOut>> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            var result = await resultTask;
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Error);

            var mappedValue = await mapper(result.Value);
            return Result.Success(mappedValue);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then chains another result-returning operation using <paramref name="binder"/> when successful; propagates the error unchanged otherwise.</summary>
        public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Result<TOut>> binder)
        {
            var result = await resultTask;
            return result.Bind(binder);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then chains another async result-returning operation using <paramref name="binder"/> when successful; propagates the error unchanged otherwise, without invoking <paramref name="binder"/> or double-wrapping its result.</summary>
        public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<Result<TOut>>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);

            var result = await resultTask;
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Error);

            return await binder(result.Value);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then applies <see cref="Result{T}.Ensure"/> using <paramref name="predicate"/>; propagates the error unchanged on an already-failed result, without invoking <paramref name="predicate"/>.</summary>
        public static async Task<Result<T>> EnsureAsync<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, Error error)
        {
            var result = await resultTask;
            return result.Ensure(predicate, error);
        }

        /// <summary>Awaits <paramref name="resultTask"/>, then converts a successful result into a failure carrying <paramref name="error"/> when the async <paramref name="predicate"/> returns <c>false</c>; propagates the error unchanged on an already-failed result, without invoking <paramref name="predicate"/>.</summary>
        public static async Task<Result<T>> EnsureAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<bool>> predicate, Error error)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(error);

            var result = await resultTask;
            if (result.IsFailure) return result;

            return await predicate(result.Value) ? result : Result.Fail<T>(error);
        }
    }
}
