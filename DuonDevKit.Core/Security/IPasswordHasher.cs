namespace DuonDevKit.Core.Security
{
    /// <summary>Hashes and verifies passwords for storage — never store a plain-text password.</summary>
    public interface IPasswordHasher
    {
        /// <summary>Returns a salted hash of <paramref name="password"/>, safe to persist.</summary>
        string Hash(string password);

        /// <summary>Returns <c>true</c> if <paramref name="password"/> matches <paramref name="hashedPassword"/> (as produced by <see cref="Hash"/>).</summary>
        bool Verify(string password, string hashedPassword);
    }
}
