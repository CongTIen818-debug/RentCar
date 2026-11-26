using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Application.Accounts;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;

namespace RentCar.Application.Auth;

public sealed record RegisterCustomerRequest(
    string FullName, DateOnly? BirthDate, string Phone, string Email, string IdNumber, string? Address,
    string LicenseNumber, string LicenseClass, DateOnly? LicenseExpiry, string Password, string ConfirmPassword);

/// <summary>UC-01 đăng nhập có khóa tạm (FR-6.4) và UC-05 đăng ký tài khoản khách hàng (FR-6.5).</summary>
public sealed class AuthService(IAppDbContext db, IPasswordHasher hasher)
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^0\d{9,10}$", RegexOptions.Compiled);
    private static readonly Regex IdPattern = new(@"^\d{9,12}$", RegexOptions.Compiled);

    public async Task<User> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        const string generic = "Email hoặc mật khẩu không đúng.";
        var normalized = (email ?? "").Trim().ToLower();

        var user = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null) throw new DomainException(generic, 401);

        if (!user.IsActive)
            throw new DomainException("Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.", 403);

        var now = VietnamTime.Now;
        if (user.LockedUntil is { } until && until > now)
        {
            var minutes = (int)Math.Ceiling((until - now).TotalMinutes);
            throw new DomainException($"Tài khoản đang tạm khóa do nhập sai nhiều lần. Thử lại sau {minutes} phút.", 423);
        }

        // Hết thời gian khóa tạm thì đặt lại bộ đếm, để một lần sai sau đó không khóa lại ngay (FR-6.4).
        if (user.LockedUntil is not null)
        {
            user.LockedUntil = null;
            user.FailedAttempts = 0;
        }

        if (!hasher.Verify(user.PasswordHash, password ?? ""))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedAttempts)
                user.LockedUntil = now.Add(LockDuration);
            await db.SaveChangesAsync(ct);
            throw new DomainException(generic, 401);
        }

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> RegisterCustomerAsync(RegisterCustomerRequest r, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var today = DateOnly.FromDateTime(now);
        var email = (r.Email ?? "").Trim().ToLower();
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(r.FullName)) errors["FullName"] = "Vui lòng nhập họ tên.";
        if (!EmailPattern.IsMatch(email)) errors["Email"] = "Email không đúng định dạng.";
        if (!PhonePattern.IsMatch((r.Phone ?? "").Trim())) errors["Phone"] = "Số điện thoại gồm 10 đến 11 chữ số, bắt đầu bằng 0.";
        if (!IdPattern.IsMatch((r.IdNumber ?? "").Trim())) errors["IdNumber"] = "Số giấy tờ tùy thân gồm 9 đến 12 chữ số (giả lập).";

        if (r.BirthDate is null) errors["BirthDate"] = "Vui lòng chọn ngày sinh.";
        else if (!BookingRules.IsOldEnough(r.BirthDate.Value, now)) errors["BirthDate"] = "Bạn cần đủ 21 tuổi để thuê xe (BR-12).";

        if (string.IsNullOrWhiteSpace(r.LicenseNumber)) errors["LicenseNumber"] = "Vui lòng nhập số GPLX.";
        if (string.IsNullOrWhiteSpace(r.LicenseClass)) errors["LicenseClass"] = "Vui lòng chọn hạng GPLX.";
        else if (!AccountRules.IsValidLicenseClass(r.LicenseClass)) errors["LicenseClass"] = "Hạng GPLX phải là B, C, D hoặc E.";
        if (r.LicenseExpiry is null) errors["LicenseExpiry"] = "Vui lòng nhập ngày hết hạn GPLX.";
        else if (r.LicenseExpiry.Value < today) errors["LicenseExpiry"] = "GPLX đã hết hạn nên không thể đăng ký (A4).";

        if (!IsStrongPassword(r.Password ?? ""))
            errors["Password"] = "Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường và chữ số (NFR-02).";
        else if (r.Password != r.ConfirmPassword)
            errors["ConfirmPassword"] = "Mật khẩu xác nhận không khớp.";

        if (errors.Count > 0) throw new FieldValidationException(errors);

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new FieldValidationException(new Dictionary<string, string>
            {
                ["Email"] = "Email đã được đăng ký. Hãy đăng nhập hoặc khôi phục mật khẩu."
            });

        var role = await db.Roles.FirstAsync(x => x.Code == RoleCodes.Customer, ct);
        var user = new User
        {
            FullName = r.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(r.Password!),
            BirthDate = r.BirthDate,
            Phone = r.Phone.Trim(),
            IdNumber = r.IdNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim(),
            IsActive = true,
            RoleId = role.Id,
            Role = role,
            DriverLicense = new DriverLicense
            {
                LicenseNumber = r.LicenseNumber.Trim(),
                LicenseClass = r.LicenseClass.Trim().ToUpperInvariant(),
                ExpiryDate = r.LicenseExpiry!.Value
            }
        };

        db.Users.Add(user);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = null,
            ObjectType = "NguoiDung",
            ObjectId = email,
            Action = "DANG_KY_KHACH_HANG",
            At = now
        });
        await db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>NFR-02: tối thiểu 8 ký tự, có chữ hoa, chữ thường và chữ số.</summary>
    public static bool IsStrongPassword(string p) =>
        p.Length >= 8 && p.Any(char.IsUpper) && p.Any(char.IsLower) && p.Any(char.IsDigit);
}
