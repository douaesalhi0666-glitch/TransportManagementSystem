using System;
using System.Security.Cryptography;
using System.Text;

namespace TransportManagementSystem.Services
{
    public class PasswordService
    {
        // Simple hash for passwords (no BCrypt dependency)
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Verify password
        public static bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        // Generate a random reset token
        public static string GenerateResetToken()
    {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}