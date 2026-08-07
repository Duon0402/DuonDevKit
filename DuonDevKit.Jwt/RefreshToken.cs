using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Auditing;

namespace DuonDevKit.Jwt
{
    /// <summary>A persisted refresh token. Add a <c>DbSet&lt;RefreshToken&gt;</c> to your <c>DbContext</c> and call <c>modelBuilder.ConfigureDuonDevKitRefreshTokens()</c> in <c>OnModelCreating</c>.</summary>
    public class RefreshToken : BaseEntity<string>, ICanCreate
    {
        /// <summary>The id of the user this token was issued for.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>The opaque token value, as returned to the caller.</summary>
        public string Token { get; set; } = string.Empty;

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
