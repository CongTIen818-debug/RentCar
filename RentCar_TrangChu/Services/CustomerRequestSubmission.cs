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
        if (ids.Length == 0 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Length)
            throw new DomainException("Vui lòng chọn danh sách xe hợp lệ, không lặp xe.");
        await using var tx = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await database.Vehicles.CountAsync(v => ids.Contains(v.Id) && v.Status == VehicleStatus.Ready, ct) != ids.Length)
            throw new DomainException("Có xe không tồn tại hoặc hiện không sẵn sàng. Vui lòng chọn lại.", 409);
        if (!await database.PickupLocations.AnyAsync(l => l.Id == locationId && l.IsActive && l.Name != "Chưa xác định - hợp đồng cũ", ct))
            throw new DomainException("Điểm giao nhận không hợp lệ hoặc không còn hoạt động.");
        // Reuse EF mapping: Vietnam dates become UTC when written. This also applies to the 24-hour hold.
        var request = await rentals.CreateRequestAsync(customerId, null, requested.OrderBy(v => v.VehicleId).ToArray(),
            start, end, locationId, locationId, ct);
        await tx.CommitAsync(ct);
        return request.Id;
    }
}
