namespace DuonDevKit.Dapper
{
    /// <summary>Error codes used by <see cref="DuonDevKit.Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>A <c>System.Data.Common.DbException</c> occurred while running a Dapper query/command via <see cref="DapperQueries"/>.</summary>
        public const string QueryFailed = "DAPPER001";
    }
}
