using System.Diagnostics.CodeAnalysis;

namespace DuonDevKit.Core.Extensions
{
    /// <summary>Extension methods for common <see cref="IEnumerable{T}"/> checks and membership tests.</summary>
    public static class EnumerableExtensions
    {
        /// <summary>Returns <c>true</c> if <paramref name="source"/> is <c>null</c> or contains no elements.</summary>
        public static bool IsEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? source)
        {
            if (source is null) return true;
            if (source.TryGetNonEnumeratedCount(out var count)) return count == 0;
            using var e = source.GetEnumerator();
            return !e.MoveNext();
        }

        /// <summary>Returns <c>true</c> if <paramref name="source"/> is not <c>null</c> and contains at least one element.</summary>
        public static bool IsNotEmpty<T>([NotNullWhen(true)] this IEnumerable<T>? source)
            => !source.IsEmpty();

        /// <summary>Returns <c>true</c> if <paramref name="item"/> equals any of <paramref name="items"/>.</summary>
        public static bool In<T>(this T item, params T[] items)
            => items.Contains(item);

        /// <summary>Returns <c>true</c> if <paramref name="item"/> does not equal any of <paramref name="items"/>.</summary>
        public static bool NotIn<T>(this T item, params T[] items)
            => !item.In(items);
    }
}
