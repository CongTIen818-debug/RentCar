using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
using Xunit;
namespace RentCar.Tests;
public class MultiVehicleTests
{
    [Fact]
    public void Clock_DoesNotRoundAJustReturnedVehicleIntoFutureSqlSecond()
    {
        Assert.Equal(0, VietnamTime.Now.Ticks % TimeSpan.TicksPerSecond);
    }
    [Fact]
    public void VietnamNineAm_RoundTripsAsTwoAmUtc()
    {
        var local = new DateTime(2026, 12, 1, 9, 0, 0);
        Assert.Equal(new DateTime(2026,12,1,2,0,0,DateTimeKind.Utc), VietnamTime.ToUtc(local));
        Assert.Equal(local, VietnamTime.FromUtc(VietnamTime.ToUtc(local)));
    }
    [Fact]
    public void VietnamMidnight_UsesPreviousUtcDate()
    {
        Assert.Equal(new DateTime(2025,12,31,17,0,0,DateTimeKind.Utc), VietnamTime.ToUtc(new DateTime(2026,1,1)));
    }
    [Fact]
    public void Confirmation_RejectsMaintenanceOnSecondVehicle()
    {
        var contract = new RentalContract
        {
            StartAt = new DateTime(2026,12,1), EndAt = new DateTime(2026,12,3),
            Customer = new User { FullName="Nguyễn An",Phone="0912345678",Email="an@rentcar.vn",IdNumber="012345678901",BirthDate=new DateOnly(1990,1,1) },
            Lines = [
                new ContractLine { WithDriver=true, Vehicle=new Vehicle {Status=VehicleStatus.Ready} },
                new ContractLine { WithDriver=true, Vehicle=new Vehicle {Status=VehicleStatus.Maintenance} }
            ]
        };
        Assert.False(ConfirmationRules.CanConfirm(ConfirmationRules.Evaluate(contract,0,false)));
    }
    [Fact]
    public void Total_UsesVehicleDiscountThenDriverFeeThenVat()
    {
        var contract = new RentalContract {GrossAmount=100_000_000m,DiscountAmount=10_000_000m,DriverAmount=6_000_000m,VatRate=.08m};
        Assert.Equal(7_680_000m,contract.VatAmount);
        Assert.Equal(103_680_000m,contract.TotalPayable);
    }
}
