using DuonDevKit.Core.Errors;
using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Jwt.Tests
{
    public class RefreshTokenServiceTests
    {
        [Fact]
        public async Task IssueAsync_ReturnsATokenAndPersistsIt()
        {
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());

            var result = await service.IssueAsync("user-1");

            Assert.True(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Value));
            Assert.Single(context.RefreshTokens.ToList());
            Assert.Equal("user-1", context.RefreshTokens.Single().UserId);
        }

        [Fact]
        public async Task IssueAsync_NeverPersistsTheRawTokenValue()
        {
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());

            var result = await service.IssueAsync("user-1");

            var stored = context.RefreshTokens.Single();
            Assert.NotEqual(result.Value, stored.TokenHash);
            Assert.DoesNotContain(result.Value, stored.TokenHash, StringComparison.Ordinal);
        }

        [Fact]
        public async Task RotateAsync_ValidToken_RevokesOldAndReturnsNewToken()
        {
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());
            var issued = await service.IssueAsync("user-1");

            var rotated = await service.RotateAsync(issued.Value);

            Assert.True(rotated.IsSuccess);
            Assert.Equal("user-1", rotated.Value.UserId);
            Assert.NotEqual(issued.Value, rotated.Value.NewRefreshToken);

            var tokens = context.RefreshTokens.ToList();
            Assert.Equal(2, tokens.Count);
            Assert.Single(tokens, t => t.IsRevoked);
            Assert.Single(tokens, t => !t.IsRevoked);
        }

        [Fact]
        public async Task RotateAsync_UnknownToken_ReturnsFailure()
        {
            var (_, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());

            var result = await service.RotateAsync("does-not-exist");

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task RotateAsync_AlreadyRotatedToken_ReturnsFailureInsteadOfIssuingAnotherToken()
        {
            var (_, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());
            var issued = await service.IssueAsync("user-1");
            await service.RotateAsync(issued.Value);

            var secondRotate = await service.RotateAsync(issued.Value);

            Assert.True(secondRotate.IsFailure);
        }

        [Fact]
        public async Task RotateAsync_ConcurrencyTokenConflict_LosingSaveFailsInsteadOfBothSucceeding()
        {
            // Exercises the mechanism RotateAsync's race-condition fix relies on directly: two separate
            // DbContexts (simulating two concurrent requests) both load the same not-yet-revoked row
            // before either saves — the exact interleaving a real race would produce — then both flip
            // IsRevoked and save. Because IsRevoked is configured as a concurrency token, the second save
            // must fail instead of silently succeeding (which would let both requests mint a valid child
            // token from the same parent).
            var settings = TestFactory.CreateSettings();
            var databaseName = Guid.NewGuid().ToString();
            var (context, service) = TestFactory.CreateRefreshTokenService(settings, databaseName: databaseName);
            var issued = await service.IssueAsync("user-1");

            var optionsA = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(databaseName).Options;
            var optionsB = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(databaseName).Options;
            using var contextA = new TestDbContext(optionsA);
            using var contextB = new TestDbContext(optionsB);

            var tokenA = await contextA.RefreshTokens.SingleAsync();
            var tokenB = await contextB.RefreshTokens.SingleAsync();
            tokenA.IsRevoked = true;
            tokenB.IsRevoked = true;

            var saveA = await new UnitOfWork(contextA).SaveChangesAsync();
            var saveB = await new UnitOfWork(contextB).SaveChangesAsync();

            Assert.True(saveA.IsSuccess);
            Assert.True(saveB.IsFailure);
            Assert.Equal(DuonDevKit.Core.Errors.ErrorType.Conflict, saveB.Error.Type);
        }

        [Fact]
        public async Task RotateAsync_ReuseOfAlreadyRotatedToken_RevokesEntireFamily()
        {
            // Simulates a stolen-token scenario: the legitimate client rotates A -> B -> C (three
            // generations), then someone replays the original token A. That replay must not just fail for
            // itself — it must revoke every descendant, including the currently-valid C, so the thief
            // (and the legitimate client, forcing a fresh login) can't use any token from this family
            // afterward.
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());
            var tokenA = await service.IssueAsync("user-1");
            var rotateToB = await service.RotateAsync(tokenA.Value);
            var rotateToC = await service.RotateAsync(rotateToB.Value.NewRefreshToken);
            Assert.True(rotateToC.IsSuccess);

            var replayA = await service.RotateAsync(tokenA.Value);

            Assert.True(replayA.IsFailure);
            var tokens = context.RefreshTokens.ToList();
            Assert.Equal(3, tokens.Count);
            Assert.All(tokens, t => Assert.True(t.IsRevoked));

            // The previously-valid C is now unusable too — cascade containment worked, not just a
            // single-token rejection.
            var attemptToUseC = await service.RotateAsync(rotateToC.Value.NewRefreshToken);
            Assert.True(attemptToUseC.IsFailure);
        }

        [Fact]
        public async Task RotateAsync_ReplayWhereFamilyRevokeSaveFails_ReturnsFailureInsteadOfSilentUnauthorized()
        {
            // If RevokeFamilyAsync's save fails for a genuine reason (not a benign concurrency conflict),
            // that failure must not be swallowed and reported as a plain Unauthorized — that would make
            // the caller (and any monitoring) believe the security containment succeeded when it didn't.
            var settings = TestFactory.CreateSettings();
            var (context, service) = TestFactory.CreateRefreshTokenService(settings);
            var root = await service.IssueAsync("user-1");
            await service.RotateAsync(root.Value); // root revoked, child issued

            var failure = Error.Unexpected("test.save-failed", "Simulated save failure.");
            var failingUnitOfWork = new AlwaysFailingSaveUnitOfWork(new UnitOfWork(context), failure);
            var replayService = new RefreshTokenService(
                new Repository<RefreshToken, string>(context), failingUnitOfWork, new JwtTokenGenerator(settings), settings);

            var replay = await replayService.RotateAsync(root.Value);

            Assert.True(replay.IsFailure);
            Assert.Equal(failure, replay.Error);
        }

        [Fact]
        public async Task RotateAsync_ReplayCascadeRevoke_SavesEachNotYetRevokedSiblingIndividually()
        {
            // RevokeFamilyAsync must save each not-yet-revoked sibling individually rather than batching
            // them into one SaveChangesAsync call: on a real relational provider SaveChanges is atomic, so
            // a concurrency conflict on ONE sibling (touched by someone else at that exact instant) would
            // abort the whole batch and leave every OTHER still-valid sibling un-revoked in the database —
            // exactly the gap a stolen token could exploit. Counting SaveChangesAsync invocations verifies
            // this directly, without depending on the InMemory provider's own non-atomic (and therefore
            // misleading) concurrency-conflict behavior.
            var settings = TestFactory.CreateSettings();
            var (context, service) = TestFactory.CreateRefreshTokenService(settings);

            var root = await service.IssueAsync("user-1");
            var familyId = context.RefreshTokens.Single().FamilyId;
            await service.RotateAsync(root.Value); // root revoked, "child" issued — one valid leaf so far

            // Seed a second, still-valid sibling in the same family so the cascade revoke has two rows.
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user-1",
                FamilyId = familyId,
                TokenHash = "sibling-hash",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
            });
            await context.SaveChangesAsync();

            var saveCount = 0;
            var instrumentedUnitOfWork = new InstrumentedUnitOfWork(new UnitOfWork(context), () => saveCount++);
            var replayService = new RefreshTokenService(
                new Repository<RefreshToken, string>(context), instrumentedUnitOfWork, new JwtTokenGenerator(settings), settings);

            var replay = await replayService.RotateAsync(root.Value);

            Assert.True(replay.IsFailure);
            Assert.Equal(2, saveCount); // one save per not-yet-revoked sibling (child + seeded sibling)
        }

        [Fact]
        public async Task RotateAsync_ExpiredToken_ReturnsFailure()
        {
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings(refreshTokenLifetime: TimeSpan.FromDays(-1)));
            var issued = await service.IssueAsync("user-1");

            var result = await service.RotateAsync(issued.Value);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task RevokeAsync_ExistingToken_MarksItRevoked()
        {
            var (context, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());
            var issued = await service.IssueAsync("user-1");

            var result = await service.RevokeAsync(issued.Value);

            Assert.True(result.IsSuccess);
            Assert.True(context.RefreshTokens.Single().IsRevoked);
        }

        [Fact]
        public async Task RevokeAsync_UnknownToken_ReturnsSuccessInsteadOfFailure()
        {
            var (_, service) = TestFactory.CreateRefreshTokenService(TestFactory.CreateSettings());

            var result = await service.RevokeAsync("does-not-exist");

            Assert.True(result.IsSuccess);
        }
    }
}
