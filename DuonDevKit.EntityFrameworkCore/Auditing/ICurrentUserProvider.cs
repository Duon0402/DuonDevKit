namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Abstraction over "who is performing this operation", used by <see cref="AuditSaveChangesInterceptor"/>
    /// to populate audit fields. Implemented by the consuming application (e.g. wrapping
    /// <c>IHttpContextAccessor</c> in a web app) so this library never depends on ASP.NET Core directly.
    /// </summary>
    public interface ICurrentUserProvider
    {
        /// <summary>The id of the currently acting user, or <c>null</c> if there is none (e.g. a background job).</summary>
        string? UserId { get; }
    }
}
