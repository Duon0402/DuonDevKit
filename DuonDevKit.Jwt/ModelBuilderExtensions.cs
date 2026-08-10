using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Jwt
{
    /// <summary>Extension methods for configuring <see cref="ModelBuilder"/> conventions for <see cref="RefreshToken"/>.</summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Adds a unique index on <see cref="RefreshToken.TokenHash"/>, a lookup index on
        /// <see cref="RefreshToken.UserId"/>, and marks <see cref="RefreshToken.IsRevoked"/> as a
        /// concurrency token — so two concurrent rotations of the same token can't both succeed (the
        /// second save fails with a concurrency conflict instead of silently minting a second valid
        /// child token). Call once in <c>OnModelCreating</c>, after adding a <c>DbSet&lt;RefreshToken&gt;</c>
        /// to your <c>DbContext</c>.
        /// </summary>
        public static void ConfigureDuonDevKitRefreshTokens(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(builder =>
            {
                builder.HasIndex(rt => rt.TokenHash).IsUnique();
                builder.HasIndex(rt => rt.UserId);
                builder.HasIndex(rt => rt.FamilyId);
                builder.Property(rt => rt.IsRevoked).IsConcurrencyToken();
            });
        }
    }
}
