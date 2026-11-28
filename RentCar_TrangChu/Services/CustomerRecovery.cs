using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Accounts;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Infrastructure.Persistence;
namespace RentCar.Services;
public sealed class CustomerRecovery(AppDbContext db, IPasswordHasher hasher, IEmailSender mailer, IConfiguration configuration, IWebHostEnvironment env)
{
    public async Task<string?> RequestAsync(string email, CancellationToken ct)
    {
        var normalized=email.Trim().ToLowerInvariant();
        var user=await db.Users.Include(u=>u.Role).SingleOrDefaultAsync(u=>u.Email==normalized && u.IsActive,ct);
        if(user is null || user.Role.Code!="KHACHHANG") return null;
        var code=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.ResetCodeHash=AccountRules.HashCode(code); user.ResetCodeExpiresAt=VietnamTime.Now.AddMinutes(30);
        await db.SaveChangesAsync(ct);
        var origin=configuration["CustomerAccount:PublicBaseUrl"]??"https://localhost:7246";
        if(!Uri.TryCreate(origin,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo)) throw new InvalidOperationException("CustomerAccount:PublicBaseUrl phải là địa chỉ HTTPS.");
        var link=origin.TrimEnd('/')+"/tai-khoan/dat-lai-mat-khau?email="+Uri.EscapeDataString(user.Email)+"&code="+code;
        var sent=await mailer.SendAsync(user.Email,"RentCar: đặt lại mật khẩu","Mở liên kết để đặt lại mật khẩu: "+link+"\nLiên kết có hiệu lực 30 phút, chỉ dùng một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.",ct);
        if(!sent){user.ResetCodeHash=null;user.ResetCodeExpiresAt=null;}
        db.AuditLogs.Add(new AuditLog {ActorId=null,ObjectType="NguoiDung",ObjectId=user.Id.ToString(),Action="KHACH_YEU_CAU_DAT_LAI",NewValue=sent?(mailer.IsSimulated?"mo-phong":"email"):"loi-gui",At=VietnamTime.Now});
        await db.SaveChangesAsync(ct);
        return sent && mailer.IsSimulated && env.IsDevelopment() && configuration.GetValue<bool>("CustomerAccount:ShowResetPreview") ? link : null;
    }
    public async Task ResetAsync(string email,string code,string password,string confirm,CancellationToken ct)
    {
        var errors=new Dictionary<string,string>();
        if(!AccountRules.IsStrongPassword(password)) errors["NewPassword"]="Mật khẩu tối thiểu 8 ký tự, có chữ hoa, chữ thường và số.";
        if(password!=confirm) errors["ConfirmPassword"]="Mật khẩu xác nhận không khớp.";
        if(errors.Count>0) throw new FieldValidationException(errors);
        var hash=hasher.Hash(password);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var normalized=email.Trim().ToLowerInvariant();
        var user=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.nguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE email={normalized}").Include(u=>u.Role).SingleOrDefaultAsync(ct);
        if(user is null || user.Role.Code!="KHACHHANG" || !user.IsActive || user.ResetCodeHash is null || user.ResetCodeExpiresAt is null || user.ResetCodeExpiresAt<=VietnamTime.Now || !AccountRules.CodeMatches(user.ResetCodeHash,code))
            throw new FieldValidationException(new Dictionary<string,string>{{"Code","Liên kết không hợp lệ, đã dùng hoặc đã hết hạn."}});
        user.PasswordHash=hash; user.ResetCodeHash=null;user.ResetCodeExpiresAt=null;
        // Keep the member's account lock; resetting a password must not unlock a locked account.
        db.AuditLogs.Add(new AuditLog {ActorId=null,ObjectType="NguoiDung",ObjectId=user.Id.ToString(),Action="KHACH_DAT_LAI_MAT_KHAU",At=VietnamTime.Now});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
