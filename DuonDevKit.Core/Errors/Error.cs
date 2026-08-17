namespace DuonDevKit.Core.Errors
{
    /// <summary>
    /// Represents an error with a <see cref="ErrorType"/>, a machine-readable <see cref="Code"/>,
    /// and a human-readable <see cref="Message"/>.
    /// </summary>
    /// <remarks>
    /// The constructor is intentionally private. Instances can only be produced via <see cref="None"/>
    /// or one of the static factory methods (<see cref="Validation"/>, <see cref="Business"/>, etc.), so
    /// <see cref="Type"/> can never disagree with how the error was actually created — e.g. it is not
    /// possible to construct an error with <see cref="ErrorType.None"/> but a non-empty <see cref="Code"/>
    /// or <see cref="Message"/>, which would otherwise let a failure result carry a "no error" type.
    /// </remarks>
    public sealed record Error
    {
        /// <summary>Gets the error classification.</summary>
        public ErrorType Type { get; }

        /// <summary>Gets the machine-readable error code.</summary>
        public string Code { get; }

        /// <summary>Gets the human-readable error message.</summary>
        public string Message { get; }

        private Error(ErrorType type, string code, string message)
        {
            Type = type;
            Code = code;
            Message = message;
        }

        /// <summary>Represents the absence of an error. Used by successful results.</summary>
        public static readonly Error None =
            new(ErrorType.None, string.Empty, string.Empty);

        /// <summary>Creates a validation error (invalid input).</summary>
        public static Error Validation(string code, string message)
            => new(ErrorType.Validation, code, message);

        /// <summary>Creates a business rule violation error.</summary>
        public static Error Business(string code, string message)
            => new(ErrorType.Business, code, message);

        /// <summary>Creates a not-found error (missing resource).</summary>
        public static Error NotFound(string code, string message)
            => new(ErrorType.NotFound, code, message);

        /// <summary>Creates a conflict error (e.g. duplicate resource, state conflict).</summary>
        public static Error Conflict(string code, string message)
            => new(ErrorType.Conflict, code, message);

        /// <summary>Creates an unauthorized error (missing or invalid credentials).</summary>
        public static Error Unauthorized(string code, string message)
            => new(ErrorType.Unauthorized, code, message);

        /// <summary>Creates a forbidden error (authenticated but not allowed).</summary>
        public static Error Forbidden(string code, string message)
            => new(ErrorType.Forbidden, code, message);

        /// <summary>Creates an unexpected/unhandled error.</summary>
        public static Error Unexpected(string code, string message)
            => new(ErrorType.Unexpected, code, message);

        /// <summary>Throws <see cref="ArgumentException"/> if <paramref name="error"/> disagrees with <paramref name="isSuccess"/> — shared by <see cref="Results.Result"/> and <see cref="Results.Result{T}"/> so both enforce the same success/error invariant identically.</summary>
        internal static void ValidateInvariant(bool isSuccess, Error error, string paramName)
        {
            if (isSuccess && error != None)
                throw new ArgumentException("A successful result cannot contain an error.", paramName);

            if (!isSuccess && error == None)
                throw new ArgumentException("A failure result must contain an error.", paramName);
        }
    }
}
