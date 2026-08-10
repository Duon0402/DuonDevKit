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
    }
}
