namespace DuonDevKit.AspNetCore
{
    /// <summary>Error codes used by <see cref="Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>An exception reached <see cref="ApplicationBuilderExtensions.UseDuonDevKitExceptionHandling"/> unhandled.</summary>
        public const string UnhandledException = "UNHANDLED_EXCEPTION";

        /// <summary>A <see cref="Validation.ValidationFilter{T}"/>-validated request parameter failed <see cref="System.ComponentModel.DataAnnotations"/> validation. Shares its value with <see cref="Core.Validation.ValidationErrorCodes.Invalid"/> — same underlying validation mechanism, same code.</summary>
        public const string ValidationFailed = Core.Validation.ValidationErrorCodes.Invalid;
    }
}
