using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;

namespace RentCar.Application.Accounts;

/// <summary>Trạng thái hiển thị của một mã đăng ký nhân viên.</summary>
public enum InvitationStatus { Pending, Used, Expired, Revoked }

/// <summary>Một dòng trong danh sách mã đăng ký. Không chứa mã thô hay bản băm.</summary>
public sealed record StaffInvitationItem(int Id, string Email, string FullName, string RoleName, string RoleCode,
    DateTime IssuedAt, DateTime ExpiresAt, DateTime? UsedAt, string IssuedByName, InvitationStatus Status);

/// <summary>Kết quả cấp mã. Code là mã thô, chỉ được hiện cho quản trị viên đúng một lần.</summary>
public sealed record IssuedStaffCode(string Code, string Email, string FullName, string RoleName, DateTime ExpiresAt);

/// <summary>Dữ liệu nhân viên gửi lên khi tự đăng ký bằng mã. Họ tên và vai trò lấy từ mã, không nhập lại.</summary>
public sealed record StaffRedeemRequest(string Code, string Email, string? Phone, string Password, string ConfirmPassword);

/// <summary>Một dòng thô đọc từ CSDL, trước khi tính trạng thái.</summary>
public sealed record StaffInvitationRow(int Id, string Email, string FullName, string RoleName, string RoleCode,
    DateTime IssuedAt, DateTime ExpiresAt, DateTime? UsedAt, bool IsRevoked, string IssuedByName);

/// <summary>
/// v4 (thay cho việc quản trị viên đặt mật khẩu ban đầu): quản trị viên cấp mã đăng ký cho nhân viên;
/// nhân viên dùng mã để tự tạo tài khoản. Mã dùng một lần, gắn với một email và một vai trò,
/// hết hạn sau 7 ngày, chỉ lưu bản băm SHA-256.
/// </summary>
public sealed class StaffInvitationService(IAppDbContext db, IPasswordHasher hasher)
{
    /// <summary>Một thông báo chung cho mọi lỗi về mã, để không tiết lộ mã nào đã dùng, bị thu hồi hay tồn tại.</summary>
    private const string CodeRejected =
        "Mã đăng ký không đúng, đã dùng, đã thu hồi hoặc hết hạn, hoặc email không khớp với mã. " +
        "Hãy liên hệ quản trị viên để được cấp mã mới.";

    public async Task<IReadOnlyList<StaffInvitationItem>> ListAsync(string? state, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var rows = await db.StaffInvitations.AsNoTracking()
            .OrderByDescending(x => x.IssuedAt).ThenByDescending(x => x.Id)
            .Select(x => new StaffInvitationRow(x.Id, x.Email, x.FullName, x.Role.Name, x.Role.Code,
                x.IssuedAt, x.ExpiresAt, x.UsedAt, x.IsRevoked, x.IssuedBy.FullName))
            .ToListAsync(ct);

        var items = rows.Select(r => new StaffInvitationItem(r.Id, r.Email, r.FullName, r.RoleName, r.RoleCode,
            r.IssuedAt, r.ExpiresAt, r.UsedAt, r.IssuedByName,
            StatusOf(r.UsedAt, r.IsRevoked, r.ExpiresAt, now))).ToList();

        if (Enum.TryParse<InvitationStatus>(state, ignoreCase: true, out var wanted))
            items = items.Where(i => i.Status == wanted).ToList();
        return items;
    }

