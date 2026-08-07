using System.Security.Cryptography;

namespace DuonDevKit.Core.Security
{
    /// <summary>
    /// Default <see cref="IPasswordHasher"/> — PBKDF2-HMAC-SHA256 with a random salt per password,
    /// built entirely on <see cref="System.Security.Cryptography"/> (no third-party dependency). The
    /// hash is a single self-describing string (<c>{iterations}.{salt}.{hash}</c>, salt/hash Base64),
    /// so <see cref="Verify"/> never needs the iteration count passed in separately, and the iteration
    /// count can be raised in a later release without invalidating hashes already stored.
    /// </summary>
    public sealed class Pbkdf2PasswordHasher(int iterations = 100_000) : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;

        /// <inheritdoc />
        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <inheritdoc />
        public bool Verify(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var storedIterations))
                return false;

            byte[] salt, expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, storedIterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
