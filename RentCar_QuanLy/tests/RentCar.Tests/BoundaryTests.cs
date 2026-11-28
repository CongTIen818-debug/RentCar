using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Pricing;
using RentCar.Domain.Rules;
using Xunit;

namespace RentCar.Tests;

/// <summary>
/// Ca biên bổ sung (YEU_CAU_TIEP_THEO mục 3.D): đúng ngưỡng 48 giờ, khách không đến, hủy do công ty,
/// ân hạn trả muộn, hạn mức km, giá tuần và điều kiện xác nhận hợp đồng.
/// Ca biên về giữ chỗ hết hạn và trùng lịch đồng thời cần CSDL thật nên không nằm ở đây.
/// </summary>
public class BoundaryTests
{
    private const decimal Daily = 800_000m;
    private const decimal Weekly = 4_800_000m;
    private static readonly DateTime T0 = new(2026, 12, 1, 9, 0, 0);

    // ---------- Hủy và tiền cọc (BR-14) ----------

    [Fact] // đúng 48 giờ trước giờ nhận xe: vẫn được hoàn đủ
    public void Cancel_ExactlyFortyEightHoursBefore_RefundsFullDeposit()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.CustomerCancel, T0.AddHours(-48), T0, 1_000_000m);
        Assert.Equal(1_000_000m, s.Refund);
        Assert.Equal(0m, s.Retained);
    }

    [Fact] // thiếu 1 phút so với 48 giờ: giữ 30% cọc
    public void Cancel_FortySevenHoursFiftyNineMinutesBefore_RetainsThirtyPercent()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.CustomerCancel,
            T0.AddHours(-48).AddMinutes(1), T0, 1_000_000m);
        Assert.Equal(700_000m, s.Refund);
        Assert.Equal(300_000m, s.Retained);
    }

    [Fact] // khách không đến: giữ toàn bộ cọc, không cần xét thời gian
    public void NoShow_RetainsWholeDeposit()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.NoShow, T0.AddHours(-72), T0, 1_000_000m);
        Assert.Equal(0m, s.Refund);
        Assert.Equal(1_000_000m, s.Retained);
    }

    [Fact] // công ty hủy: hoàn đủ cọc dù chỉ còn vài giờ
    public void CompanyCancelled_RefundsWholeDeposit_EvenWithinTwoDays()
    {
        var s = PricingCalculator.SettleDeposit(CancellationKind.CompanyCancelled, T0.AddHours(-1), T0, 1_000_000m);
        Assert.Equal(1_000_000m, s.Refund);
        Assert.Equal(0m, s.Retained);
    }

    // ---------- Số ngày và giá (BR-09, BR-10) ----------

    [Fact] // đúng 24 giờ là 1 ngày
    public void CountDays_ExactlyTwentyFourHours_IsOneDay()
    {
        Assert.Equal(1, PricingCalculator.CountDays(T0, T0.AddHours(24)));
    }

    [Fact] // hơn 24 giờ một phút là 2 ngày
    public void CountDays_TwentyFourHoursOneMinute_IsTwoDays()
    {
        Assert.Equal(2, PricingCalculator.CountDays(T0, T0.AddHours(24).AddMinutes(1)));
    }

    [Fact] // tối thiểu 1 ngày
    public void CountDays_FourHours_IsOneDay()
    {
        Assert.Equal(1, PricingCalculator.CountDays(T0, T0.AddHours(4)));
    }

    [Fact] // đúng 7 ngày: tính theo giá tuần, không cộng giá ngày
    public void Quote_SevenDays_UsesWeeklyRateOnly()
    {
        var q = PricingCalculator.Quote(T0, T0.AddDays(7), Daily, Weekly);
        Assert.Equal(7, q.DayCount);
        Assert.Equal(1, q.WeekCount);
        Assert.Equal(Weekly, q.Gross);
    }

    [Fact] // 8 ngày: 1 tuần + 1 ngày
    public void Quote_EightDays_IsOneWeekPlusOneDay()
    {
        var q = PricingCalculator.Quote(T0, T0.AddDays(8), Daily, Weekly);
        Assert.Equal(Weekly + Daily, q.Gross);
    }

    [Fact] // đúng 4 giờ là hợp lệ (BR-12)
    public void Window_ExactlyFourHours_IsAccepted()
    {
        Assert.Null(Record.Exception(() => BookingRules.ValidateWindow(T0, T0.AddHours(4))));
    }

    [Fact] // đúng 30 ngày là hợp lệ (BR-12)
    public void Window_ExactlyThirtyDays_IsAccepted()
    {
        Assert.Null(Record.Exception(() => BookingRules.ValidateWindow(T0, T0.AddDays(30))));
    }

    // ---------- Trả muộn (SP-01, ân hạn 120 phút) ----------

    [Fact] // trả đúng hết ân hạn: không tính phí
    public void LateReturn_ExactlyGraceMinutes_HasNoCharge()
    {
        Assert.Equal(0m, PricingCalculator.LateReturnCharge(T0, T0.AddMinutes(120), Daily));
    }

    [Fact] // quá ân hạn 1 phút: tính 1 ngày
    public void LateReturn_OneMinuteAfterGrace_ChargesOneDay()
    {
        Assert.Equal(Daily, PricingCalculator.LateReturnCharge(T0, T0.AddMinutes(121), Daily));
    }

    [Fact] // quá ân hạn đúng 24 giờ: vẫn 1 ngày
    public void LateReturn_TwentyFourHoursAfterGrace_ChargesOneDay()
    {
        Assert.Equal(Daily, PricingCalculator.LateReturnCharge(T0, T0.AddMinutes(120 + 1440), Daily));
    }

    [Fact] // quá ân hạn 24 giờ một phút: 2 ngày
    public void LateReturn_OneMinuteBeyondOneDayAfterGrace_ChargesTwoDays()
    {
        Assert.Equal(2 * Daily, PricingCalculator.LateReturnCharge(T0, T0.AddMinutes(120 + 1441), Daily));
    }

    // ---------- Vượt km (SP-02, 200 km mỗi ngày, 5.000 đ mỗi km) ----------

    [Fact] // đúng hạn mức 3 ngày = 600 km: không tính phí
    public void Mileage_ExactlyAllowance_HasNoCharge()
    {
        Assert.Equal(0m, PricingCalculator.MileageCharge(600, 3));
    }

    [Fact] // vượt 1 km: tính đúng đơn giá 1 km
    public void Mileage_OneKmOverAllowance_ChargesOneKmRate()
    {
        Assert.Equal(5_000m, PricingCalculator.MileageCharge(601, 3));
    }

    // ---------- Điều kiện xác nhận hợp đồng (BR-01, BR-03, BR-12, BR-14) ----------

    private static RentalContract BuildContract() => new()
    {
        StartAt = T0,
        EndAt = T0.AddDays(2),
        DepositAmount = 1_000_000m,
        Customer = new User
        {
            FullName = "Nguyễn Văn A",
            Email = "a@example.com",
            Phone = "0912345678",
            IdNumber = "012345678901",
            BirthDate = new DateOnly(2000, 1, 1),
            DriverLicense = new DriverLicense { LicenseClass = "B", ExpiryDate = new DateOnly(2030, 12, 31) }
        },
        Lines = [new ContractLine
        {
            Vehicle = new Vehicle
            {
                Status = VehicleStatus.Ready,
                VehicleType = new VehicleType { MinLicenseClass = "B" }
            }
        }]
    };

    [Fact] // đủ mọi điều kiện: được xác nhận
    public void Confirmation_AllChecksPass_WhenNoClashAndDepositPaid()
    {
        var checks = ConfirmationRules.Evaluate(BuildContract(), depositPaid: 1_000_000m, hasClash: false);
        Assert.True(ConfirmationRules.CanConfirm(checks));
    }

    [Fact] // trùng lịch với hợp đồng hoặc yêu cầu khác: chặn xác nhận
    public void Confirmation_Clash_BlocksConfirmation()
    {
        var checks = ConfirmationRules.Evaluate(BuildContract(), depositPaid: 1_000_000m, hasClash: true);
        Assert.False(ConfirmationRules.CanConfirm(checks));
        Assert.Contains(checks, c => c.Label.Contains("chồng lịch") && !c.Passed);
    }

    [Fact] // cọc đã ghi nhận thiếu 1 đồng: chặn xác nhận
    public void Confirmation_DepositBelowRequired_BlocksConfirmation()
    {
        var checks = ConfirmationRules.Evaluate(BuildContract(), depositPaid: 999_999m, hasClash: false);
        Assert.False(ConfirmationRules.CanConfirm(checks));
    }

    [Fact] // GPLX hết hạn trước ngày trả xe: chặn xác nhận (BR-03c)
    public void Confirmation_LicenseExpiresBeforeReturn_BlocksConfirmation()
    {
        var contract = BuildContract();
        contract.Customer.DriverLicense!.ExpiryDate = new DateOnly(2026, 12, 2); // EndAt là 03/12/2026
        var checks = ConfirmationRules.Evaluate(contract, depositPaid: 1_000_000m, hasClash: false);
        Assert.False(checks.Single(c => c.Label.Contains("GPLX")).Passed);
    }
}
