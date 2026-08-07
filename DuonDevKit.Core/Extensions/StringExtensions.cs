using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DuonDevKit.Core.Extensions
{
    /// <summary>
    /// Extension methods for common <see cref="string"/> checks (emptiness, email format,
    /// case-insensitive comparison).
    /// </summary>
    public static partial class StringExtensions
    {
        /// <summary>Returns <c>true</c> if <paramref name="value"/> is <c>null</c>, empty, or whitespace only.</summary>
        public static bool IsEmpty([NotNullWhen(false)] this string? value)
            => string.IsNullOrWhiteSpace(value);

        /// <summary>Returns <c>true</c> if <paramref name="value"/> is not <c>null</c>, empty, or whitespace only.</summary>
        public static bool IsNotEmpty([NotNullWhen(true)] this string? value)
            => !value.IsEmpty();

        /// <summary>Returns <c>true</c> if <paramref name="value"/> is a non-empty string matching a basic email format.</summary>
        public static bool IsEmail(this string? value)
        {
            if (value.IsEmpty())
                return false;

            return EmailRegex().IsMatch(value);
        }

        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
        private static partial Regex EmailRegex();

        /// <summary>Returns <c>true</c> if <paramref name="value"/> does not match a basic email format.</summary>
        public static bool IsNotEmail(this string? value)
            => !value.IsEmail();

        /// <summary>Returns <c>true</c> if <paramref name="value"/> equals <paramref name="other"/>, ignoring case.</summary>
        public static bool EqualsIgnoreCase(this string? value, string? other)
            => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

        /// <summary>Returns <c>true</c> if <paramref name="value"/> does not equal <paramref name="other"/>, ignoring case.</summary>
        public static bool NotEqualsIgnoreCase(this string? value, string? other)
            => !value.EqualsIgnoreCase(other);
    }
}
