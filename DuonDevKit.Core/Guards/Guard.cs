using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Extensions;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Guards
{
    /// <summary>Entry point for guard clauses. Use <see cref="Against"/>, e.g. <c>Guard.Against.Null(value, nameof(value))</c>.</summary>
    public static class Guard
    {
        /// <summary>Guard clauses that return a failed <see cref="Result"/> (instead of throwing) when the checked condition doesn't hold.</summary>
        public static class Against
        {
            /// <summary>Fails if <paramref name="value"/> is <c>null</c>.</summary>
            public static Result Null(object? value, string paramName)
                => value is null
                    ? Error.Validation(GuardErrorCodes.Null, $"{paramName} must not be null.")
                    : Result.Success();

            /// <summary>Fails if <paramref name="value"/> is <c>null</c>, empty, or whitespace only.</summary>
            public static Result NullOrEmpty(string? value, string paramName)
                => value.IsEmpty()
                    ? Error.Validation(GuardErrorCodes.NullOrEmpty, $"{paramName} must not be null or empty.")
                    : Result.Success();

            /// <summary>Fails if <paramref name="value"/> is zero or negative.</summary>
            public static Result NegativeOrZero(int value, string paramName)
                => value <= 0
                    ? Error.Validation(GuardErrorCodes.NegativeOrZero, $"{paramName} must be greater than zero.")
                    : Result.Success();

            /// <summary>Fails if <paramref name="value"/> is negative.</summary>
            public static Result Negative(int value, string paramName)
                => value < 0
                    ? Error.Validation(GuardErrorCodes.Negative, $"{paramName} must not be negative.")
                    : Result.Success();
        }
    }
}
