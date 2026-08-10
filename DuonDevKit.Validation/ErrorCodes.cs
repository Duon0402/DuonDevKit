namespace DuonDevKit.Validation
{
    /// <summary>Error codes used by <see cref="DuonDevKit.Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>A FluentValidation <c>IValidator&lt;T&gt;</c> run produced one or more validation failures.</summary>
        public const string ValidationFailed = "VALID001";
    }
}
