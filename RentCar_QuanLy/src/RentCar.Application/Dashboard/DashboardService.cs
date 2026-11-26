using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Enums;

namespace RentCar.Application.Dashboard;

/// <summary>Một chỉ số so sánh giữa kỳ hiện tại và kỳ trước đó.</summary>
public sealed record KpiValue(decimal Current, decimal Previous)
{
    /// <summary>Tỷ lệ tăng trưởng so với kỳ trước (%). Null khi kỳ trước bằng 0.</summary>
    public decimal? GrowthPercent => Previous == 0 ? null : Math.Round((Current - Previous) / Previous * 100m, 1);
}

/// <summary>Doanh thu theo ngày, dùng cho biểu đồ.</summary>
public sealed record DailyRevenue(DateOnly Day, decimal Amount);

/// <summary>Số liệu tổng quan. Doanh thu tính theo tiền đã thu thực tế (StatusCode = 1).</summary>
public sealed record DashboardSnapshot(KpiValue Rentals, KpiValue Revenue, KpiValue NetRevenue, IReadOnlyList<DailyRevenue> Daily);

/// <summary>
/// v9: thống kê cho trang Tổng quan của Quản trị viên.
/// Doanh thu gồm tiền thuê, phụ phí và khoản giữ cọc khi hủy. Doanh thu thuần trừ thêm tiền hoàn cọc.
/// CSDL chưa có chi phí vận hành, nên chưa tính được lợi nhuận thật.
/// </summary>
public sealed class DashboardService(IAppDbContext db)
{
    private const int PeriodDays = 30;
    private const int ChartDays = 14;

    public async Task<DashboardSnapshot> GetAsync(DateTime now, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var currentStart = today.AddDays(-(PeriodDays - 1)).ToDateTime(TimeOnly.MinValue);
        var currentEnd = currentStart.AddDays(PeriodDays);
        var previousStart = currentStart.AddDays(-PeriodDays);

        var rentals = await db.Contracts.AsNoTracking()
            .Where(c => c.CreatedAt >= previousStart && c.Status != ContractStatus.Cancelled)
            .Select(c => c.CreatedAt)
            .ToListAsync(ct);

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.PaidAt >= previousStart && p.StatusCode == 1)
            .Select(p => new { p.PaidAt, p.Type, p.Amount })
            .ToListAsync(ct);

        static bool IsRevenue(PaymentType t) => t is PaymentType.Rent or PaymentType.Surcharge or PaymentType.Forfeit;

        decimal Revenue(DateTime from, DateTime to) =>
            payments.Where(p => p.PaidAt >= from && p.PaidAt < to && IsRevenue(p.Type)).Sum(p => p.Amount);

        decimal Refund(DateTime from, DateTime to) =>
            payments.Where(p => p.PaidAt >= from && p.PaidAt < to && p.Type == PaymentType.DepositRefund).Sum(p => p.Amount);

        var revenueNow = Revenue(currentStart, currentEnd);
        var revenuePrev = Revenue(previousStart, currentStart);
        var refundNow = Refund(currentStart, currentEnd);
        var refundPrev = Refund(previousStart, currentStart);

        var rentalsNow = rentals.Count(d => d >= currentStart);
        var rentalsPrev = rentals.Count - rentalsNow;

        var daily = new List<DailyRevenue>(ChartDays);
        for (var i = ChartDays - 1; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var from = day.ToDateTime(TimeOnly.MinValue);
            daily.Add(new DailyRevenue(day, Revenue(from, from.AddDays(1))));
        }

        return new DashboardSnapshot(
            new KpiValue(rentalsNow, rentalsPrev),
            new KpiValue(revenueNow, revenuePrev),
            new KpiValue(revenueNow - refundNow, revenuePrev - refundPrev),
            daily);
    }
}
