using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;

namespace RentCar.Application.Accounts;

/// <summary>
/// Must #4 (FR-6.1b, FR-6.1c, UC-07): đổi mật khẩu khi đã đăng nhập, và quên mật khẩu bằng mã đặt lại
/// dùng một lần, hết hạn sau 30 phút. Mã chỉ lưu dạng băm; mã được gửi qua email (IEmailSender), không ghi log.
/// </summary>
public sealed class AccountSecurityService(IAppDbContext db, IPasswordHasher hasher, IEmailSender mailer)
{
    private const string ResetSubject = "RentCar: mã đặt lại mật khẩu";

    /// <summary>Đổi mật khẩu của tài khoản đang đăng nhập: phải xác minh mật khẩu hiện tại.</summary>
    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword,
        string confirmPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new DomainException("Phiên đăng nhập không hợp lệ.", 401);

        var errors = new Dictionary<string, string>();
        if (!hasher.Verify(user.PasswordHash, currentPassword ?? ""))
            errors["CurrentPassword"] = "Mật khẩu hiện tại không đúng.";
        ValidateNewPassword(newPassword, confirmPassword, errors);
        if (errors.Count == 0 && newPassword == currentPassword)
            errors["NewPassword"] = "Mật khẩu mới phải khác mật khẩu hiện tại.";
        if (errors.Count > 0) throw new FieldValidationException(errors);

        user.PasswordHash = hasher.Hash(newPassword);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = userId,
            ObjectType = "NguoiDung",
            ObjectId = userId.ToString(),
            Action = "DOI_MAT_KHAU",
            At = VietnamTime.Now
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Tạo mã đặt lại và gửi qua email. Trả về mã thô chỉ khi đang ở chế độ mô phỏng (chưa cấu hình SMTP);
    /// người gọi chỉ được hiện mã đó ở môi trường Development. Trả về null trong mọi trường hợp khác,
    /// kể cả khi email không tồn tại, để người dùng không biết email có đăng ký hay không.
    /// </summary>
    public async Task<string?> RequestResetCodeAsync(string emailAddress, CancellationToken ct = default)
    {
        var normalized = (emailAddress ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && u.IsActive, ct);
        if (user is null) return null;

        var code = AccountRules.NewCode();
        var now = VietnamTime.Now;
        user.ResetCodeHash = AccountRules.HashCode(code);
        user.ResetCodeExpiresAt = now.AddMinutes(AccountRules.ResetCodeMinutes);
        await db.SaveChangesAsync(ct); // lưu băm trước khi gửi, để mã trong email luôn kiểm tra được

        var sent = await mailer.SendAsync(user.Email, ResetSubject, ResetBody(AccountRules.FormatCode(code)), ct);
        if (!sent)
        {
            // Gửi lỗi thì hủy mã vừa tạo, không để lại mã sống mà người dùng không nhận được.
            user.ResetCodeHash = null;
            user.ResetCodeExpiresAt = null;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = null,
            ObjectType = "NguoiDung",
            ObjectId = user.Id.ToString(),
            Action = "YEU_CAU_DAT_LAI_MAT_KHAU",
            NewValue = !sent ? "kenh=loi-gui" : mailer.IsSimulated ? "kenh=mo-phong" : "kenh=email",
            At = now
        });
        await db.SaveChangesAsync(ct);

        return sent && mailer.IsSimulated ? code : null;
    }

    /// <summary>Đặt lại mật khẩu bằng mã còn hạn và chưa dùng. Mã bị xóa ngay sau khi dùng để chặn dùng lại.</summary>
    public async Task ResetPasswordAsync(string email, string code, string newPassword, string confirmPassword,
        CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        ValidateNewPassword(newPassword, confirmPassword, errors);
        if (errors.Count > 0) throw new FieldValidationException(errors);

        var normalized = (email ?? "").Trim().ToLowerInvariant();
        var now = VietnamTime.Now;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && u.IsActive, ct);

        // Sai email, sai mã, hết hạn hoặc đã dùng đều trả cùng một thông báo.
        var codeOk = user is not null && user.ResetCodeHash is not null && user.ResetCodeExpiresAt is { } expires
                     && expires >= now && AccountRules.CodeMatches(user.ResetCodeHash, code ?? "");
        if (user is null || !codeOk)
            throw new FieldValidationException(new Dictionary<string, string>
            {
                ["Code"] = "Mã đặt lại không đúng hoặc đã hết hạn."
            });

        user.PasswordHash = hasher.Hash(newPassword);
        user.ResetCodeHash = null;
        user.ResetCodeExpiresAt = null;
        user.FailedAttempts = 0;
        user.LockedUntil = null;

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = null,
            ObjectType = "NguoiDung",
            ObjectId = user.Id.ToString(),
            Action = "DAT_LAI_MAT_KHAU",
            At = now
        });
        await db.SaveChangesAsync(ct);
    }

    private static string ResetBody(string formattedCode) =>
        "Xin chào,\n\n" +
        $"Mã đặt lại mật khẩu RentCar của bạn là: {formattedCode}\n\n" +
        $"Mã có hiệu lực trong {AccountRules.ResetCodeMinutes} phút và chỉ dùng được một lần.\n" +
        "Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.";

    private static void ValidateNewPassword(string newPassword, string confirmPassword, Dictionary<string, string> errors)
    {
        if (!AccountRules.IsStrongPassword(newPassword))
            errors["NewPassword"] = "Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường và chữ số (NFR-02).";
        else if (newPassword != confirmPassword)
            errors["ConfirmPassword"] = "Mật khẩu xác nhận không khớp.";
    }
}
