using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;

namespace RentCar.Application.Accounts;

public sealed record ProfileData(string FullName, string Email, string Phone, DateOnly? BirthDate,
    string? IdNumber, string? Address, string? LicenseNumber, string? LicenseClass, DateOnly? LicenseExpiry);

public sealed record ProfileUpdateRequest(string FullName, string Phone, string? Address,
    string LicenseNumber, string LicenseClass, DateOnly? LicenseExpiry);

/// <summary>
/// Must #5 (FR-6.2a): khách cập nhật họ tên, số điện thoại, địa chỉ và GPLX.
/// Số giấy tờ tùy thân chỉ xem, không sửa tại đây.
/// </summary>
public sealed class ProfileService(IAppDbContext db)
{
    public async Task<ProfileData> GetAsync(int userId, CancellationToken ct = default)
    {
        var u = await db.Users.AsNoTracking().Include(x => x.DriverLicense)
            .FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new DomainException("Không tìm thấy hồ sơ.", 404);
        return new ProfileData(u.FullName, u.Email, u.Phone ?? "", u.BirthDate, u.IdNumber, u.Address,
            u.DriverLicense?.LicenseNumber, u.DriverLicense?.LicenseClass, u.DriverLicense?.ExpiryDate);
    }

    public async Task UpdateAsync(int userId, ProfileUpdateRequest r, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(VietnamTime.Now);
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(r.FullName)) errors["FullName"] = "Vui lòng nhập họ tên.";
        if (!AccountRules.IsValidPhone(r.Phone)) errors["Phone"] = "Số điện thoại gồm 10 đến 11 chữ số, bắt đầu bằng 0.";
        if (string.IsNullOrWhiteSpace(r.LicenseNumber)) errors["LicenseNumber"] = "Vui lòng nhập số GPLX.";
        else if (r.LicenseNumber.Trim().Length > 30) errors["LicenseNumber"] = "Số GPLX tối đa 30 ký tự.";
        if (!AccountRules.IsValidLicenseClass(r.LicenseClass)) errors["LicenseClass"] = "Hạng GPLX phải là B, C, D hoặc E.";
        if (r.LicenseExpiry is null) errors["LicenseExpiry"] = "Vui lòng nhập ngày hết hạn GPLX.";
        else if (r.LicenseExpiry.Value < today) errors["LicenseExpiry"] = "GPLX đã hết hạn, vui lòng nhập GPLX còn hiệu lực.";
        if (errors.Count > 0) throw new FieldValidationException(errors);

        var licenseNumber = r.LicenseNumber.Trim().ToUpperInvariant();
        var licenseClass = r.LicenseClass.Trim().ToUpperInvariant();

        var user = await db.Users.Include(x => x.Role).Include(x => x.DriverLicense)
            .FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new DomainException("Không tìm thấy hồ sơ.", 404);
        if (user.Role.Code != RoleCodes.Customer)
            throw new DomainException("Chỉ tài khoản khách hàng mới cập nhật GPLX tại đây.", 403);

        if (await db.DriverLicenses.AnyAsync(x => x.LicenseNumber == licenseNumber && x.UserId != userId, ct))
            throw new FieldValidationException(new Dictionary<string, string>
            {
                ["LicenseNumber"] = "Số GPLX này đã được đăng ký bởi tài khoản khác."
            });

        user.FullName = r.FullName.Trim();
        user.Phone = r.Phone.Trim();
        user.Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim();

        if (user.DriverLicense is null)
        {
            user.DriverLicense = new DriverLicense
            {
                LicenseNumber = licenseNumber,
                LicenseClass = licenseClass,
                ExpiryDate = r.LicenseExpiry!.Value
            };
        }
        else
        {
            user.DriverLicense.LicenseNumber = licenseNumber;
            user.DriverLicense.LicenseClass = licenseClass;
            user.DriverLicense.ExpiryDate = r.LicenseExpiry!.Value;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = userId,
            ObjectType = "NguoiDung",
            ObjectId = userId.ToString(),
            Action = "CAP_NHAT_HO_SO",
            At = VietnamTime.Now
        });
        await db.SaveChangesAsync(ct);
    }
}
