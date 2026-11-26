using RentCar.Models;
using Xunit;
namespace RentCar.CustomerRequests.Tests;
public class RequestRulesTests
{
    private static readonly DateTime Deadline = new(2026,12,1,2,0,0,DateTimeKind.Utc);
    [Theory]
    [InlineData(1,"Đang chờ duyệt")]
    [InlineData(2,"Đã duyệt")]
    [InlineData(3,"Đã hủy")]
    public void OnlyThreeRequestLabels(int status,string expected) => Assert.Equal(expected,CustomerRequestRules.Label(status));
    [Fact] public void ApprovedBeforeDeadline_CanPay() => Assert.True(CustomerRequestRules.CanPay(2,false,Deadline,Deadline.AddSeconds(-1),2));
    [Fact] public void AtExactDeadline_CannotPay() => Assert.False(CustomerRequestRules.CanPay(2,false,Deadline,Deadline,2));
    [Fact] public void AfterDeadline_RemainsApprovedButExpired()
    {
        Assert.Equal("Đã duyệt",CustomerRequestRules.Label(2));
        Assert.True(CustomerRequestRules.IsExpired(2,false,Deadline,Deadline.AddDays(2)));
    }
    [Theory][InlineData(1)][InlineData(3)] public void NonApproved_CannotPay(int status) => Assert.False(CustomerRequestRules.CanPay(status,false,Deadline,Deadline.AddSeconds(-1),2));
    [Fact] public void DepositAlreadyPaid_DoesNotExpireAfter24Hours()
    {
        Assert.False(CustomerRequestRules.IsExpired(2,true,Deadline,Deadline.AddDays(2)));
        Assert.False(CustomerRequestRules.CanPay(2,true,Deadline,Deadline.AddSeconds(-1),2));
    }
    [Fact] public void EmptyVehicleList_CannotPay() => Assert.False(CustomerRequestRules.CanPay(2,false,Deadline,Deadline.AddSeconds(-1),0));
    [Fact] public void DisplayUsesVietnamTimezone() => Assert.Equal("01/12/2026 09:00",CustomerRequestCard.When(Deadline));
}
