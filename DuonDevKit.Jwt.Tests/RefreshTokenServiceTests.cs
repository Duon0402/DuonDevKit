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
            Assert.True(tokens.Single(t => t.Token == issued.Value).IsRevoked);
            Assert.False(tokens.Single(t => t.Token == rotated.Value.NewRefreshToken).IsRevoked);
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
