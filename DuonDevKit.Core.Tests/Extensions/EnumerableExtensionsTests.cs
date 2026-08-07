using DuonDevKit.Core.Extensions;

namespace DuonDevKit.Core.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void IsEmpty_ReturnsTrue_ForNull()
        {
            IEnumerable<int>? source = null;

            Assert.True(source.IsEmpty());
        }

        [Fact]
        public void IsEmpty_ReturnsTrue_ForEmptyList()
        {
            var source = new List<int>();

            Assert.True(source.IsEmpty());
        }

        [Fact]
        public void IsEmpty_ReturnsFalse_ForNonEmptyList()
        {
            var source = new List<int> { 1, 2, 3 };

            Assert.False(source.IsEmpty());
        }

        [Fact]
        public void IsEmpty_ReturnsTrue_ForEmptyLazySequence()
        {
            static IEnumerable<int> Empty()
            {
                yield break;
            }

            Assert.True(Empty().IsEmpty());
        }

        [Fact]
        public void IsEmpty_ReturnsFalse_ForNonEmptyLazySequence()
        {
            static IEnumerable<int> One()
            {
                yield return 1;
            }

            Assert.False(One().IsEmpty());
        }

        [Fact]
        public void IsNotEmpty_ReturnsFalse_ForNull()
        {
            IEnumerable<int>? source = null;

            Assert.False(source.IsNotEmpty());
        }

        [Fact]
        public void IsNotEmpty_ReturnsTrue_ForNonEmptyList()
        {
            var source = new List<int> { 1 };

            Assert.True(source.IsNotEmpty());
        }

        [Theory]
        [InlineData(2, 1, 2, 3)]
        [InlineData(1, 1)]
        public void In_ReturnsTrue_WhenItemIsAmongValues(int item, params int[] values)
        {
            Assert.True(item.In(values));
        }

        [Fact]
        public void In_ReturnsFalse_WhenItemIsNotAmongValues()
        {
            Assert.False(4.In(1, 2, 3));
        }

        [Fact]
        public void NotIn_ReturnsTrue_WhenItemIsNotAmongValues()
        {
            Assert.True(4.NotIn(1, 2, 3));
        }

        [Fact]
        public void NotIn_ReturnsFalse_WhenItemIsAmongValues()
        {
            Assert.False(2.NotIn(1, 2, 3));
        }
    }
}
