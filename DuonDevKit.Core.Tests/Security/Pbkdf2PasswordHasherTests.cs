using DuonDevKit.Core.Security;

namespace DuonDevKit.Core.Tests.Security
{
    public class Pbkdf2PasswordHasherTests
    {
        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var hasher = new Pbkdf2PasswordHasher(iterations: 1000);
            var hash = hasher.Hash("correct-horse-battery-staple");

            Assert.True(hasher.Verify("correct-horse-battery-staple", hash));
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hasher = new Pbkdf2PasswordHasher(iterations: 1000);
            var hash = hasher.Hash("correct-horse-battery-staple");

            Assert.False(hasher.Verify("wrong-password", hash));
        }

        [Fact]
        public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        {
            var hasher = new Pbkdf2PasswordHasher(iterations: 1000);

            var first = hasher.Hash("same-password");
            var second = hasher.Hash("same-password");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing()
        {
            var hasher = new Pbkdf2PasswordHasher();

            Assert.False(hasher.Verify("anything", "not-a-valid-hash"));
        }

        [Fact]
        public void Verify_HashProducedWithDifferentIterationCount_StillVerifies()
        {
            var oldHasher = new Pbkdf2PasswordHasher(iterations: 1000);
            var hash = oldHasher.Hash("password");

            var newHasher = new Pbkdf2PasswordHasher(iterations: 5000);
            Assert.True(newHasher.Verify("password", hash));
        }
    }
}
