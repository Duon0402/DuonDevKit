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
    /// The resulting <see cref="Error"/> carries every failure joined into one message
    /// (<c>"PropertyName: ErrorMessage; ..."</c>) — <see cref="Error"/> has no field-level structure to
    /// preserve a per-property error list. For an HTTP endpoint that needs a field-level
    /// <c>{ "PropertyName": ["message"] }</c> response body, validate directly against
    /// <see cref="IValidator{T}"/>/<see cref="ValidationResult"/> instead of going through this
    /// extension — see <c>DuonDevKit.AspNetCore</c>'s <c>WithDuonDevKitValidation&lt;T&gt;()</c> for that
    /// case (DataAnnotations-based, so it needs no dependency on this package).
    /// </remarks>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Runs <paramref name="validator"/> against <paramref name="instance"/> and converts the result
        /// to a <see cref="Result"/>. Named <c>ValidateToResult</c> rather than <c>Validate</c> so it
        /// doesn't shadow <see cref="IValidator{T}.Validate(ValidationContext{T})"/>'s own overloads
        /// (an extension method never wins overload resolution against an instance method of the same
        /// name, so a same-named extension would silently never be called).
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

        /// <summary>Converts a FluentValidation <see cref="ValidationResult"/> into a <see cref="Result"/> — success if <see cref="ValidationResult.IsValid"/>, otherwise a failure joining every <see cref="ValidationFailure"/> into one message.</summary>
        public static Result ToResult(this ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            if (validationResult.IsValid)
                return Result.Success();

            var message = string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            return Error.Validation(ErrorCodes.ValidationFailed, message);
        }
    }
}
