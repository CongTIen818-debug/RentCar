using System.Globalization;
using RentCar.Domain.Common;

namespace RentCar.Models;

// Quy ước phía khách: yêu cầu chỉ có 1 chờ duyệt, 2 đã duyệt, 3 đã hủy.
public static class CustomerRequestRules
{
    public static bool IsExpired(int status, bool depositPaid, DateTime deadlineUtc, DateTime nowUtc) =>
        status == 2 && !depositPaid && nowUtc >= deadlineUtc;
    public static bool CanPay(int status, bool depositPaid, DateTime deadlineUtc, DateTime nowUtc, int vehicleCount) =>
        status == 2 && !depositPaid && nowUtc < deadlineUtc && vehicleCount > 0;
    public static string Label(int status) => status switch { 1 => "Đang chờ duyệt", 2 => "Đã duyệt", 3 => "Đã hủy", _ => "Chưa xác định" };
}

public sealed class CustomerRequestRow
{
    public int Id { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime PickupAtUtc { get; set; }
    public DateTime ReturnAtUtc { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public string PickupLocation { get; set; } = "";
    public string ReturnLocation { get; set; } = "";
    public decimal RequiredDeposit { get; set; }
    public decimal PaidDeposit { get; set; }
    public bool HasDeposit { get; set; }
    public bool HasUnreadApproval { get; set; }
}
public sealed class CustomerRequestVehicle
{
    public int RequestId { get; set; }
    public int VehicleId { get; set; }
    public string Name { get; set; } = "";
    public string Plate { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool WithDriver { get; set; }
}
public sealed class CustomerRequestCounts
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Cancelled { get; set; }
}
public sealed record CustomerRequestCard(CustomerRequestRow Request, IReadOnlyList<CustomerRequestVehicle> Vehicles, DateTime NowUtc)
{
    public bool IsExpired => CustomerRequestRules.IsExpired(Request.Status, Request.HasDeposit, Request.DeadlineUtc, NowUtc);
    public bool CanPay => CustomerRequestRules.CanPay(Request.Status, Request.HasDeposit, Request.DeadlineUtc, NowUtc, Vehicles.Count);
    public string Version => Request.DeadlineUtc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    public static string When(DateTime utc) => VietnamTime.FromUtc(utc).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
}
public sealed record CustomerRequestsPage(IReadOnlyList<CustomerRequestCard> Items, CustomerRequestCounts Counts,
    int Page, int PageCount, int? Status, int UnreadCount, string? Error = null);
public sealed record RequestNoticeState(int Count);
