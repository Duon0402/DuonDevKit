using DuonDevKit.Core.Errors;

namespace DuonDevKit.Core.Results
{
    /// <summary>
    /// Represents the outcome of an operation that does not return a value —
    /// either a success or a failure carrying an <see cref="Errors.Error"/>.
    /// </summary>
    public sealed class Result
    {
        public bool IsSuccess { get; }

        /// <summary>Gets a value indicating whether the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>Gets the error describing why the operation failed. Equals <see cref="Error.None"/> when successful.</summary>
        public Error Error { get; }

        private Result(bool isSuccess, Error error)
        {
            Error.ValidateInvariant(isSuccess, error, nameof(error));

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success()
            => new(true, Error.None);

        /// <summary>Creates a failed result carrying the given error.</summary>
        public static Result Fail(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new(false, error);
        }

        /// <summary>Creates a successful <see cref="Result{T}"/> carrying the given value. <typeparamref name="T"/> is inferred from <paramref name="value"/>.</summary>
        public static Result<T> Success<T>(T value)
            => Result<T>.Success(value);

        /// <summary>Creates a failed <see cref="Result{T}"/> carrying the given error.</summary>
        public static Result<T> Fail<T>(Error error)
            => Result<T>.Fail(error);

        /// <summary>Implicitly converts an error into a failed result.</summary>
        public static implicit operator Result(Error error)
            => Fail(error);

        /// <summary>Invokes <paramref name="onSuccess"/> when this result is successful, or <paramref name="onFailure"/> when it failed, and returns the produced value.</summary>
        public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return IsSuccess ? onSuccess() : onFailure(Error);
        }

        /// <summary>Returns the first failure among <paramref name="results"/>, or <see cref="Success()"/> if all succeeded (or none were given).</summary>
        public static Result Combine(params Result[] results)
        {
            ArgumentNullException.ThrowIfNull(results);

            foreach (var result in results)
            {
                if (result.IsFailure)
                    return result;
            }

            return Success();
        }

        /// <inheritdoc />
        public override string ToString()
            => IsSuccess ? "Success" : $"Failure: {Error.Code} - {Error.Message}";
    }
}
