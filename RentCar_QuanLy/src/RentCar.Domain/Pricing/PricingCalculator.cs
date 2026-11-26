using RentCar.Domain.Common;
using RentCar.Domain.Rules;

namespace RentCar.Domain.Pricing;

public sealed record PriceQuote(int DayCount, int WeekCount, decimal DailyRate, decimal WeeklyRate, decimal Gross, decimal DiscountAmount)
{
    public decimal Total => Gross - DiscountAmount;
}

public enum CancellationKind
{
    CustomerCancel,
    NoShow,
    CompanyCancelled
}

public readonly record struct DepositSettlement(decimal Refund, decimal Retained);

/// <summary>Tính tiền theo SRS mục 3.9 (SP-01, SP-02) và mục 3.7 (BR-09, BR-10, BR-14).</summary>
public static class PricingCalculator
{
    /// <summary>BR-09: làm tròn lên theo mỗi 24 giờ, tối thiểu 1 ngày.</summary>
    public static int CountDays(DateTime startAt, DateTime endAt)
    {
        var minutes = (endAt - startAt).TotalMinutes;
        return Math.Max(1, (int)Math.Ceiling(minutes / (24 * 60)));
    }

    /// <summary>
    /// BR-10: giá ngày đã nhân hệ số, mỗi 7 ngày tính theo giá tuần, phần dư tính theo ngày, rồi trừ ưu đãi.
    /// Làm tròn đến 1 đồng (mục 2.4.2).
    /// </summary>
    public static PriceQuote Quote(DateTime startAt, DateTime endAt, decimal dailyRate, decimal weeklyRate,
        decimal priceFactor = 1m, decimal discountPercent = 0m)
    {
        BookingRules.ValidateWindow(startAt, endAt);
        if (dailyRate < 0 || weeklyRate < 0)
            throw new DomainException("Giá thuê không được âm (BR-02).");

        var days = CountDays(startAt, endAt);
        var weeks = days / 7;
        var rest = days % 7;

        var dailyApplied = Round(dailyRate * priceFactor);
        var weeklyApplied = Round(weeklyRate * priceFactor);
        var gross = weeks * weeklyApplied + rest * dailyApplied;
        var discount = Round(gross * discountPercent / 100m);

        return new PriceQuote(days, weeks, dailyApplied, weeklyApplied, gross, discount);
    }

    /// <summary>SP-01: trả muộn quá ân hạn; số ngày trễ làm tròn lên, nhân với giá ngày.</summary>
    public static decimal LateReturnCharge(DateTime plannedEnd, DateTime actualReturn, decimal dailyRate,
        int graceMinutes = BookingRules.LateGraceMinutes)
    {
        var late = (actualReturn - plannedEnd).TotalMinutes;
        if (late <= graceMinutes) return 0m;
        var lateDays = (int)Math.Ceiling((late - graceMinutes) / (24 * 60));
        return lateDays * dailyRate;
    }

    /// <summary>SP-02: km vượt hạn mức = km mỗi ngày × số ngày thuê, nhân đơn giá km.</summary>
    public static decimal MileageCharge(int drivenKm, int dayCount,
        int kmPerDay = BookingRules.KmPerDayAllowance, decimal pricePerKm = BookingRules.KmUnitPrice)
    {
        var allowance = kmPerDay * dayCount;
        var over = Math.Max(0, drivenKm - allowance);
        return over * pricePerKm;
    }

    /// <summary>BR-14: bảng xử lý tiền cọc khi hủy hợp đồng.</summary>
    public static DepositSettlement SettleDeposit(CancellationKind kind, DateTime now, DateTime startAt, decimal deposit)
    {
        if (kind == CancellationKind.CompanyCancelled) return new(deposit, 0m);
        if (kind == CancellationKind.NoShow) return new(0m, deposit);

        if (startAt - now >= TimeSpan.FromHours(BookingRules.CancelFreeHours))
            return new(deposit, 0m);

        var retained = Round(deposit * 0.30m);
        return new(deposit - retained, retained);
    }

    private static decimal Round(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
