using System.Security.Cryptography;
using System.Text;
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
            var (entity, rawToken) = BuildNewToken(userId, familyId: Guid.NewGuid().ToString());

            var addResult = await repository.AddAsync(entity, ct);
            if (addResult.IsFailure)
                return Result.Fail<string>(addResult.Error);

            var saveResult = await unitOfWork.SaveChangesAsync(ct);
            return saveResult.IsFailure ? Result.Fail<string>(saveResult.Error) : Result.Success(rawToken);
        }

        /// <inheritdoc />
        public async Task<Result<RefreshTokenRotationResult>> RotateAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = Hash(refreshToken);

            var existing = await repository.FindOneAsync(rt => rt.TokenHash == tokenHash, ct: ct);
            if (!existing.HasValue)
                return Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token not found.");

            var current = existing.Value;

            // A rotated-away token being presented again is a signal it may have been stolen (the
            // legitimate client already moved on to its child token) — revoke the whole family instead of
            // just rejecting this one attempt, so a leaked-but-already-used token can't be replayed
            // indefinitely against whichever child token is currently valid.
            if (current.IsRevoked)
            {
                await RevokeFamilyAsync(current.FamilyId, ct);
                return Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token is revoked or expired.");
            }

            if (current.ExpiresAt <= DateTime.UtcNow)
                return Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token is revoked or expired.");

            current.IsRevoked = true;
            var (newToken, rawNewToken) = BuildNewToken(current.UserId, current.FamilyId);

            var addResult = await repository.AddAsync(newToken, ct);
            if (addResult.IsFailure)
                return Result.Fail<RefreshTokenRotationResult>(addResult.Error);

            // IsRevoked is configured as a concurrency token (see ConfigureDuonDevKitRefreshTokens), so
            // this save fails with a Conflict if another RotateAsync call already flipped it to true
            // since we loaded it above — closing the load-check-then-save race window between two
            // concurrent calls on the same token (which would otherwise both pass the check above and
            // each mint a valid child token from the same parent). Normalize that race loss to the same
            // Unauthorized outcome a sequential replay would get — from the caller's perspective it's the
            // same situation: this token was already rotated by the time the request was processed.
            var saveResult = await unitOfWork.SaveChangesAsync(ct);
            if (saveResult.IsFailure)
            {
                return saveResult.Error.Type == ErrorType.Conflict
                    ? Error.Unauthorized(ErrorCodes.InvalidRefreshToken, "Refresh token is revoked or expired.")
                    : Result.Fail<RefreshTokenRotationResult>(saveResult.Error);
            }

            return Result.Success(new RefreshTokenRotationResult(current.UserId, rawNewToken));
        }

        /// <summary>Revokes every not-yet-revoked token sharing <paramref name="familyId"/> — the containment action for a detected reuse attack.</summary>
        private async Task RevokeFamilyAsync(string familyId, CancellationToken ct)
        {
            var familyResult = await repository.ListAsync(rt => rt.FamilyId == familyId && !rt.IsRevoked, ct: ct);
            if (familyResult.IsFailure || familyResult.Value.Count == 0)
                return;

            foreach (var token in familyResult.Value)
                token.IsRevoked = true;

            await unitOfWork.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task<Result> RevokeAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = Hash(refreshToken);

            var existing = await repository.FindOneAsync(rt => rt.TokenHash == tokenHash, ct: ct);
            if (!existing.HasValue)
                return Result.Success();

            existing.Value.IsRevoked = true;
            return await unitOfWork.SaveChangesAsync(ct);
        }

        private (RefreshToken Entity, string RawToken) BuildNewToken(string userId, string familyId)
        {
            var rawToken = tokenGenerator.GenerateRefreshToken();
            var entity = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                FamilyId = familyId,
                TokenHash = Hash(rawToken),
                ExpiresAt = DateTime.UtcNow.Add(settings.RefreshTokenLifetime),
            };

            return (entity, rawToken);
        }

        /// <summary>
        /// Hashes a refresh token for storage/lookup. SHA-256 (no salt, no slow KDF) is sufficient here —
        /// unlike a password, the input already has 256 bits of generator-supplied entropy
        /// (<see cref="IJwtTokenGenerator.GenerateRefreshToken"/>), so it isn't guessable/brute-forceable
        /// the way a human-chosen password is; a fast general-purpose hash is enough to make a stolen
        /// database useless without also stealing the plaintext token that was returned to the client.
        /// </summary>
        private static string Hash(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
