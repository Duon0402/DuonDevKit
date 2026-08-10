using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Auditing;

namespace DuonDevKit.Jwt
{
    /// <summary>A persisted refresh token. Add a <c>DbSet&lt;RefreshToken&gt;</c> to your <c>DbContext</c> and call <c>modelBuilder.ConfigureDuonDevKitRefreshTokens()</c> in <c>OnModelCreating</c>.</summary>
    public class RefreshToken : BaseEntity<string>, ICanCreate
    {
        /// <summary>The id of the user this token was issued for.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Groups a token with every token descended from it through <see cref="IRefreshTokenService.RotateAsync"/>
        /// rotation — the original <see cref="IRefreshTokenService.IssueAsync"/> call starts a new family
        /// (own <see cref="Id"/>), and every rotation carries the parent's <see cref="FamilyId"/> forward.
        /// Used to revoke the whole chain at once when a rotated-away token is presented again (reuse —
        /// a signal the token may have been stolen), instead of only rejecting that one replay attempt.
        /// </summary>
        public string FamilyId { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hash of the opaque token value returned to the caller — never the raw token itself,
        /// so a database leak doesn't hand out directly-usable refresh tokens (same principle as password
        /// hashing). Looked up by hashing the incoming token and comparing hashes.
        /// </summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>When this token stops being valid.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Whether this token has been rotated or explicitly revoked and can no longer be used.</summary>
        public bool IsRevoked { get; set; }

        /// <inheritdoc />
        public DateTime CreatedAt { get; set; }

        /// <inheritdoc />
        public string? CreatedBy { get; set; }
    }
}
