using DuonDevKit.Core.Validation;

namespace DuonDevKit.Validation
{
    /// <summary>Error codes used by <see cref="DuonDevKit.Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>A FluentValidation <c>IValidator&lt;T&gt;</c> run produced one or more validation failures. Shares its value with <see cref="ValidationErrorCodes.Invalid"/> — the DataAnnotations and FluentValidation paths report the same conceptual failure under the same code.</summary>
        public const string ValidationFailed = ValidationErrorCodes.Invalid;
    }
}
