using System.Data;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Infrastructure.Persistence;
namespace RentCar.Services;
public sealed class CustomerPrivacy(AppDbContext db,IPasswordHasher hasher)
{
    public async Task AnonymizeAsync(int userId,string password,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var user=await db.Users.Include(u=>u.DriverLicense).Include(u=>u.Role).SingleOrDefaultAsync(u=>u.Id==userId,ct)
            ??throw new DomainException("Không tìm thấy tài khoản.",404);
        if(user.Role.Code!="KHACHHANG" || !user.IsActive) throw new DomainException("Tài khoản không được thực hiện thao tác này.",403);
        if(!hasher.Verify(user.PasswordHash,password)) throw new FieldValidationException(new Dictionary<string,string>{{"Password","Mật khẩu hiện tại không đúng."}});
        if(await db.Contracts.AnyAsync(c=>c.CustomerId==userId && (c.Status==ContractStatus.PendingConfirmation||c.Status==ContractStatus.Confirmed||c.Status==ContractStatus.Active),ct)
          || await db.RentalRequests.AnyAsync(q=>q.CustomerId==userId && q.Status!=RentalRequestStatus.Cancelled && q.HoldUntil>VietnamTime.Now,ct))
            throw new DomainException("Bạn còn yêu cầu hoặc hợp đồng chưa kết thúc. Vui lòng liên hệ nhân viên để xử lý trước khi ẩn danh tài khoản.",409);
        user.FullName="Khách hàng đã ẩn danh #"+user.Id;user.Email=$"anondanh-{user.Id}-{Guid.NewGuid():N}@rentcar.invalid";
        user.Phone=null;user.Address=null;user.IdNumber=null;user.BirthDate=null;user.IsActive=false;
        user.PasswordHash=hasher.Hash(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        user.ResetCodeHash=null;user.ResetCodeExpiresAt=null;
        if(user.DriverLicense is not null) db.DriverLicenses.Remove(user.DriverLicense);
        db.AuditLogs.Add(new AuditLog {ActorId=userId,ObjectType="NguoiDung",ObjectId=userId.ToString(),Action="KHACH_AN_DANH",NewValue="an-danh; giu-giao-dich-tai-chinh",At=VietnamTime.Now});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
