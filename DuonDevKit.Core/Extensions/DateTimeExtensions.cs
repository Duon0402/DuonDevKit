namespace DuonDevKit.Core.Extensions
{
    /// <summary>
    /// Extension methods for common <see cref="DateTime"/>/<see cref="DateTimeOffset"/> operations: range
    /// boundaries (day/week/month/year), comparisons (<see cref="IsBetween(DateTime, DateTime, DateTime, bool)"/>,
    /// weekday/weekend, past/future), and time zone conversion.
    /// <para>
    /// Range boundaries: every <c>StartOfX</c> returns midnight of the first day in the period; every
    /// <c>EndOfX</c> returns the last representable tick of the last day (<c>23:59:59.9999999</c>) rather
    /// than midnight of that day — so a range filter like
    /// <c>date &gt;= StartOfMonth &amp;&amp; date &lt;= EndOfMonth</c> actually includes every moment of
    /// the last day instead of excluding all but its first tick.
    /// </para>
    /// <para>
    /// <see cref="DateTime"/> overloads preserve <see cref="DateTime.Kind"/> where the result is still a
    /// <see cref="DateTime"/>, and are Kind-aware for "now"-relative comparisons
    /// (<see cref="IsToday(DateTime)"/>, <see cref="IsInPast(DateTime)"/>, <see cref="IsInFuture(DateTime)"/>).
    /// <see cref="DateTimeOffset"/> overloads are always unambiguous (an explicit UTC instant), and are
    /// generally preferable when a time zone is involved — see <see cref="ToTimeZone(DateTimeOffset, TimeZoneInfo)"/>.
    /// </para>
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

        // --- Comparisons ---

        /// <summary>
        /// Returns <c>true</c> if <paramref name="dateTime"/> falls within <paramref name="start"/> and
        /// <paramref name="end"/> — <paramref name="inclusive"/> controls whether the bounds themselves
        /// count (defaults to <c>true</c>). <paramref name="start"/>/<paramref name="end"/> are compared
        /// as given, in whichever order — swapped automatically if <paramref name="start"/> is after
        /// <paramref name="end"/>, so a reversed pair doesn't silently return <c>false</c> for everything.
        /// </summary>
        public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end, bool inclusive = true)
        {
            if (start > end) (start, end) = (end, start);
            return inclusive
                ? dateTime >= start && dateTime <= end
                : dateTime > start && dateTime < end;
        }

        /// <summary>Returns <c>true</c> if <paramref name="dateTime"/> and <paramref name="other"/> fall on the same calendar day.</summary>
        public static bool IsSameDay(this DateTime dateTime, DateTime other)
            => dateTime.Date == other.Date;

        /// <summary>Returns <c>true</c> if <paramref name="dateTime"/> falls on a Saturday or Sunday.</summary>
        public static bool IsWeekend(this DateTime dateTime)
            => dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        /// <summary>Returns <c>true</c> if <paramref name="dateTime"/> does not fall on a Saturday or Sunday.</summary>
        public static bool IsWeekday(this DateTime dateTime)
            => !dateTime.IsWeekend();

        /// <summary>
        /// Returns <c>true</c> if <paramref name="dateTime"/> falls on the current calendar day, comparing
        /// against <see cref="DateTime.UtcNow"/> when <paramref name="dateTime"/>'s <see cref="DateTime.Kind"/>
        /// is <see cref="DateTimeKind.Utc"/>, or <see cref="DateTime.Now"/> otherwise (treating
        /// <see cref="DateTimeKind.Unspecified"/> as local, matching <see cref="DateTime.ToLocalTime"/>'s
        /// own convention).
        /// </summary>
        public static bool IsToday(this DateTime dateTime)
            => dateTime.IsSameDay(dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);

        /// <summary>Returns <c>true</c> if <paramref name="dateTime"/> is before now — see <see cref="IsToday"/> for how "now" is chosen based on <see cref="DateTime.Kind"/>.</summary>
        public static bool IsInPast(this DateTime dateTime)
            => dateTime < (dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);

        /// <summary>Returns <c>true</c> if <paramref name="dateTime"/> is after now — see <see cref="IsToday"/> for how "now" is chosen based on <see cref="DateTime.Kind"/>.</summary>
        public static bool IsInFuture(this DateTime dateTime)
            => dateTime > (dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> falls within <paramref name="start"/> and <paramref name="end"/> (compared as absolute instants, so <c>Offset</c> differences don't matter) — see the <see cref="DateTime"/> overload for <paramref name="inclusive"/>/ordering behavior.</summary>
        public static bool IsBetween(this DateTimeOffset dateTimeOffset, DateTimeOffset start, DateTimeOffset end, bool inclusive = true)
        {
            if (start > end) (start, end) = (end, start);
            return inclusive
                ? dateTimeOffset >= start && dateTimeOffset <= end
                : dateTimeOffset > start && dateTimeOffset < end;
        }

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> and <paramref name="other"/> fall on the same calendar day in their respective offsets.</summary>
        public static bool IsSameDay(this DateTimeOffset dateTimeOffset, DateTimeOffset other)
            => dateTimeOffset.Date == other.Date;

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> falls on a Saturday or Sunday.</summary>
        public static bool IsWeekend(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> does not fall on a Saturday or Sunday.</summary>
        public static bool IsWeekday(this DateTimeOffset dateTimeOffset)
            => !dateTimeOffset.IsWeekend();

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> falls on the current calendar day in its own offset. Unlike the <see cref="DateTime"/> overload, this is always unambiguous — <see cref="DateTimeOffset"/> carries its own UTC instant.</summary>
        public static bool IsToday(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset.IsSameDay(dateTimeOffset.Offset == TimeSpan.Zero ? DateTimeOffset.UtcNow : DateTimeOffset.Now.ToOffset(dateTimeOffset.Offset));

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> is before the current instant.</summary>
        public static bool IsInPast(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset < DateTimeOffset.UtcNow;

        /// <summary>Returns <c>true</c> if <paramref name="dateTimeOffset"/> is after the current instant.</summary>
        public static bool IsInFuture(this DateTimeOffset dateTimeOffset)
            => dateTimeOffset > DateTimeOffset.UtcNow;

        // --- Time zone conversion ---

        /// <summary>
        /// Converts <paramref name="dateTime"/> to <paramref name="timeZone"/>, returning a
        /// <see cref="DateTimeKind.Unspecified"/> result (the .NET convention for "local to some
        /// specific, possibly non-machine, time zone" — see <see cref="TimeZoneInfo.ConvertTime(DateTime, TimeZoneInfo)"/>).
        /// Prefer the <see cref="DateTimeOffset"/> overload when possible — it carries the resulting
        /// offset explicitly instead of relying on the caller to remember which zone a bare
        /// <see cref="DateTime"/> is now expressed in.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="dateTime"/>.Kind is <see cref="DateTimeKind.Unspecified"/> — which time zone it's currently expressed in can't be inferred, so treating it as UTC or local would silently pick one.</exception>
        public static DateTime ToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("Cannot convert a DateTime with an Unspecified Kind to a time zone — its current time zone is unknown. Set Kind to Utc/Local first, or use the DateTimeOffset overload.", nameof(dateTime));

            return TimeZoneInfo.ConvertTime(dateTime, timeZone);
        }

        /// <summary>
        /// Treats <paramref name="dateTime"/> as a "wall clock" reading in <paramref name="timeZone"/>
        /// (its <see cref="DateTime.Kind"/> is ignored) and converts it to UTC — for parsing a business
        /// time like "9:00 AM in America/New_York" into an unambiguous instant. Throws for a time that
        /// doesn't exist (spring-forward DST gap) or is ambiguous (fall-back DST overlap) in
        /// <paramref name="timeZone"/> — resolve the ambiguity explicitly (e.g. pick the earlier/later
        /// occurrence) before calling this, rather than have it guess.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="dateTime"/> falls in a DST gap or overlap in <paramref name="timeZone"/>.</exception>
        public static DateTime ToUtcFrom(this DateTime dateTime, TimeZoneInfo timeZone)
        {
            var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);

            if (timeZone.IsInvalidTime(unspecified))
                throw new ArgumentException($"{dateTime:O} does not exist in time zone '{timeZone.Id}' (falls in a DST spring-forward gap).", nameof(dateTime));

            if (timeZone.IsAmbiguousTime(unspecified))
                throw new ArgumentException($"{dateTime:O} is ambiguous in time zone '{timeZone.Id}' (falls in a DST fall-back overlap) — resolve which occurrence is meant before converting.", nameof(dateTime));

            return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
        }

        /// <summary>
        /// Converts <paramref name="dateTimeOffset"/> to <paramref name="timeZone"/>, returning a
        /// <see cref="DateTimeOffset"/> with that zone's offset at this instant (already correct across
        /// a DST boundary — no ambiguity, since <see cref="DateTimeOffset"/> always carries its own UTC
        /// instant regardless of which zone it's currently expressed in).
        /// </summary>
        public static DateTimeOffset ToTimeZone(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZone)
            => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZone);
    }
}
