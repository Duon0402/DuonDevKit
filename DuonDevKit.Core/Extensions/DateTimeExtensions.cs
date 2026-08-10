namespace DuonDevKit.Core.Extensions
{
    /// <summary>
    /// Extension methods for common <see cref="DateTime"/>/<see cref="DateTimeOffset"/> range boundaries
    /// (day/week/month/year). Every <c>StartOfX</c> returns midnight of the first day in the period;
    /// every <c>EndOfX</c> returns the last representable tick of the last day (<c>23:59:59.9999999</c>)
    /// rather than midnight of that day — so a range filter like
    /// <c>date &gt;= StartOfMonth &amp;&amp; date &lt;= EndOfMonth</c> actually includes every moment of
    /// the last day instead of excluding all but its first tick. <see cref="DateTime"/> overloads
    /// preserve <see cref="DateTime.Kind"/>; <see cref="DateTimeOffset"/> overloads preserve
    /// <see cref="DateTimeOffset.Offset"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>Returns midnight (<c>00:00:00</c>) of <paramref name="dateTime"/>'s day.</summary>
        public static DateTime StartOfDay(this DateTime dateTime)
            => dateTime.Date;

        /// <summary>Returns the last tick (<c>23:59:59.9999999</c>) of <paramref name="dateTime"/>'s day.</summary>
        public static DateTime EndOfDay(this DateTime dateTime)
            => dateTime.Date.AddDays(1).AddTicks(-1);

        /// <summary>Returns midnight of the first day of the week containing <paramref name="dateTime"/>, where the week starts on <paramref name="startOfWeek"/> (defaults to Monday).</summary>
        public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.Date.AddDays(-diff);
        }

        /// <summary>Returns the last tick of the last day of the week containing <paramref name="dateTime"/>, where the week starts on <paramref name="startOfWeek"/> (defaults to Monday).</summary>
        public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
            => dateTime.StartOfWeek(startOfWeek).AddDays(7).AddTicks(-1);

        /// <summary>Returns midnight of the first day of <paramref name="dateTime"/>'s month.</summary>
        public static DateTime StartOfMonth(this DateTime dateTime)
            => new(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);

        /// <summary>Returns the last tick of the last day of <paramref name="dateTime"/>'s month.</summary>
        public static DateTime EndOfMonth(this DateTime dateTime)
            => dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);

        /// <summary>Returns midnight of January 1st of <paramref name="dateTime"/>'s year.</summary>
        public static DateTime StartOfYear(this DateTime dateTime)
            => new(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);

        /// <summary>Returns the last tick of December 31st of <paramref name="dateTime"/>'s year.</summary>
        public static DateTime EndOfYear(this DateTime dateTime)
            => dateTime.StartOfYear().AddYears(1).AddTicks(-1);

        /// <summary>Returns midnight (<c>00:00:00</c>) of <paramref name="dateTimeOffset"/>'s day, at the same offset.</summary>
        public static DateTimeOffset StartOfDay(this DateTimeOffset dateTimeOffset)
            => new(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, dateTimeOffset.Offset);

        /// <summary>Returns the last tick (<c>23:59:59.9999999</c>) of <paramref name="dateTimeOffset"/>'s day, at the same offset.</summary>
        public static DateTimeOffset EndOfDay(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset.StartOfDay().AddDays(1).AddTicks(-1);

        /// <summary>Returns midnight of the first day of the week containing <paramref name="dateTimeOffset"/>, where the week starts on <paramref name="startOfWeek"/> (defaults to Monday).</summary>
        public static DateTimeOffset StartOfWeek(this DateTimeOffset dateTimeOffset, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = (7 + (dateTimeOffset.DayOfWeek - startOfWeek)) % 7;
            return dateTimeOffset.StartOfDay().AddDays(-diff);
        }

        /// <summary>Returns the last tick of the last day of the week containing <paramref name="dateTimeOffset"/>, where the week starts on <paramref name="startOfWeek"/> (defaults to Monday).</summary>
        public static DateTimeOffset EndOfWeek(this DateTimeOffset dateTimeOffset, DayOfWeek startOfWeek = DayOfWeek.Monday)
            => dateTimeOffset.StartOfWeek(startOfWeek).AddDays(7).AddTicks(-1);

        /// <summary>Returns midnight of the first day of <paramref name="dateTimeOffset"/>'s month, at the same offset.</summary>
        public static DateTimeOffset StartOfMonth(this DateTimeOffset dateTimeOffset)
            => new(dateTimeOffset.Year, dateTimeOffset.Month, 1, 0, 0, 0, dateTimeOffset.Offset);

        /// <summary>Returns the last tick of the last day of <paramref name="dateTimeOffset"/>'s month.</summary>
        public static DateTimeOffset EndOfMonth(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset.StartOfMonth().AddMonths(1).AddTicks(-1);

        /// <summary>Returns midnight of January 1st of <paramref name="dateTimeOffset"/>'s year, at the same offset.</summary>
        public static DateTimeOffset StartOfYear(this DateTimeOffset dateTimeOffset)
            => new(dateTimeOffset.Year, 1, 1, 0, 0, 0, dateTimeOffset.Offset);

        /// <summary>Returns the last tick of December 31st of <paramref name="dateTimeOffset"/>'s year.</summary>
        public static DateTimeOffset EndOfYear(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset.StartOfYear().AddYears(1).AddTicks(-1);
    }
}
