using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;

namespace RentCar.Application.Accounts;

public sealed record StaffListItem(int Id, string FullName, string Email, string? Phone,
    string RoleCode, string RoleName, bool IsActive, DateTime? LockedUntil);

public sealed record StaffUpdateRequest(int Id, string FullName, string? Phone, string RoleCode);

/// <summary>
/// Must #3 (FR-6.3, UC-06): Quản trị viên xem, sửa, khóa, mở khóa và đổi vai trò tài khoản nhân viên.
/// Tài khoản nhân viên được tạo bằng mã đăng ký (StaffInvitationService), không do quản trị viên đặt mật khẩu.
/// Không cho khóa hoặc hạ quyền quản trị viên hoạt động cuối cùng.
/// </summary>
public sealed class StaffAccountService(IAppDbContext db, IPasswordHasher hasher)
{
    public async Task<IReadOnlyList<StaffListItem>> ListAsync(string? keyword, string? roleCode,
        bool? isActive, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking()
            .Where(u => AccountRules.StaffRoles.Contains(u.Role.Code));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(u => u.FullName.Contains(k) || u.Email.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(roleCode)) query = query.Where(u => u.Role.Code == roleCode);
        if (isActive is { } active) query = query.Where(u => u.IsActive == active);

        return await query.OrderBy(u => u.FullName)
            .Select(u => new StaffListItem(u.Id, u.FullName, u.Email, u.Phone,
                u.Role.Code, u.Role.Name, u.IsActive, u.LockedUntil))
            .ToListAsync(ct);
    }

    public async Task<StaffListItem?> GetAsync(int id, CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Id == id && AccountRules.StaffRoles.Contains(u.Role.Code))
            .Select(u => new StaffListItem(u.Id, u.FullName, u.Email, u.Phone,
                u.Role.Code, u.Role.Name, u.IsActive, u.LockedUntil))
            .FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(StaffUpdateRequest r, int actorId, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.FullName)) errors["FullName"] = "Vui lòng nhập họ tên.";
        if (!string.IsNullOrWhiteSpace(r.Phone) && !AccountRules.IsValidPhone(r.Phone))
            errors["Phone"] = "Số điện thoại gồm 10 đến 11 chữ số, bắt đầu bằng 0.";
        if (!AccountRules.StaffRoles.Contains(r.RoleCode)) errors["RoleCode"] = "Vai trò không hợp lệ.";
        if (errors.Count > 0) throw new FieldValidationException(errors);

        var user = await GetStaffAsync(r.Id, ct);
        var oldRole = user.Role.Code;

        if (oldRole == RoleCodes.Admin && r.RoleCode != RoleCodes.Admin && user.IsActive)
            await EnsureAnotherActiveAdminAsync(user.Id, ct);

        if (r.RoleCode != oldRole)
        {
            var role = await db.Roles.FirstOrDefaultAsync(x => x.Code == r.RoleCode, ct)
                ?? throw new DomainException("Vai trò chưa có trong CSDL.", 500);
            user.RoleId = role.Id;
            user.Role = role;
        }

        user.FullName = r.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ObjectType = "NguoiDung",
            ObjectId = user.Id.ToString(),
            Action = "CAP_NHAT_NHAN_VIEN",
            OldValue = $"vaiTro={oldRole}",
            NewValue = $"vaiTro={r.RoleCode}",
            At = VietnamTime.Now
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(int id, bool active, int actorId, CancellationToken ct = default)
    {
        var user = await GetStaffAsync(id, ct);
        if (user.IsActive == active) return;

        if (!active)
        {
            if (id == actorId)
                throw new DomainException("Bạn không thể tự khóa tài khoản đang đăng nhập.", 409);
            if (user.Role.Code == RoleCodes.Admin)
                await EnsureAnotherActiveAdminAsync(id, ct);
        }

        user.IsActive = active;
        if (active)
        {
            // Mở khóa thì xóa luôn bộ đếm sai và khóa tạm cũ.
            user.FailedAttempts = 0;
            user.LockedUntil = null;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ObjectType = "NguoiDung",
            ObjectId = id.ToString(),
            Action = active ? "MO_KHOA_TAI_KHOAN" : "KHOA_TAI_KHOAN",
            At = VietnamTime.Now
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<User> GetStaffAsync(int id, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && AccountRules.StaffRoles.Contains(u.Role.Code), ct);
        return user ?? throw new DomainException("Không tìm thấy tài khoản nhân viên.", 404);
    }

    /// <summary>Phải còn ít nhất một quản trị viên đang hoạt động ngoài người đang bị thao tác.</summary>
    private async Task EnsureAnotherActiveAdminAsync(int userId, CancellationToken ct)
    {
        var others = await db.Users.CountAsync(
            u => u.Id != userId && u.IsActive && u.Role.Code == RoleCodes.Admin, ct);
        if (others == 0)
            throw new DomainException(
                "Phải còn ít nhất một quản trị viên đang hoạt động, không thể thực hiện thao tác này (FR-6.3).", 409);
    }
}
