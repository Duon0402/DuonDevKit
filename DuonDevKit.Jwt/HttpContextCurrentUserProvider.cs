using System.Security.Claims;
using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.AspNetCore.Http;

namespace DuonDevKit.Jwt
{
    /// <summary>
    /// <see cref="ICurrentUserProvider"/> reading the acting user's id from the current request's JWT claims
    /// (<see cref="ClaimTypes.NameIdentifier"/>), so <c>AuditSaveChangesInterceptor</c> attributes changes to
    /// whoever the access token identifies. <see cref="UserId"/> is <c>null</c> outside a request (e.g. a
    /// background job) or for an unauthenticated request.
    /// </summary>
    public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
    {
        /// <inheritdoc />
        public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
