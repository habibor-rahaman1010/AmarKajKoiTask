using AmarKajKoi.ServicesInterface;
using System.Security.Cryptography;
using System.Text;

namespace AmarKajKoi.ServicesImplement
{
    // Simple PBKDF2 hasher — avoids external dependency.
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;
        private const string Marker = "PBKDF2$";

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var derived = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iterations,
                HashAlgorithmName.SHA256, KeySize);

            return $"{Marker}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";
        }

        public bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            // Support seed placeholder "PLAIN:xxxx" — accept once and treat as valid.
            if (storedHash.StartsWith("PLAIN:", StringComparison.Ordinal))
            {
                var plain = storedHash.Substring("PLAIN:".Length);
                return string.Equals(plain, password, StringComparison.Ordinal);
            }

            if (!storedHash.StartsWith(Marker, StringComparison.Ordinal))
            {
                return false;
            }
            var parts = storedHash.Substring(Marker.Length).Split('$');
            if (parts.Length != 3)
            {
                return false;
            }
            if (!int.TryParse(parts[0], out var iters))
            {
                return false;
            }

            var salt    = Convert.FromBase64String(parts[1]);
            var stored  = Convert.FromBase64String(parts[2]);
            var derived = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iters,
                HashAlgorithmName.SHA256, stored.Length);
            return CryptographicOperations.FixedTimeEquals(derived, stored);
        }
    }
}