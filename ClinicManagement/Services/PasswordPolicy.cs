using System.Security.Cryptography;

namespace ClinicManagement.Services;

public static class PasswordPolicy
{
    private const int MinimumLength = 10;
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string DigitChars = "23456789";
    private const string SymbolChars = "@$!%*?&";
    private static readonly string[] CommonWeakPasswords =
    [
        "123456",
        "12345678",
        "123456789",
        "1234567890",
        "password",
        "password1",
        "password123",
        "admin",
        "admin123",
        "letan",
        "bacsi",
        "clinic",
        "clinic123",
        "qwerty",
        "qwerty123",
        "abc123",
        "111111",
        "000000"
    ];

    public static string GenerateTemporaryPassword(int length = 14)
    {
        if (length < MinimumLength)
        {
            length = MinimumLength;
        }

        var chars = new List<char>
        {
            Pick(LowercaseChars),
            Pick(UppercaseChars),
            Pick(DigitChars),
            Pick(SymbolChars)
        };

        var allChars = LowercaseChars + UppercaseChars + DigitChars + SymbolChars;
        while (chars.Count < length)
        {
            chars.Add(Pick(allChars));
        }

        Shuffle(chars);
        return new string(chars.ToArray());
    }

    public static string? ValidateNewPassword(string newPassword, string currentPassword, string? username = null)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return "Mật khẩu mới không được để trống.";
        }

        if (newPassword == currentPassword)
        {
            return "Mật khẩu mới phải khác mật khẩu hiện tại.";
        }

        if (newPassword.Length < MinimumLength)
        {
            return $"Mật khẩu mới phải có ít nhất {MinimumLength} ký tự.";
        }

        if (newPassword.Any(char.IsWhiteSpace))
        {
            return "Mật khẩu mới không được chứa khoảng trắng.";
        }

        if (!newPassword.Any(char.IsLower))
        {
            return "Mật khẩu mới cần có ít nhất 1 chữ thường.";
        }

        if (!newPassword.Any(char.IsUpper))
        {
            return "Mật khẩu mới cần có ít nhất 1 chữ hoa.";
        }

        if (!newPassword.Any(char.IsDigit))
        {
            return "Mật khẩu mới cần có ít nhất 1 chữ số.";
        }

        if (!newPassword.Any(ch => SymbolChars.Contains(ch)))
        {
            return "Mật khẩu mới cần có ít nhất 1 ký tự đặc biệt: @$!%*?&.";
        }

        var normalizedPassword = newPassword.Trim().ToLowerInvariant();
        if (CommonWeakPasswords.Contains(normalizedPassword))
        {
            return "Mật khẩu mới quá phổ biến hoặc kém an toàn. Vui lòng chọn mật khẩu khác.";
        }

        if (!string.IsNullOrWhiteSpace(username) &&
            normalizedPassword.Contains(username.Trim().ToLowerInvariant()))
        {
            return "Mật khẩu mới không nên chứa tên đăng nhập.";
        }

        return null;
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];

    private static void Shuffle(IList<char> chars)
    {
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
