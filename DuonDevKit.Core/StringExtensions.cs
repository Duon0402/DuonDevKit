using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DuonDevKit.Core
{
    public static class StringExtensions
    {
        public static bool IsEmpty([NotNullWhen(false)] this string? value)
            => string.IsNullOrWhiteSpace(value);

        public static bool IsNotEmpty([NotNullWhen(true)] this string? value)
            => !value.IsEmpty();

        public static bool IsEmail(this string? value)
        {
            if (value.IsEmpty())
                return false;

            return EmailRegex().IsMatch(value);
        }

        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
        private static partial Regex EmailRegex();

        public static bool IsNotEmail(this string? value)
            => !value.IsEmail();

        public static bool EqualsIgnoreCase(this string? value, string? other)
            => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

        public static bool NotEqualsIgnoreCase(this string? value, string? other)
            => !value.EqualsIgnoreCase(other);
    }
}
