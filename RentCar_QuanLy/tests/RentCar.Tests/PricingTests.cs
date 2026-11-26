using RentCar.Domain.Common;
using RentCar.Domain.Pricing;
using RentCar.Domain.Rules;
using Xunit;

namespace RentCar.Tests;

/// <summary>Kiểm thử theo các ví dụ V1 đến V6 (SRS mục 3.9) và TC-10, TC-12 đến TC-15 (mục 8.2).</summary>
public class PricingTests
{
    private const decimal Daily = 800_000m;
    private const decimal Weekly = 4_800_000m;
    private static readonly DateTime T0 = new(2026, 12, 1, 9, 0, 0);

    [Fact] // V1: thuê 3 ngày, ưu đãi 10%
    public void V1_ThreeDaysWithTenPercentDiscount_TotalIs2160000()
    {
        var q = PricingCalculator.Quote(T0, T0.AddDays(3), Daily, Weekly, 1m, 10m);
        Assert.Equal(2_160_000m, q.Total);
    }

    [Fact] // V2: 25 giờ làm tròn lên 2 ngày
    public void V2_TwentyFiveHours_RoundsUpToTwoDays()
    {
        var q = PricingCalculator.Quote(T0, T0.AddHours(25), Daily, Weekly);
        Assert.Equal(2, q.DayCount);
        Assert.Equal(1_600_000m, q.Total);
    }

    [Fact] // V3: trễ 90 phút nằm trong ân hạn 120 phút
    public void V3_LateByNinetyMinutes_HasNoCharge()
    {
        var planned = T0.AddDays(3);
        Assert.Equal(0m, PricingCalculator.LateReturnCharge(planned, planned.AddMinutes(90), Daily));
    }

    [Fact] // V4: trễ 125 phút, làm tròn lên 1 ngày
    public void V4_LateByOneHundredTwentyFiveMinutes_ChargesOneDay()
    {
        var planned = T0.AddDays(2);
        Assert.Equal(800_000m, PricingCalculator.LateReturnCharge(planned, planned.AddMinutes(125), Daily));
    }

    [Fact] // V5: chạy 720 km, hạn mức 600 km
    public void V5_Driven720KmWithAllowance600_ChargesSixHundredThousand()
    {
        Assert.Equal(600_000m, PricingCalculator.MileageCharge(720, 3));
    }

    [Fact] // V6: 9 ngày = 1 tuần + 2 ngày
    public void V6_NineDays_UsesOneWeekPlusTwoDays()
    {
        var q = PricingCalculator.Quote(T0, T0.AddDays(9), Daily, Weekly);
        Assert.Equal(6_400_000m, q.Total);
    }

    [Fact] // BR-14: hủy trước 48 giờ được hoàn đủ cọc
    public void Cancel_MoreThan48HoursBefore_RefundsFullDeposit()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.CustomerCancel, T0.AddHours(-72), T0, 1_000_000m);
        Assert.Equal(1_000_000m, s.Refund);
        Assert.Equal(0m, s.Retained);
    }

    [Fact] // BR-14: hủy trong 48 giờ giữ 30% cọc
    public void Cancel_Within48Hours_RetainsThirtyPercent()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.CustomerCancel, T0.AddHours(-20), T0, 1_000_000m);
        Assert.Equal(700_000m, s.Refund);
        Assert.Equal(300_000m, s.Retained);
    }

    [Fact] // BR-12: thời gian thuê dưới 4 giờ bị từ chối
    public void Window_BelowFourHours_IsRejected()
    {
        Assert.Throws<DomainException>(() => PricingCalculator.Quote(T0, T0.AddHours(3), Daily, Weekly));
    }

    [Fact] // BR-12: thời gian thuê trên 30 ngày bị từ chối
    public void Window_OverThirtyDays_IsRejected()
    {
        Assert.Throws<DomainException>(() => PricingCalculator.Quote(T0, T0.AddDays(31), Daily, Weekly));
    }
}

public class BookingRulesTests
{
    private static readonly DateTime Start = new(2026, 12, 1, 9, 0, 0);

    [Fact] // BR-12: đúng 21 tuổi tại ngày nhận xe là hợp lệ
    public void Age_ExactlyTwentyOne_IsAccepted()
    {
        Assert.True(BookingRules.IsOldEnough(new DateOnly(2005, 12, 1), Start));
    }

    [Fact] // BR-12: thiếu một ngày so với 21 tuổi là không hợp lệ
    public void Age_OneDayShort_IsRejected()
    {
        Assert.False(BookingRules.IsOldEnough(new DateOnly(2005, 12, 2), Start));
    }

    [Fact] // BR-03c: hạng C đáp ứng hạng B
    public void License_ClassCCoversClassB_WhenValid()
    {
        Assert.True(BookingRules.LicenseCovers("C", "B", new DateOnly(2030, 1, 1), Start.AddDays(4)));
    }

    [Fact] // BR-03c: GPLX hết hạn trước khi trả xe là không hợp lệ
    public void License_ExpiresBeforeReturn_IsRejected()
    {
        Assert.False(BookingRules.LicenseCovers("B", "B", new DateOnly(2026, 12, 3), Start.AddDays(4)));
    }
}
