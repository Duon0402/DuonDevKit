using DuonDevKit.Core.Results;

namespace DuonDevKit.Jwt
{
    /// <summary>The outcome of a successful <see cref="IRefreshTokenService.RotateAsync"/> call.</summary>
    public sealed record RefreshTokenRotationResult(string UserId, string NewRefreshToken);

    /// <summary>Issues, rotates, and revokes persisted <see cref="RefreshToken"/>s.</summary>
    public interface IRefreshTokenService
    {
        /// <summary>Issues and persists a new refresh token for <paramref name="userId"/>.</summary>
        Task<Result<string>> IssueAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// Validates <paramref name="refreshToken"/>, revokes it, and issues a new one for the same user —
        /// fails with <c>Error.Unauthorized</c> if the token doesn't exist, is already revoked, or has expired.
        /// </summary>
        Task<Result<RefreshTokenRotationResult>> RotateAsync(string refreshToken, CancellationToken ct = default);

        /// <summary>Revokes <paramref name="refreshToken"/> if it exists (a no-op, not a failure, if it doesn't — the caller's goal is already satisfied).</summary>
        Task<Result> RevokeAsync(string refreshToken, CancellationToken ct = default);
    }
}
