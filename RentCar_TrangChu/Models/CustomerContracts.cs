using System.Globalization;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
using RentCar.Application.Rentals;

namespace RentCar.Models;

public static class CustomerContractDisplay
{
    public static string Money(decimal amount) => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    // Database and domain both use Vietnam wall-clock time; display directly without adding hours.
    public static string When(DateTime date) => date.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    public static string Label(ContractStatus status) => status switch
    {
        ContractStatus.PendingConfirmation => "Chờ xác nhận", ContractStatus.Confirmed => "Đã xác nhận",
        ContractStatus.Active => "Đang thuê", ContractStatus.Completed => "Hoàn thành", ContractStatus.Cancelled => "Đã hủy", _ => "Chưa xác định"
    };
    public static string Tone(ContractStatus status) => status switch
    { ContractStatus.PendingConfirmation => "pending", ContractStatus.Active => "active", ContractStatus.Cancelled => "cancelled", _ => "approved" };
    public static string PaymentLabel(PaymentType type) => type switch
    { PaymentType.Deposit => "Thu cọc", PaymentType.Rent => "Tiền thuê", PaymentType.Surcharge => "Phụ phí", PaymentType.DepositRefund => "Hoàn cọc", PaymentType.Forfeit => "Giữ cọc / phí hủy", _ => "Giao dịch" };
}

public sealed class CustomerContractMoney(RentalContract contract)
{
    private decimal Sum(PaymentType type) => contract.Payments.Where(p => p.StatusCode == 1 && p.Type == type).Sum(p => p.Amount);
    public decimal DepositPaid => Sum(PaymentType.Deposit);
    public decimal DepositRequired => contract.DepositAmount;
    public decimal DepositMissing => Math.Max(0, DepositRequired - DepositPaid);
    public decimal RentPaid => Sum(PaymentType.Rent) + Sum(PaymentType.Surcharge);
    public decimal ExtraCharges => contract.Handover?.ExtraCharges ?? 0;
    public decimal OutstandingRent => SettlementRules.Balance(contract).OutstandingRent;
    public decimal Retained => Sum(PaymentType.Forfeit);
    public decimal RefundPaid => Sum(PaymentType.DepositRefund);
    public decimal DepositHeld => Math.Max(0, DepositPaid - Retained - RefundPaid);
    public bool CanSettleRefund => contract.Status == ContractStatus.Cancelled || contract.Handover?.ReturnedAt is not null;
    public decimal RefundOutstanding => CanSettleRefund ? DepositHeld : 0;
}
public sealed record CustomerContractCard(RentalContract Contract)
{
    public CustomerContractMoney Money => new(Contract);
    public bool Overdue => Contract.Status == ContractStatus.Active && Contract.EndAt <= VietnamTime.Now && Contract.Handover?.ReturnedAt is null;
}
public sealed record CustomerContractCount(ContractStatus Status, int Count);
public sealed record CustomerContractsPage(IReadOnlyList<CustomerContractCard> Items, IReadOnlyList<CustomerContractCount> Counts,
    int Total, int Page, int Pages, ContractStatus? Status, string? Query, string? Error = null);
public sealed record CustomerContractDetails(RentalContract Contract)
{
    public CustomerContractMoney Money => new(Contract);
}
public sealed record CustomerDepositReceipt(RentalContract Contract, IReadOnlyList<DepositReceiptLine> Lines);
