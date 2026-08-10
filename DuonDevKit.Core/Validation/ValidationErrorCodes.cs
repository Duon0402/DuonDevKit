namespace DuonDevKit.Core.Validation
{
    /// <summary>Error codes for <see cref="DataAnnotationsValidator"/> — split into its own file to avoid hardcoded strings.</summary>
    public static class ValidationErrorCodes
    {
        /// <summary><see cref="DataAnnotationsValidator.Validate"/> found one or more <see cref="System.ComponentModel.DataAnnotations"/> violations.</summary>
        public const string Invalid = "VALIDATION001";
    }
}
