using Microsoft.AspNetCore.Identity;
using RentCar.Application.Abstractions;

namespace RentCar.Infrastructure.Services;

/// <summary>
/// Băm mật khẩu bằng PasswordHasher của ASP.NET Core Identity (PBKDF2, NFR-02).
/// Định dạng này khớp với chuỗi băm đang có trong cột nguoiDung.matKhau của CSDL mẫu.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();

    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public bool Verify(string hashedPassword, string password) =>
        Hasher.VerifyHashedPassword(null!, hashedPassword, password) != PasswordVerificationResult.Failed;
}
