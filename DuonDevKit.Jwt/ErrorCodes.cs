namespace DuonDevKit.Jwt
{
    /// <summary>Error codes used by <see cref="DuonDevKit.Core.Errors.Error"/> instances raised within this library.</summary>
    public static class ErrorCodes
    {
        /// <summary>A refresh token passed to <see cref="IRefreshTokenService.RotateAsync"/>/<see cref="IRefreshTokenService.RevokeAsync"/> doesn't exist, is revoked, or has expired.</summary>
        public const string InvalidRefreshToken = "JWT001";
    }
}
