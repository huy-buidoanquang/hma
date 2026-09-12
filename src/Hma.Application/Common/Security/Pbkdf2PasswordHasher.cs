using Hma.Application.Abstractions.Security;

namespace Hma.Application.Common.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        return $"pbkdf2:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string hash, string password)
    {
        try
        {
            var parts = hash.Split(':');
            if (parts.Length != 3 || parts[0] != "pbkdf2") return false;
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
