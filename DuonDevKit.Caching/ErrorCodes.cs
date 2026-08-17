namespace DuonDevKit.Caching
{
    /// <summary>Error codes used by <see cref="DuonDevKit.Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>An exception occurred while reading, writing, or removing a cache entry (e.g. Redis connectivity failure, serialization failure).</summary>
        public const string CacheUnavailable = "CACHE001";
    }
}