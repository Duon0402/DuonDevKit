namespace DuonDevKit.AspNetCore
{
    /// <summary>Error codes used by <see cref="Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>An exception reached <see cref="ApplicationBuilderExtensions.UseDuonDevKitExceptionHandling"/> unhandled.</summary>
        public const string UnhandledException = "UNHANDLED_EXCEPTION";
    }
}
