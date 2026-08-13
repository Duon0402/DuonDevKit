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
    public sealed class Pbkdf2PasswordHasher(int iterations = 600_000) : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;

        /// <summary>Upper bound on the iteration count read back from a stored hash string, so a corrupted/tampered value can't force a multi-minute PBKDF2 run.</summary>
        private const int MaxIterations = 2_000_000;

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
            ArgumentNullException.ThrowIfNull(password);

            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            var parts = hashedPassword.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var storedIterations) || storedIterations is <= 0 or > MaxIterations)
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

            if (salt.Length == 0 || expectedHash.Length == 0)
                return false;

            byte[] actualHash;
            try
            {
                actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, storedIterations, HashAlgorithmName.SHA256, expectedHash.Length);
            }
            catch (Exception)
            {
                // Any other malformed-input failure from a corrupted/hand-edited hash string (e.g. an
                // absurdly large iteration count) — Verify's contract is "never throw", always fall through
                // to false instead.
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
