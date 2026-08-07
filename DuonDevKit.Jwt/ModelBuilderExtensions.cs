using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Jwt
{
    /// <summary>Extension methods for configuring <see cref="ModelBuilder"/> conventions for <see cref="RefreshToken"/>.</summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>Adds a unique index on <see cref="RefreshToken.Token"/> and a lookup index on <see cref="RefreshToken.UserId"/>. Call once in <c>OnModelCreating</c>, after adding a <c>DbSet&lt;RefreshToken&gt;</c> to your <c>DbContext</c>.</summary>
        public static void ConfigureDuonDevKitRefreshTokens(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(builder =>
            {
                builder.HasIndex(rt => rt.Token).IsUnique();
                builder.HasIndex(rt => rt.UserId);
            });
        }
    }
}
