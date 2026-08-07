namespace DuonDevKit.Core.Guards
{
    /// <summary>Error codes used by <see cref="Error"/> instances raised by <see cref="Guard.Against"/>.</summary>
    public static class GuardErrorCodes
    {
        /// <summary>Raised by <see cref="Guard.Against.Null"/>.</summary>
        public const string Null = "GUARD001";

        /// <summary>Raised by <see cref="Guard.Against.NullOrEmpty"/>.</summary>
        public const string NullOrEmpty = "GUARD002";

        /// <summary>Raised by <see cref="Guard.Against.NegativeOrZero"/>.</summary>
        public const string NegativeOrZero = "GUARD003";

        /// <summary>Raised by <see cref="Guard.Against.Negative"/>.</summary>
        public const string Negative = "GUARD004";
    }
}
