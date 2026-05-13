using System.Security.Cryptography;

namespace ClinicManagement.Services;

public class PasswordHashService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "pbkdf2";

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Mật khẩu không được để trống.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return false;
        }

        if (!IsHashed(storedValue))
        {
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(storedValue),
                System.Text.Encoding.UTF8.GetBytes(password));
        }

        var parts = storedValue.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool NeedsUpgrade(string storedValue) => !IsHashed(storedValue);

    private static bool IsHashed(string storedValue) => storedValue.StartsWith($"{Prefix}$", StringComparison.Ordinal);
}
