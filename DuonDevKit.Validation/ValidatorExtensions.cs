using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using FluentValidation;
using FluentValidation.Results;

namespace DuonDevKit.Validation
{
    /// <summary>
    /// Bridges FluentValidation's <see cref="IValidator{T}"/>/<see cref="ValidationResult"/> into
    /// <see cref="Result"/>, so a validator plugs into the same success/failure flow as the rest of
    /// DuonDevKit without the caller touching <see cref="ValidationResult"/> directly.
    /// </summary>
    /// <remarks>
    /// The resulting <see cref="Error"/> carries every blocking (<see cref="Severity.Error"/>) failure
    /// joined into one message (<c>"PropertyName: ErrorMessage; ..."</c>) — <see cref="Error"/> has no
    /// field-level structure to preserve a per-property error list. For a Minimal API endpoint that needs
    /// a field-level <c>{ "PropertyName": ["message"] }</c> response body instead, use
    /// <c>DuonDevKit.AspNetCore</c>'s <c>WithDuonDevKitFluentValidation&lt;T&gt;()</c>, which validates
    /// directly against this same <see cref="IValidator{T}"/> without going through this extension.
    /// </remarks>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Runs <paramref name="validator"/> against <paramref name="instance"/> and converts the result
        /// to a <see cref="Result"/>. Named <c>ValidateToResult</c> rather than <c>Validate</c> so it
        /// doesn't shadow <see cref="IValidator{T}.Validate(T)"/>'s own overloads (an extension method
        /// never wins overload resolution against an instance method of the same name, so a same-named
        /// extension would silently never be called).
        /// </summary>
        public static Result ValidateToResult<T>(this IValidator<T> validator, T instance)
        {
            ArgumentNullException.ThrowIfNull(validator);

            return validator.Validate(instance).ToResult();
        }

        /// <summary>Async counterpart of <see cref="ValidateToResult{T}"/> — see its remarks for why this isn't named <c>ValidateAsync</c>.</summary>
        public static async Task<Result> ValidateToResultAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(validator);

            var result = await validator.ValidateAsync(instance, ct);
            return result.ToResult();
        }

        /// <summary>
        /// Converts a FluentValidation <see cref="ValidationResult"/> into a <see cref="Result"/> — a
        /// failure joining every <see cref="Severity.Error"/> <see cref="ValidationFailure"/> into one
        /// message, or success if there are none. A rule marked <c>.WithSeverity(Severity.Warning)</c> or
        /// <c>Severity.Info</c> makes FluentValidation report <see cref="ValidationResult.IsValid"/> as
        /// <c>false</c>, but doesn't fail the <see cref="Result"/> here — those severities are meant to be
        /// non-blocking.
        /// </summary>
        public static Result ToResult(this ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            var blocking = validationResult.Errors.Where(e => e.Severity == Severity.Error).ToList();
            if (blocking.Count == 0)
                return Result.Success();

            var message = string.Join("; ", blocking.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            return Error.Validation(ErrorCodes.ValidationFailed, message);
        }
    }
}