    /// <summary>Cấp mã cho một nhân viên. Mã thô chỉ được trả về một lần; CSDL chỉ lưu bản băm.</summary>
    public async Task<IssuedStaffCode> IssueAsync(string email, string fullName, string roleCode, int adminId,
        CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var normalizedEmail = (email ?? "").Trim().ToLowerInvariant();
        var name = (fullName ?? "").Trim();

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name)) errors["FullName"] = "Vui lòng nhập họ tên nhân viên.";
        else if (name.Length > 100) errors["FullName"] = "Họ tên tối đa 100 ký tự.";
        if (!AccountRules.IsValidEmail(normalizedEmail)) errors["Email"] = "Email không đúng định dạng.";
        if (!AccountRules.StaffRoles.Contains(roleCode)) errors["RoleCode"] = "Vai trò không hợp lệ.";
        if (errors.Count > 0) throw new FieldValidationException(errors);

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct))
            throw new FieldValidationException(new Dictionary<string, string>
            {
                ["Email"] = "Email này đã có tài khoản. Hãy dùng email khác."
            });

        var role = await db.Roles.FirstOrDefaultAsync(x => x.Code == roleCode, ct)
            ?? throw new DomainException("Vai trò chưa có trong CSDL. Hãy chạy script database trước.", 500);

        await using var tx = await db.BeginTransactionAsync(ct);

        // Mỗi email chỉ có một mã chưa dùng. Mã cũ còn hạn thì chặn việc cấp mã mới;
        // mã cũ đã hết hạn thì tự thu hồi để cho phép cấp mã mới.
        var unused = await db.StaffInvitations
            .Where(x => x.Email == normalizedEmail && x.UsedAt == null && !x.IsRevoked)
            .ToListAsync(ct);
        foreach (var old in unused)
        {
            if (old.ExpiresAt >= now)
                throw new FieldValidationException(new Dictionary<string, string>
                {
                    ["Email"] = "Email này đang có mã chưa dùng. Hãy thu hồi mã đó trước khi cấp mã mới."
                });

            old.IsRevoked = true;
            db.AuditLogs.Add(new AuditLog
            {
                ActorId = adminId,
                ObjectType = "DangKyNhanVien",
                ObjectId = old.Id.ToString(),
                Action = "THU_HOI_MA_NHAN_VIEN",
                NewValue = "lyDo=het-han",
                At = now
            });
        }

        var code = AccountRules.NewCode();
        var invitation = new StaffInvitation
        {
            CodeHash = AccountRules.HashCode(code),
            Email = normalizedEmail,
            FullName = name,
            RoleId = role.Id,
            Role = role,
            ExpiresAt = now.AddDays(AccountRules.InvitationDays),
            UsedAt = null,
            IsRevoked = false,
            IssuedAt = now,
            IssuedById = adminId
        };
        db.StaffInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = adminId,
            ObjectType = "DangKyNhanVien",
            ObjectId = invitation.Id.ToString(),
            Action = "CAP_MA_NHAN_VIEN",
            NewValue = $"vaiTro={roleCode}",
            At = now
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new IssuedStaffCode(AccountRules.FormatCode(code), normalizedEmail, name, role.Name, invitation.ExpiresAt);
    }

    /// <summary>Nhân viên dùng mã để tạo tài khoản. Tài khoản dùng được ngay, với đúng vai trò ghi trong mã.</summary>
    public async Task RedeemAsync(StaffRedeemRequest r, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var email = (r.Email ?? "").Trim().ToLowerInvariant();

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Code)) errors["Code"] = "Vui lòng nhập mã đăng ký.";
        if (!AccountRules.IsValidEmail(email)) errors["Email"] = "Email không đúng định dạng.";
        if (!string.IsNullOrWhiteSpace(r.Phone) && !AccountRules.IsValidPhone(r.Phone))
            errors["Phone"] = "Số điện thoại gồm 10 đến 11 chữ số, bắt đầu bằng 0.";
        if (!AccountRules.IsStrongPassword(r.Password))
            errors["Password"] = "Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường và chữ số (NFR-02).";
        else if (r.Password != r.ConfirmPassword)
            errors["ConfirmPassword"] = "Mật khẩu xác nhận không khớp.";
        if (errors.Count > 0) throw new FieldValidationException(errors);

        var invitation = await db.StaffInvitations.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.CodeHash == AccountRules.HashCode(r.Code), ct);
        if (invitation is null || !IsUsable(invitation, email, now))
            throw CodeRejectedError();

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new FieldValidationException(new Dictionary<string, string>
            {
                ["Email"] = "Email này đã có tài khoản. Hãy đăng nhập hoặc dùng chức năng quên mật khẩu."
            });

        await using var tx = await db.BeginTransactionAsync(ct);

        // Đánh dấu đã dùng bằng một lệnh UPDATE có điều kiện: nếu hai lần gửi đến cùng lúc, chỉ một lần thành công.
        var invitationId = invitation.Id;
        var claimed = await db.StaffInvitations
            .Where(x => x.Id == invitationId && x.UsedAt == null && !x.IsRevoked)
            .ToListAsync(ct);
        foreach (var inv in claimed)
        {
            inv.UsedAt = now;
        }
        if (claimed.Count == 0) throw CodeRejectedError();

        var user = new User
        {
            FullName = invitation.FullName,
            Email = email,
            PasswordHash = hasher.Hash(r.Password),
            Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim(),
            IsActive = true,
            RoleId = invitation.RoleId,
            Role = invitation.Role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = user.Id,
            ObjectType = "NguoiDung",
            ObjectId = user.Id.ToString(),
            Action = "DANG_KY_NHAN_VIEN",
            NewValue = $"vaiTro={invitation.Role.Code}",
            At = now
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>Thu hồi mã chưa dùng. Mã đã dùng không thu hồi được vì tài khoản đã được tạo từ mã đó.</summary>
    public async Task RevokeAsync(int id, int adminId, CancellationToken ct = default)
    {
        var invitation = await db.StaffInvitations.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("Không tìm thấy mã đăng ký.", 404);
        if (invitation.UsedAt is not null)
            throw new DomainException("Mã này đã được dùng để tạo tài khoản nên không thể thu hồi.", 409);
        if (invitation.IsRevoked) return;

        invitation.IsRevoked = true;
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = adminId,
            ObjectType = "DangKyNhanVien",
            ObjectId = id.ToString(),
            Action = "THU_HOI_MA_NHAN_VIEN",
            NewValue = "lyDo=quan-tri-vien",
            At = VietnamTime.Now
        });
        await db.SaveChangesAsync(ct);
    }

    private static bool IsUsable(StaffInvitation i, string email, DateTime now) =>
        i.UsedAt is null && !i.IsRevoked && i.ExpiresAt >= now && i.Email == email;

    private static FieldValidationException CodeRejectedError() =>
        new(new Dictionary<string, string> { ["Code"] = CodeRejected });

    private static InvitationStatus StatusOf(DateTime? usedAt, bool isRevoked, DateTime expiresAt, DateTime now) =>
        usedAt is not null ? InvitationStatus.Used
        : isRevoked ? InvitationStatus.Revoked
        : expiresAt < now ? InvitationStatus.Expired
        : InvitationStatus.Pending;
}
