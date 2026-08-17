namespace DuonDevKit.Caching.Tests
{
    public class CachingSettingsTests
    {
        [Fact]
        public void DefaultExpiration_NotSet_IsFiveMinutes()
        {
            var settings = new CachingSettings();

            Assert.Equal(TimeSpan.FromMinutes(5), settings.DefaultExpiration);
        }

        [Fact]
        public void RedisConnectionString_NotSet_IsNull()
        {
            var settings = new CachingSettings();

            Assert.Null(settings.RedisConnectionString);
        }
    }
}