using System.Data;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Rentals;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
using RentCar.Infrastructure.Persistence;

namespace RentCar.Services;

// Adapter for the member's single/multi-vehicle request buttons. Management code stays unchanged.
public sealed class CustomerRequestSubmission(AppDbContext database, RentalService rentals)
{
    public static void ValidateSchedule(DateTime start, DateTime end, DateTime nowVietnam)
    {
        if (start.Date <= nowVietnam.Date) throw new DomainException("Ngày nhận xe phải sau ngày hôm nay.");
        BookingRules.ValidateWindow(start, end);
    }
    public async Task<int> SubmitAsync(int customerId, IReadOnlyList<RequestedVehicle> requested,
        DateTime pickupAt, DateTime returnAt, int locationId, CancellationToken ct)
    {
        var start = DateTime.SpecifyKind(pickupAt, DateTimeKind.Unspecified);
        var end = DateTime.SpecifyKind(returnAt, DateTimeKind.Unspecified);
        ValidateSchedule(start, end, VietnamTime.Now);
        var ids = requested.Select(v => v.VehicleId).ToArray();
        if (ids.Length == 0 || ids.Length > 100 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Length)
            throw new DomainException("Vui lòng chọn danh sách xe hợp lệ, không lặp xe.");
        await using var tx = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await database.Vehicles.CountAsync(v => ids.Contains(v.Id) && v.Status == VehicleStatus.Ready, ct) != ids.Length)
            throw new DomainException("Có xe không tồn tại hoặc hiện không sẵn sàng. Vui lòng chọn lại.", 409);
        if (!await database.PickupLocations.AnyAsync(l => l.Id == locationId && l.IsActive && l.Name != "Chưa xác định - hợp đồng cũ", ct))
            throw new DomainException("Điểm giao nhận không hợp lệ hoặc không còn hoạt động.");
        var customer=await database.Users.AsNoTracking().Include(u=>u.Role).Include(u=>u.DriverLicense).SingleOrDefaultAsync(u=>u.Id==customerId,ct);
        if(customer is null || !customer.IsActive || customer.Role.Code!="KHACHHANG") throw new DomainException("Tài khoản không được gửi yêu cầu thuê xe.",403);
        if(string.IsNullOrWhiteSpace(customer.FullName)||string.IsNullOrWhiteSpace(customer.Phone)||string.IsNullOrWhiteSpace(customer.Email)||string.IsNullOrWhiteSpace(customer.IdNumber))
            throw new DomainException("Vui lòng cập nhật đầy đủ hồ sơ và giấy tờ giả lập trước khi gửi yêu cầu.");
        if(customer.BirthDate is not {} birth || !BookingRules.IsOldEnough(birth,start))throw new DomainException("Người thuê phải đủ 21 tuổi tại ngày nhận xe. Vui lòng kiểm tra hồ sơ.");
        var cars=await database.Vehicles.AsNoTracking().Where(v=>ids.Contains(v.Id)).Include(v=>v.VehicleType).ToListAsync(ct);
        if(requested.Any(line=>!line.WithDriver && (customer.DriverLicense is not {} license || !BookingRules.LicenseCovers(license.LicenseClass,cars.Single(v=>v.Id==line.VehicleId).VehicleType.MinLicenseClass,license.ExpiryDate,end))))
            throw new DomainException("Vui lòng cập nhật GPLX đúng hạng và còn hiệu lực đến hết ngày trả xe trong mục Hồ sơ của tôi.");
        // Store Vietnam wall-clock dates directly, including the 24-hour hold.
        var request = await rentals.CreateRequestAsync(customerId, null, requested.OrderBy(v => v.VehicleId).ToArray(),
            start, end, locationId, locationId, ct);
        await tx.CommitAsync(ct);
        return request.Id;
    }
}
