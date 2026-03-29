using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.API.Services
{
    public static class PasswordHasherHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split('.', 3, StringSplitOptions.None);
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out var iterations)) return false;
            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
    }
}
