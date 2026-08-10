using DuonDevKit.Core.Extensions;

namespace DuonDevKit.Core.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void StartOfDay_ReturnsMidnightOfSameDay()
        {
            var dateTime = new DateTime(2024, 3, 15, 13, 45, 30);

            Assert.Equal(new DateTime(2024, 3, 15, 0, 0, 0), dateTime.StartOfDay());
        }

        [Fact]
        public void EndOfDay_ReturnsLastTickOfSameDay()
        {
            var dateTime = new DateTime(2024, 3, 15, 13, 45, 30);

            Assert.Equal(new DateTime(2024, 3, 15, 23, 59, 59, 999).AddTicks(9999), dateTime.EndOfDay());
        }

        [Theory]
        [InlineData(2024, 3, 13, 2024, 3, 11)] // Wednesday -> Monday
        [InlineData(2024, 3, 11, 2024, 3, 11)] // Monday -> itself
        [InlineData(2024, 3, 17, 2024, 3, 11)] // Sunday -> preceding Monday
        public void StartOfWeek_DefaultsToMonday(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
        {
            var dateTime = new DateTime(year, month, day, 9, 0, 0);

            Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay), dateTime.StartOfWeek());
        }

        [Fact]
        public void StartOfWeek_WithSundayAsStart_TreatsSundayAsFirstDay()
        {
            var wednesday = new DateTime(2024, 3, 13);

            Assert.Equal(new DateTime(2024, 3, 10), wednesday.StartOfWeek(DayOfWeek.Sunday));
        }

        [Fact]
        public void EndOfWeek_ReturnsLastTickOfSeventhDay()
        {
            var wednesday = new DateTime(2024, 3, 13, 9, 0, 0);

            Assert.Equal(new DateTime(2024, 3, 17, 23, 59, 59, 999).AddTicks(9999), wednesday.EndOfWeek());
        }

        [Fact]
        public void StartOfMonth_ReturnsMidnightOfFirstDay()
        {
            var dateTime = new DateTime(2024, 3, 15, 13, 45, 30);

            Assert.Equal(new DateTime(2024, 3, 1), dateTime.StartOfMonth());
        }

        [Fact]
        public void EndOfMonth_ReturnsLastTickOfLastDay()
        {
            var dateTime = new DateTime(2024, 3, 15);

            Assert.Equal(new DateTime(2024, 3, 31, 23, 59, 59, 999).AddTicks(9999), dateTime.EndOfMonth());
        }

        [Fact]
        public void EndOfMonth_LeapYearFebruary_ReturnsFebruary29()
        {
            var dateTime = new DateTime(2024, 2, 10);

            Assert.Equal(new DateTime(2024, 2, 29, 23, 59, 59, 999).AddTicks(9999), dateTime.EndOfMonth());
        }

        [Fact]
        public void EndOfMonth_NonLeapYearFebruary_ReturnsFebruary28()
        {
            var dateTime = new DateTime(2023, 2, 10);

            Assert.Equal(new DateTime(2023, 2, 28, 23, 59, 59, 999).AddTicks(9999), dateTime.EndOfMonth());
        }

        [Fact]
        public void StartOfYear_ReturnsMidnightOfJanuaryFirst()
        {
            var dateTime = new DateTime(2024, 7, 4, 13, 45, 30);

            Assert.Equal(new DateTime(2024, 1, 1), dateTime.StartOfYear());
        }

        [Fact]
        public void EndOfYear_ReturnsLastTickOfDecember31st()
        {
            var dateTime = new DateTime(2024, 7, 4);

            Assert.Equal(new DateTime(2024, 12, 31, 23, 59, 59, 999).AddTicks(9999), dateTime.EndOfYear());
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void StartOfMonthAndStartOfYear_PreserveDateTimeKind(DateTimeKind kind)
        {
            var dateTime = new DateTime(2024, 3, 15, 13, 45, 30, kind);

            Assert.Equal(kind, dateTime.StartOfMonth().Kind);
            Assert.Equal(kind, dateTime.StartOfYear().Kind);
        }

        // --- DateTimeOffset overloads ---

        private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

        [Fact]
        public void StartOfDay_DateTimeOffset_ReturnsMidnightAtSameOffset()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 3, 15, 13, 45, 30, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 15, 0, 0, 0, Offset), dateTimeOffset.StartOfDay());
        }

        [Fact]
        public void EndOfDay_DateTimeOffset_ReturnsLastTickAtSameOffset()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 3, 15, 13, 45, 30, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 15, 23, 59, 59, 999, Offset).AddTicks(9999), dateTimeOffset.EndOfDay());
        }

        [Fact]
        public void StartOfWeek_DateTimeOffset_DefaultsToMonday()
        {
            var wednesday = new DateTimeOffset(2024, 3, 13, 9, 0, 0, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 11, 0, 0, 0, Offset), wednesday.StartOfWeek());
        }

        [Fact]
        public void EndOfWeek_DateTimeOffset_ReturnsLastTickOfSeventhDay()
        {
            var wednesday = new DateTimeOffset(2024, 3, 13, 9, 0, 0, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 17, 23, 59, 59, 999, Offset).AddTicks(9999), wednesday.EndOfWeek());
        }

        [Fact]
        public void StartOfMonth_DateTimeOffset_ReturnsMidnightOfFirstDayAtSameOffset()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 3, 15, 13, 45, 30, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 1, 0, 0, 0, Offset), dateTimeOffset.StartOfMonth());
        }

        [Fact]
        public void EndOfMonth_DateTimeOffset_ReturnsLastTickOfLastDay()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 3, 15, 0, 0, 0, Offset);

            Assert.Equal(new DateTimeOffset(2024, 3, 31, 23, 59, 59, 999, Offset).AddTicks(9999), dateTimeOffset.EndOfMonth());
        }

        [Fact]
        public void StartOfYear_DateTimeOffset_ReturnsMidnightOfJanuaryFirstAtSameOffset()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 7, 4, 13, 45, 30, Offset);

            Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, Offset), dateTimeOffset.StartOfYear());
        }

        [Fact]
        public void EndOfYear_DateTimeOffset_ReturnsLastTickOfDecember31st()
        {
            var dateTimeOffset = new DateTimeOffset(2024, 7, 4, 0, 0, 0, Offset);

            Assert.Equal(new DateTimeOffset(2024, 12, 31, 23, 59, 59, 999, Offset).AddTicks(9999), dateTimeOffset.EndOfYear());
        }

        [Fact]
        public void StartOfMonthAndStartOfYear_DateTimeOffset_PreserveOffset()
        {
            var negativeOffset = new DateTimeOffset(2024, 3, 15, 13, 45, 30, TimeSpan.FromHours(-5));

            Assert.Equal(TimeSpan.FromHours(-5), negativeOffset.StartOfMonth().Offset);
            Assert.Equal(TimeSpan.FromHours(-5), negativeOffset.StartOfYear().Offset);
        }

        // --- IsBetween / comparisons (DateTime) ---

        [Fact]
        public void IsBetween_ValueInsideRange_ReturnsTrue()
        {
            var value = new DateTime(2024, 3, 15);

            Assert.True(value.IsBetween(new DateTime(2024, 3, 1), new DateTime(2024, 3, 31)));
        }

        [Fact]
        public void IsBetween_ValueOutsideRange_ReturnsFalse()
        {
            var value = new DateTime(2024, 4, 1);

            Assert.False(value.IsBetween(new DateTime(2024, 3, 1), new DateTime(2024, 3, 31)));
        }

        [Fact]
        public void IsBetween_InclusiveByDefault_BoundsCount()
        {
            var start = new DateTime(2024, 3, 1);
            var end = new DateTime(2024, 3, 31);

            Assert.True(start.IsBetween(start, end));
            Assert.True(end.IsBetween(start, end));
        }

        [Fact]
        public void IsBetween_Exclusive_BoundsDoNotCount()
        {
            var start = new DateTime(2024, 3, 1);
            var end = new DateTime(2024, 3, 31);

            Assert.False(start.IsBetween(start, end, inclusive: false));
            Assert.False(end.IsBetween(start, end, inclusive: false));
        }

        [Fact]
        public void IsBetween_ReversedStartAndEnd_StillEvaluatesCorrectly()
        {
            var value = new DateTime(2024, 3, 15);

            Assert.True(value.IsBetween(new DateTime(2024, 3, 31), new DateTime(2024, 3, 1)));
        }

        [Fact]
        public void IsSameDay_SameCalendarDayDifferentTimes_ReturnsTrue()
        {
            var morning = new DateTime(2024, 3, 15, 6, 0, 0);
            var evening = new DateTime(2024, 3, 15, 22, 0, 0);

            Assert.True(morning.IsSameDay(evening));
        }

        [Fact]
        public void IsSameDay_DifferentCalendarDays_ReturnsFalse()
        {
            var day1 = new DateTime(2024, 3, 15);
            var day2 = new DateTime(2024, 3, 16);

            Assert.False(day1.IsSameDay(day2));
        }

        [Theory]
        [InlineData(2024, 3, 16, true)]  // Saturday
        [InlineData(2024, 3, 17, true)]  // Sunday
        [InlineData(2024, 3, 15, false)] // Friday
        public void IsWeekend_ReturnsExpected(int year, int month, int day, bool expected)
        {
            var dateTime = new DateTime(year, month, day);

            Assert.Equal(expected, dateTime.IsWeekend());
            Assert.Equal(!expected, dateTime.IsWeekday());
        }

        [Fact]
        public void IsToday_UtcNow_ReturnsTrue()
            => Assert.True(DateTime.UtcNow.IsToday());

        [Fact]
        public void IsToday_YesterdayUtc_ReturnsFalse()
            => Assert.False(DateTime.UtcNow.AddDays(-1).IsToday());

        [Fact]
        public void IsInPast_OneHourAgoUtc_ReturnsTrue()
            => Assert.True(DateTime.UtcNow.AddHours(-1).IsInPast());

        [Fact]
        public void IsInFuture_OneHourAheadUtc_ReturnsTrue()
            => Assert.True(DateTime.UtcNow.AddHours(1).IsInFuture());

        // --- IsBetween / comparisons (DateTimeOffset) ---

        [Fact]
        public void IsBetween_DateTimeOffset_ComparesAbsoluteInstantsRegardlessOfOffset()
        {
            var value = new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero);
            // Same instant expressed at a +9 offset — still the same point in time.
            var startAtDifferentOffset = new DateTimeOffset(2024, 3, 15, 21, 0, 0, TimeSpan.FromHours(9));

            Assert.True(value.IsBetween(startAtDifferentOffset, value));
        }

        [Fact]
        public void IsSameDay_DateTimeOffset_DifferentDays_ReturnsFalse()
        {
            var day1 = new DateTimeOffset(2024, 3, 15, 0, 0, 0, Offset);
            var day2 = new DateTimeOffset(2024, 3, 16, 0, 0, 0, Offset);

            Assert.False(day1.IsSameDay(day2));
        }

        [Fact]
        public void IsWeekend_DateTimeOffset_Saturday_ReturnsTrue()
        {
            var saturday = new DateTimeOffset(2024, 3, 16, 0, 0, 0, Offset);

            Assert.True(saturday.IsWeekend());
            Assert.False(saturday.IsWeekday());
        }

        [Fact]
        public void IsInPast_DateTimeOffset_OneHourAgo_ReturnsTrue()
            => Assert.True(DateTimeOffset.UtcNow.AddHours(-1).IsInPast());

        [Fact]
        public void IsInFuture_DateTimeOffset_OneHourAhead_ReturnsTrue()
            => Assert.True(DateTimeOffset.UtcNow.AddHours(1).IsInFuture());

        // --- Time zone conversion ---

        private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        private static readonly TimeZoneInfo Tokyo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

        [Fact]
        public void ToTimeZone_DateTime_UtcKind_ConvertsToTargetZoneWallClock()
        {
            var utcNoon = new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc); // EDT = UTC-4 in June

            var converted = utcNoon.ToTimeZone(NewYork);

            Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0), converted);
        }

        [Fact]
        public void ToTimeZone_DateTime_UnspecifiedKind_Throws()
        {
            var unspecified = new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Unspecified);

            Assert.Throws<ArgumentException>(() => unspecified.ToTimeZone(NewYork));
        }

        [Fact]
        public void ToUtcFrom_NormalWallClockTime_ConvertsCorrectly()
        {
            var nySummerMorning = new DateTime(2024, 6, 15, 9, 0, 0); // EDT = UTC-4

            var utc = nySummerMorning.ToUtcFrom(NewYork);

            Assert.Equal(new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc), utc);
        }

        [Fact]
        public void ToUtcFrom_TimeInDstSpringForwardGap_Throws()
        {
            // 2024-03-10 02:30 does not exist in America/New_York (clocks jump 2:00 -> 3:00).
            var gap = new DateTime(2024, 3, 10, 2, 30, 0);

            Assert.Throws<ArgumentException>(() => gap.ToUtcFrom(NewYork));
        }

        [Fact]
        public void ToUtcFrom_TimeInDstFallBackOverlap_Throws()
        {
            // 2024-11-03 01:30 occurs twice in America/New_York (clocks fall back 2:00 -> 1:00).
            var ambiguous = new DateTime(2024, 11, 3, 1, 30, 0);

            Assert.Throws<ArgumentException>(() => ambiguous.ToUtcFrom(NewYork));
        }

        [Fact]
        public void ToTimeZone_DateTimeOffset_ConvertsToTargetOffsetPreservingInstant()
        {
            var utcNoon = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

            var converted = utcNoon.ToTimeZone(Tokyo); // UTC+9, no DST

            Assert.Equal(TimeSpan.FromHours(9), converted.Offset);
            Assert.Equal(new DateTime(2024, 6, 15, 21, 0, 0), converted.DateTime);
            Assert.Equal(utcNoon, converted); // same absolute instant
        }
    }
}
