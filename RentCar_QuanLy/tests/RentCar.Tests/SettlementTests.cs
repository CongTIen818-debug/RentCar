using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
using Xunit;

namespace RentCar.Tests;

/// <summary>Khoản phát sinh khi trả xe và cân đối thu chi: V4, V5, TC-11, TC-14, TC-15, TC-34 (SRS mục 8.2).</summary>
public class SettlementTests
{
    private static readonly DateTime Start = new(2026, 12, 1, 9, 0, 0);

    [Fact] // V4: trả muộn 125 phút, làm tròn lên 1 ngày
    public void ReturnCharges_LateByOneHundredTwentyFiveMinutes_ChargesOneDay()
    {
        var planned = Start.AddDays(2);
        var c = SettlementRules.CalculateReturnCharges(planned, planned.AddMinutes(125), 800_000m, 2, 0, 0m);
        Assert.Equal(800_000m, c.Late);
        Assert.Equal(800_000m, c.Total);
    }

    [Fact] // V5: 720 km trong 3 ngày, hạn mức 600 km
    public void ReturnCharges_720KmOverThreeDays_ChargesMileage()
    {
        var c = SettlementRules.CalculateReturnCharges(Start.AddDays(3), Start.AddDays(3), 800_000m, 3, 720, 0m);
        Assert.Equal(0m, c.Late);
        Assert.Equal(600_000m, c.Mileage);
    }

    [Fact] // TC-11 và SP-03: chi phí hư hỏng cộng vào tổng phát sinh
    public void ReturnCharges_DamageIsAddedToTotal()
    {
        var c = SettlementRules.CalculateReturnCharges(Start.AddDays(3), Start.AddDays(3), 800_000m, 3, 0, 1_500_000m);
        Assert.Equal(1_500_000m, c.Damage);
        Assert.Equal(1_500_000m, c.Total);
    }

    [Fact]
    public void ReturnCharges_NegativeDamage_IsRejected()
    {
        Assert.Throws<RentCar.Domain.Common.DomainException>(() =>
            SettlementRules.CalculateReturnCharges(Start, Start, 800_000m, 1, 0, -1m));
    }

    [Fact] // V1 đã thu đủ tiền thuê, đã hoàn đủ cọc: hợp đồng đủ điều kiện hoàn thành
    public void Balance_RentAndDepositFullySettled_IsSettled()
    {
        var c = Contract(ContractStatus.Active, gross: 2_400_000m, discount: 240_000m, deposit: 3_000_000m,
            Pay(PaymentType.Rent, 2_160_000m), Pay(PaymentType.DepositRefund, 3_000_000m));
        c.Handover = new HandoverRecord { ReturnedAt = Start.AddDays(3), ExtraCharges = 0m };

        Assert.True(SettlementRules.Balance(c).IsSettled);
    }

    [Fact] // TC-14: phụ phí trả muộn chưa thu nên chưa hoàn thành
    public void Balance_LateChargeUnpaid_LeavesOutstandingRent()
    {
        var c = Contract(ContractStatus.Active, gross: 2_400_000m, discount: 0m, deposit: 3_000_000m,
            Pay(PaymentType.Rent, 2_400_000m), Pay(PaymentType.DepositRefund, 3_000_000m));
        c.Handover = new HandoverRecord { ReturnedAt = Start.AddDays(2), ExtraCharges = 800_000m };

        var b = SettlementRules.Balance(c);
        Assert.Equal(800_000m, b.OutstandingRent);
        Assert.False(b.IsSettled);
    }

    [Fact] // Chưa hoàn đủ cọc thì chưa hoàn thành dù đã thu đủ tiền thuê
    public void Balance_DepositNotFullyRefunded_IsNotSettled()
    {
        var c = Contract(ContractStatus.Active, gross: 2_400_000m, discount: 0m, deposit: 3_000_000m,
            Pay(PaymentType.Rent, 2_400_000m), Pay(PaymentType.DepositRefund, 1_000_000m));
        c.Handover = new HandoverRecord { ReturnedAt = Start.AddDays(3), ExtraCharges = 0m };

        var b = SettlementRules.Balance(c);
        Assert.Equal(2_000_000m, b.OutstandingRefund);
        Assert.False(b.IsSettled);
    }

    [Fact] // TC-34: hủy dưới 48 giờ giữ 30% cọc, chỉ hoàn 70%
    public void Balance_CancelledWithForfeit_RefundsRemainderOnly()
    {
        var c = Contract(ContractStatus.Cancelled, gross: 2_400_000m, discount: 0m, deposit: 1_000_000m,
            Pay(PaymentType.Forfeit, 300_000m), Pay(PaymentType.DepositRefund, 700_000m));

        var b = SettlementRules.Balance(c);
        Assert.Equal(0m, b.RentDue);
        Assert.Equal(700_000m, b.RefundDue);
        Assert.True(b.IsSettled);
    }

    [Fact] // Giao dịch đã đảo bút toán (StatusCode 0) không được tính
    public void Balance_ReversedPayment_IsIgnored()
    {
        var reversed = Pay(PaymentType.Rent, 2_160_000m);
        reversed.StatusCode = 0;
        var c = Contract(ContractStatus.Active, gross: 2_400_000m, discount: 240_000m, deposit: 0m, reversed);

        Assert.Equal(2_160_000m, SettlementRules.Balance(c).OutstandingRent);
    }

    [Fact] // CSDL không có hình thức "Giữ cọc" (CK_thanhToan_phuongThuc)
    public void PaymentMethods_OnlyAcceptValuesAllowedByDatabase()
    {
        Assert.True(PaymentMethods.IsAllowed("Tiền mặt"));
        Assert.True(PaymentMethods.IsAllowed(null));
        Assert.False(PaymentMethods.IsAllowed("Giữ cọc"));
    }

    private static RentalContract Contract(ContractStatus status, decimal gross, decimal discount, decimal deposit,
        params Payment[] payments) => new()
    {
        Status = status,
        GrossAmount = gross,
        DiscountAmount = discount,
        DepositAmount = deposit,
        Payments = payments.ToList()
    };

    private static Payment Pay(PaymentType type, decimal amount) => new()
    {
        Type = type,
        Amount = amount,
        StatusCode = 1
    };
}
