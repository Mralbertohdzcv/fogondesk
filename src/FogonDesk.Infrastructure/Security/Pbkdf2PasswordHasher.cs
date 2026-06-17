using System;
using System.Security.Cryptography;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Security
{
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 120000;

        public PasswordHashResult HashPassword(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
            {
                throw new ArgumentException("La contraseña es obligatoria.", nameof(plainTextPassword));
            }

            var salt = new byte[SaltSize];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(salt);
            }

            var hash = CreateHash(plainTextPassword, salt);
            return new PasswordHashResult
            {
                SaltBase64 = Convert.ToBase64String(salt),
                HashBase64 = Convert.ToBase64String(hash)
            };
        }

        public bool Verify(string plainTextPassword, string saltBase64, string hashBase64)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || string.IsNullOrWhiteSpace(saltBase64) || string.IsNullOrWhiteSpace(hashBase64))
            {
                return false;
            }

            var salt = Convert.FromBase64String(saltBase64);
            var expectedHash = Convert.FromBase64String(hashBase64);
            var actualHash = CreateHash(plainTextPassword, salt);
            return AreEqual(expectedHash, actualHash);
        }

        private static byte[] CreateHash(string plainTextPassword, byte[] salt)
        {
            using (var derivation = new Rfc2898DeriveBytes(plainTextPassword, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return derivation.GetBytes(HashSize);
            }
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }
    }
}
