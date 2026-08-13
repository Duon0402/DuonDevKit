namespace DuonDevKit.Core.Results
{
    /// <summary>A single page of <typeparamref name="T"/> out of a larger, filtered result set.</summary>
    public sealed class PagedResult<T>
    {
        /// <summary>The items on this page.</summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>The 1-based page number this page represents.</summary>
        public int PageNumber { get; }

        /// <summary>The maximum number of items per page.</summary>
        public int PageSize { get; }

        /// <summary>The total number of items across all pages (i.e. matching the filter, not just this page).</summary>
        public int TotalCount { get; }

        /// <summary>The total number of pages, given <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>Whether a page before <see cref="PageNumber"/> exists.</summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>Whether a page after <see cref="PageNumber"/> exists.</summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>Creates a page of results.</summary>
        public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
            ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }
}
