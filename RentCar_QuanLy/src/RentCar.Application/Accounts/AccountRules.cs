using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RentCar.Application.Auth;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;

namespace RentCar.Application.Accounts;

/// <summary>Quy tắc dùng chung cho tài khoản nhân viên, mật khẩu, mã dùng một lần và hồ sơ khách hàng.</summary>
public static class AccountRules
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^0\d{9,10}$", RegexOptions.Compiled);

    /// <summary>Các vai trò nhân viên mà Quản trị viên được cấp (không gồm khách hàng).</summary>
    public static readonly string[] StaffRoles = [RoleCodes.Admin, RoleCodes.Sales, RoleCodes.Handover, RoleCodes.Accounting];

    /// <summary>Mã đặt lại mật khẩu có hiệu lực 30 phút (FR-6.1c).</summary>
    public const int ResetCodeMinutes = 30;

    /// <summary>Mã đăng ký nhân viên có hiệu lực 7 ngày kể từ lúc cấp (v4).</summary>
    public const int InvitationDays = 7;

    // Bỏ các ký tự dễ nhầm (0, 1, I, O) để người dùng nhập đúng mã.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 12;

    public static bool IsValidEmail(string? value) => EmailPattern.IsMatch((value ?? "").Trim().ToLowerInvariant());

    public static bool IsValidPhone(string? value) => PhonePattern.IsMatch((value ?? "").Trim());

    public static bool IsStrongPassword(string? value) => AuthService.IsStrongPassword(value ?? "");

    public static bool IsValidLicenseClass(string? value) =>
        BookingRules.LicenseClasses.Contains((value ?? "").Trim().ToUpperInvariant());

    /// <summary>Sinh mã ngẫu nhiên bằng RNG mật mã học: 12 ký tự, dùng cho mã đặt lại và mã đăng ký nhân viên.</summary>
    public static string NewCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return new string(chars);
    }

    /// <summary>Hiển thị mã theo nhóm 4 ký tự, ví dụ ABCD-EFGH-JKLM.</summary>
    public static string FormatCode(string code)
    {
        var raw = NormalizeCode(code);
        var parts = new List<string>();
        for (var i = 0; i < raw.Length; i += 4)
            parts.Add(raw.Substring(i, Math.Min(4, raw.Length - i)));
        return string.Join("-", parts);
    }

    /// <summary>Chuẩn hóa mã người dùng nhập: bỏ dấu gạch, khoảng trắng và đổi thành chữ hoa.</summary>
    public static string NormalizeCode(string? input) =>
        new string((input ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>Băm SHA-256 của mã đã chuẩn hóa. Mã thô không bao giờ được lưu trong CSDL.</summary>
    public static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeCode(code))));

    /// <summary>So sánh băm theo thời gian hằng số để tránh rò rỉ thông tin qua thời gian phản hồi.</summary>
    public static bool CodeMatches(string storedHash, string code) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash), Encoding.UTF8.GetBytes(HashCode(code)));
}
