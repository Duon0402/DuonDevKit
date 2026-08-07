namespace DuonDevKit.Core.Errors
{
    /// <summary>Classifies the kind of failure an <see cref="Error"/> represents.</summary>
    public enum ErrorType
    {
        /// <summary>No error. Used by successful results.</summary>
        None = 0,

        /// <summary>Invalid input supplied by the caller.</summary>
        Validation = 1,

        /// <summary>A business/domain rule was violated.</summary>
        Business = 2,

        /// <summary>The requested resource does not exist.</summary>
        NotFound = 3,

        /// <summary>The request conflicts with the current state of the resource.</summary>
        Conflict = 4,

        /// <summary>The caller is not authenticated.</summary>
        Unauthorized = 5,

        /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
        Forbidden = 6,

        /// <summary>An unexpected, unhandled error occurred.</summary>
        Unexpected = 7
    }
}
