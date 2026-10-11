using JobMatchAI.Application.Authentication.Interfaces;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace JobMatchAI.Application.Authentication.Services
{
    public class Argon2PasswordHasher(ILogger<Argon2PasswordHasher> logger) : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 2;
        private const int MemorySize = 19456;
        private const int Parallelism = 1;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = Parallelism,
                Iterations = Iterations,
                MemorySize = MemorySize
            };

            var hash = argon2.GetBytes(HashSize);

            var version = new byte[] { 0x01 };
            var combined = new byte[1 + SaltSize + HashSize];

            Buffer.BlockCopy(version, 0, combined, 0, 1);
            Buffer.BlockCopy(salt, 0, combined, 1, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, 1 + SaltSize, HashSize);

            return Convert.ToBase64String(combined);
        }

        public bool Verify(string password, string storedHash)
        {
            byte[] combined;
            try
            {
                combined = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            if (combined.Length < 1 || combined[0] != 0x01)
            {
                logger.LogWarning("Legacy or unknown hash format detected.");
                return false;
            }

            if (combined.Length != 1 + SaltSize + HashSize) return false;

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(combined, 1, salt, 0, SaltSize);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = Parallelism,
                Iterations = Iterations,
                MemorySize = MemorySize
            };

            var computedHash = argon2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(
                computedHash, combined.AsSpan(1 + SaltSize, HashSize));
        }
    }
}
