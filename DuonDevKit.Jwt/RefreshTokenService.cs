using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Repositories;

namespace DuonDevKit.Jwt
{
    /// <summary>Default <see cref="IRefreshTokenService"/>, backed by <see cref="IRepository{T, TId}"/>/<see cref="IUnitOfWork"/>.</summary>
    public sealed class RefreshTokenService(
        IRepository<RefreshToken, string> repository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator tokenGenerator,
        JwtSettings settings) : IRefreshTokenService
    {
        /// <inheritdoc />
        public async Task<Result<string>> IssueAsync(string userId, CancellationToken ct = default)
        {
            var entity = BuildNewToken(userId);

            var addResult = await repository.AddAsync(entity, ct);
            if (addResult.IsFailure)
                return Result.Fail<string>(addResult.Error);

            var saveResult = await unitOfWork.SaveChangesAsync(ct);
            return saveResult.IsFailure ? Result.Fail<string>(saveResult.Error) : Result.Success(entity.Token);
        }

        /// <inheritdoc />
        public async Task<Result<RefreshTokenRotationResult>> RotateAsync(string refreshToken, CancellationToken ct = default)
        {
            var existing = await repository.FindOneAsync(rt => rt.Token == refreshToken, ct: ct);
            if (!existing.HasValue)
                return Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token not found.");

            var current = existing.Value;
            if (current.IsRevoked || current.ExpiresAt <= DateTime.UtcNow)
                return Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token is revoked or expired.");

            current.IsRevoked = true;
            var newToken = BuildNewToken(current.UserId);

            var addResult = await repository.AddAsync(newToken, ct);
            if (addResult.IsFailure)
                return Result.Fail<RefreshTokenRotationResult>(addResult.Error);

            var saveResult = await unitOfWork.SaveChangesAsync(ct);
            if (saveResult.IsFailure)
                return Result.Fail<RefreshTokenRotationResult>(saveResult.Error);

            return Result.Success(new RefreshTokenRotationResult(current.UserId, newToken.Token));
        }

        /// <inheritdoc />
        public async Task<Result> RevokeAsync(string refreshToken, CancellationToken ct = default)
        {
            var existing = await repository.FindOneAsync(rt => rt.Token == refreshToken, ct: ct);
            if (!existing.HasValue)
                return Result.Success();

            existing.Value.IsRevoked = true;
            return await unitOfWork.SaveChangesAsync(ct);
        }

        private RefreshToken BuildNewToken(string userId) => new()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = tokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.Add(settings.RefreshTokenLifetime),
        };
    }
}
