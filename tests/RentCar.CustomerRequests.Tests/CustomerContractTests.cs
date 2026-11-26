using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Models;
using Xunit;

public class CustomerContractTests
{
    private static Payment Paid(PaymentType type, decimal amount, int status = 1) => new() { Type = type, Amount = amount, StatusCode = status };
    [Fact] public void DeclaredDepositIsNotSuccessfulPayment()
    {
        var money = new CustomerContractMoney(new() { DepositAmount = 5000000 });
        Assert.Equal(0, money.DepositPaid); Assert.Equal(5000000, money.DepositMissing); Assert.Equal(0, money.DepositHeld);
    }
    [Fact] public void ReversedPaymentsAreExcluded()
    {
        var c = new RentalContract { DepositAmount = 5000000, Payments = [Paid(PaymentType.Deposit,5000000),Paid(PaymentType.Deposit,2000000,0),Paid(PaymentType.Rent,1000000),Paid(PaymentType.Rent,9000000,0)] };
        var m = new CustomerContractMoney(c); Assert.Equal(5000000,m.DepositPaid); Assert.Equal(1000000,m.RentPaid);
    }
    [Fact] public void DepositDoesNotPayRentAutomatically()
    {
        var c = new RentalContract { GrossAmount = 10000000, DepositAmount = 5000000, Payments = [Paid(PaymentType.Deposit,5000000)] };
        Assert.Equal(10000000,new CustomerContractMoney(c).OutstandingRent);
    }
    [Fact] public void CancellationRefundUsesCapturedDepositOnly()
    {
        var c = new RentalContract { Status = ContractStatus.Cancelled,DepositAmount = 10000000,Payments = [Paid(PaymentType.Deposit,5000000),Paid(PaymentType.Forfeit,1500000),Paid(PaymentType.DepositRefund,1000000)] };
        var m=new CustomerContractMoney(c); Assert.Equal(2500000,m.RefundOutstanding); Assert.Equal(0,m.OutstandingRent);
    }
    [Fact] public void NoRefundForUnpaidDeposit()
    { Assert.Equal(0,new CustomerContractMoney(new() {Status=ContractStatus.Cancelled,DepositAmount=5000000}).RefundOutstanding); }
    [Fact] public void ActiveContractHoldsDepositUntilReturn()
    {
        var m=new CustomerContractMoney(new() {Status=ContractStatus.Active,Payments=[Paid(PaymentType.Deposit,5000000)]});
        Assert.False(m.CanSettleRefund); Assert.Equal(0,m.RefundOutstanding); Assert.Equal(5000000,m.DepositHeld);
    }
    [Fact] public void ReturnedContractShowsPendingRefund()
    {
        var m=new CustomerContractMoney(new() {Status=ContractStatus.Active,Handover=new() {ReturnedAt=new DateTime(2026,11,26)},Payments=[Paid(PaymentType.Deposit,5000000)]});
        Assert.Equal(5000000,m.RefundOutstanding);
    }
    [Fact] public void SettlementIncludesSavedSurcharge()
    {
        var c=new RentalContract {GrossAmount=10000000,Handover=new() {ExtraCharges=200000},Payments=[Paid(PaymentType.Rent,9000000),Paid(PaymentType.Surcharge,100000)]};
        Assert.Equal(1100000,new CustomerContractMoney(c).OutstandingRent);
    }
    [Fact] public void CurrentVehicleRateDoesNotRepriceContract()
    {
        var c=new RentalContract {GrossAmount=10000000,Lines=[new() {DailyRateAtBooking=5000000,DayCount=2,Vehicle=new() {DailyRate=99000000}}]};
        Assert.Equal(10000000,c.TotalPayable);Assert.Equal(10000000,c.Lines[0].GrossLine);
    }
    [Fact] public void TaxAlreadyIncludedDoesNotGainEightPercent()
    { Assert.Equal(10000000,new RentalContract {GrossAmount=10000000,VatRate=0}.TotalPayable); }
    [Fact] public void LegacySavedTaxIsRetained()
    { Assert.Equal(10800000,new RentalContract {GrossAmount=10000000,VatRate=.08m}.TotalPayable); }
    [Fact] public void VietnamDateIsNotConvertedTwice()
    { Assert.Equal("26/11/2026 09:00",CustomerContractDisplay.When(new DateTime(2026,11,26,9,0,0))); }
}
