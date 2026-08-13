using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Tests.Results
{
    public class PagedResultTests
    {
        [Fact]
        public void Constructor_ValidArguments_SetsProperties()
        {
            var paged = new PagedResult<int>([1, 2, 3], pageNumber: 1, pageSize: 3, totalCount: 10);

            Assert.Equal([1, 2, 3], paged.Items);
            Assert.Equal(1, paged.PageNumber);
            Assert.Equal(3, paged.PageSize);
            Assert.Equal(10, paged.TotalCount);
            Assert.Equal(4, paged.TotalPages);
            Assert.False(paged.HasPreviousPage);
            Assert.True(paged.HasNextPage);
        }

        [Fact]
        public void Constructor_NullItems_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PagedResult<int>(null!, 1, 10, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_NonPositivePageNumber_Throws(int pageNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResult<int>([], pageNumber, 10, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_NonPositivePageSize_Throws(int pageSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResult<int>([], 1, pageSize, 0));
        }

        [Fact]
        public void Constructor_NegativeTotalCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResult<int>([], 1, 10, -1));
        }
    }
}
