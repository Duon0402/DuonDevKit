using DuonDevKit.Core.Extensions;

namespace DuonDevKit.Core.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void IsEmpty_ReturnsTrue_ForNullOrWhitespace(string? value)
        {
            Assert.True(value.IsEmpty());
        }

        [Theory]
        [InlineData("a")]
        [InlineData("hello world")]
        [InlineData("  padded  ")]
        public void IsEmpty_ReturnsFalse_ForNonEmptyContent(string? value)
        {
            Assert.False(value.IsEmpty());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsNotEmpty_ReturnsFalse_ForNullOrWhitespace(string? value)
        {
            Assert.False(value.IsNotEmpty());
        }

        [Theory]
        [InlineData("a")]
        [InlineData("hello world")]
        public void IsNotEmpty_ReturnsTrue_ForNonEmptyContent(string? value)
        {
            Assert.True(value.IsNotEmpty());
        }

        [Theory]
        [InlineData("a@b.com")]
        [InlineData("first.last@example.com")]
        [InlineData("user+tag@sub.example.co")]
        public void IsEmail_ReturnsTrue_ForValidFormat(string value)
        {
            Assert.True(value.IsEmail());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("a@b")]
        [InlineData("a@b@c.com")]
        [InlineData("@b.com")]
        [InlineData("a@.com")]
        [InlineData("a b@c.com")]
        public void IsEmail_ReturnsFalse_ForInvalidOrEmptyValue(string? value)
        {
            Assert.False(value.IsEmail());
        }

        [Theory]
        [InlineData("a@b.com")]
        public void IsNotEmail_ReturnsFalse_ForValidFormat(string value)
        {
            Assert.False(value.IsNotEmail());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("a@b")]
        [InlineData("a@b@c.com")]
        public void IsNotEmail_ReturnsTrue_ForInvalidOrEmptyValue(string? value)
        {
            Assert.True(value.IsNotEmail());
        }

        [Theory]
        [InlineData("ABC", "abc")]
        [InlineData("abc", "ABC")]
        [InlineData("AbC", "aBc")]
        [InlineData("same", "same")]
        [InlineData("", "")]
        public void EqualsIgnoreCase_ReturnsTrue_ForSameContentDifferentCase(string? value, string? other)
        {
            Assert.True(value.EqualsIgnoreCase(other));
        }

        [Fact]
        public void EqualsIgnoreCase_ReturnsTrue_WhenBothAreNull()
        {
            string? value = null;
            string? other = null;

            Assert.True(value.EqualsIgnoreCase(other));
        }

        [Theory]
        [InlineData("abc", "def")]
        [InlineData("abc", null)]
        [InlineData(null, "abc")]
        [InlineData("abc", "abcd")]
        public void EqualsIgnoreCase_ReturnsFalse_ForDifferentContent(string? value, string? other)
        {
            Assert.False(value.EqualsIgnoreCase(other));
        }

        [Theory]
        [InlineData("ABC", "abc")]
        [InlineData("same", "same")]
        public void NotEqualsIgnoreCase_ReturnsFalse_ForSameContentDifferentCase(string? value, string? other)
        {
            Assert.False(value.NotEqualsIgnoreCase(other));
        }

        [Theory]
        [InlineData("abc", "def")]
        [InlineData("abc", null)]
        [InlineData(null, "abc")]
        public void NotEqualsIgnoreCase_ReturnsTrue_ForDifferentContent(string? value, string? other)
        {
            Assert.True(value.NotEqualsIgnoreCase(other));
        }
    }
}
