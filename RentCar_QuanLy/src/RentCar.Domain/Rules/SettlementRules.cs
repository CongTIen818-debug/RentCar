using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Pricing;

namespace RentCar.Domain.Rules;

/// <summary>Khoản phát sinh khi nhận lại xe: SP-01 trả muộn, SP-02 vượt km, SP-03 hư hỏng.</summary>
public sealed record ReturnCharges(decimal Late, decimal Mileage, decimal Damage)
{
    public decimal Total => Late + Mileage + Damage;
}

/// <summary>Số tiền còn lại của hợp đồng: khoản phải thu (thuê và phát sinh) và khoản phải hoàn (cọc).</summary>
public sealed record ContractBalance(decimal RentDue, decimal RentPaid, decimal RefundDue, decimal RefundPaid)
{
    public decimal OutstandingRent => Math.Max(0, RentDue - RentPaid);
    public decimal OutstandingRefund => Math.Max(0, RefundDue - RefundPaid);

    /// <summary>UC-03 bước 7b: thu đủ và hoàn đủ thì hợp đồng chuyển sang Hoàn thành.</summary>
    public bool IsSettled => OutstandingRent == 0 && OutstandingRefund == 0;
}

/// <summary>Tính khoản phát sinh và số dư hợp đồng. Hàm thuần, kiểm thử được không cần CSDL.</summary>
public static class SettlementRules
{
    /// <summary>
    /// Tính SP-01 (trả muộn theo giá ngày đã chốt trong hợp đồng, BR-14) và SP-02 (vượt km theo số ngày thuê của hợp đồng).
    /// </summary>
    public static ReturnCharges CalculateReturnCharges(DateTime plannedEnd, DateTime returnedAt, decimal dailyRate,
        int dayCount, int drivenKm, decimal damageCharge,
        int graceMinutes = BookingRules.LateGraceMinutes,
        int kmPerDay = BookingRules.KmPerDayAllowance,
        decimal pricePerKm = BookingRules.KmUnitPrice)
    {
        if (damageCharge < 0)
            throw new DomainException("Chi phí hư hỏng không được âm (BR-02).");

        return new ReturnCharges(
            PricingCalculator.LateReturnCharge(plannedEnd, returnedAt, dailyRate, graceMinutes),
            PricingCalculator.MileageCharge(drivenKm, dayCount, kmPerDay, pricePerKm),
            damageCharge);
    }

    /// <summary>
    /// Số dư: phải thu = tổng tiền thuê và phát sinh (không tính khi đã hủy);
    /// phải hoàn = cọc trừ phần đã giữ lại khi hủy (BR-14).
    /// </summary>
    public static ContractBalance Balance(RentalContract contract)
    {
        var valid = contract.Payments.Where(p => p.StatusCode == 1).ToList();
        decimal Sum(params PaymentType[] types) => valid.Where(p => types.Contains(p.Type)).Sum(p => p.Amount);

        var extra = contract.Handover?.ExtraCharges ?? 0m;
        var rentDue = contract.Status == ContractStatus.Cancelled ? 0m : contract.TotalPayable + extra;
        var refundDue = Math.Max(0m, contract.DepositAmount - Sum(PaymentType.Forfeit));

        return new ContractBalance(
            rentDue,
            Sum(PaymentType.Rent, PaymentType.Surcharge),
            refundDue,
            Sum(PaymentType.DepositRefund));
    }
}
