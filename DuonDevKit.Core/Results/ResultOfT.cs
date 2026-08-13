using DuonDevKit.Core.Errors;

namespace DuonDevKit.Core.Results
{
    /// <summary>
    /// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/> —
    /// either a successful result carrying <see cref="Value"/>, or a failed result carrying an <see cref="Errors.Error"/>.
    /// </summary>
    public sealed class Result<T>
    {
        private readonly T? _value;

        /// <summary>Gets a value indicating whether the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>Gets a value indicating whether the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>Gets the error describing why the operation failed. Equals <see cref="Error.None"/> when successful.</summary>
        public Error Error { get; }

        /// <summary>
        /// Gets the value produced by a successful operation.
        /// Throws <see cref="InvalidOperationException"/> when accessed on a failed result.
        /// </summary>
        public T Value
        {
            get
            {
                if (IsFailure)
                    throw new InvalidOperationException("Cannot access the value of a failed result. Check IsSuccess/IsFailure first.");

                return _value!;
            }
        }

        private Result(bool isSuccess, Error error, T? value)
        {
            if (isSuccess && error != Error.None)
                throw new ArgumentException("A successful result cannot contain an error.", nameof(error));

            if (!isSuccess && error == Error.None)
                throw new ArgumentException("A failure result must contain an error.", nameof(error));

            IsSuccess = isSuccess;
            Error = error;
            _value = value;
        }

        /// <summary>Creates a successful result carrying the given value.</summary>
        public static Result<T> Success(T value)
            => new(true, Error.None, value);

        /// <summary>Creates a failed result carrying the given error.</summary>
        public static Result<T> Fail(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new(false, error, default);
        }

        /// <summary>Implicitly converts an error into a failed result.</summary>
        public static implicit operator Result<T>(Error error)
            => Fail(error);

        /// <summary>Invokes <paramref name="onSuccess"/> with the value when this result is successful, or <paramref name="onFailure"/> when it failed, and returns the produced value.</summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return IsSuccess ? onSuccess(Value) : onFailure(Error);
        }

        /// <summary>Transforms the value of a successful result using <paramref name="mapper"/>; propagates the error unchanged otherwise.</summary>
        public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            return IsSuccess ? Result<TOut>.Success(mapper(Value)) : Result<TOut>.Fail(Error);
        }

        /// <summary>Chains another result-returning operation when this result is successful; propagates the error unchanged otherwise.</summary>
        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);

            return IsSuccess ? binder(Value) : Result<TOut>.Fail(Error);
        }

        /// <summary>Converts a successful result into a failure carrying <paramref name="error"/> when <paramref name="predicate"/> returns <c>false</c>; propagates the error unchanged on an already-failed result, without invoking <paramref name="predicate"/>.</summary>
        public Result<T> Ensure(Func<T, bool> predicate, Error error)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(error);

            if (IsFailure) return this;
            return predicate(Value) ? this : Fail(error);
        }

        /// <inheritdoc />
        public override string ToString()
            => IsSuccess ? $"Success: {Value}" : $"Failure: {Error.Code} - {Error.Message}";
    }
}
