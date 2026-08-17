using System.ComponentModel.DataAnnotations;
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Validation
{
    /// <summary>
    /// Runs <see cref="System.ComponentModel.DataAnnotations"/> validation (<see cref="RequiredAttribute"/>,
    /// <see cref="MaxLengthAttribute"/>, <see cref="RangeAttribute"/>, a custom <see cref="IValidatableObject"/>,
    /// etc.) against any object and converts the outcome to a <see cref="Result"/> — no third-party
    /// dependency, unlike <c>DuonDevKit.Validation</c>'s FluentValidation integration. Prefer this for
    /// straightforward attribute-based checks; reach for <c>DuonDevKit.Validation</c> when rules need to
    /// be conditional, compare properties against each other, or call out to a database/service.
    /// </summary>
    /// <remarks>
    /// This is a thin wrapper over <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>,
    /// which only inspects <paramref name="instance"/>'s own properties — a well-known DataAnnotations
    /// limitation, not something this wrapper adds or could remove. A nested complex property or a
    /// collection of sub-objects is <em>not</em> recursively validated; if a request DTO has nested
    /// objects that need their own rules checked, either give the nested type its own
    /// <see cref="IValidatableObject"/> implementation that validates it manually, or use
    /// <c>DuonDevKit.Validation</c>, whose FluentValidation-based validators support nested/child
    /// validators natively.
    /// </remarks>
    public static class DataAnnotationsValidator
    {
        /// <summary>
        /// Validates every property decorated with a <see cref="ValidationAttribute"/> on
        /// <paramref name="instance"/> (and runs <see cref="IValidatableObject.Validate"/> if it's
        /// implemented) — success if none reported a violation, otherwise a failure joining every
        /// violation into one message.
        /// </summary>
        public static Result Validate(object instance)
        {
            var results = Collect(instance);
            if (results.Count == 0)
                return Result.Success();

            var message = string.Join("; ", results.Select(FormatFailure));
            return Error.Validation(ValidationErrorCodes.Invalid, message);
        }

        /// <summary>
        /// Runs the same underlying <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>
        /// call as <see cref="Validate"/>, returning the raw violations for callers that need their own
        /// projection instead of a joined-message <see cref="Result"/> (e.g. <c>DuonDevKit.AspNetCore</c>'s
        /// <c>ValidationFilter&lt;T&gt;</c>, which maps them to a field-name-to-messages dictionary).
        /// Internal: the raw <see cref="ValidationResult"/> shape isn't part of this library's public,
        /// <see cref="Result"/>-based surface.
        /// </summary>
        internal static IReadOnlyList<ValidationResult> Collect(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            var context = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
            return results;
        }

        private static string FormatFailure(ValidationResult result)
        {
            var members = string.Join(", ", result.MemberNames);
            return members.Length > 0 ? $"{members}: {result.ErrorMessage}" : result.ErrorMessage ?? "Invalid value.";
        }
    }
}
