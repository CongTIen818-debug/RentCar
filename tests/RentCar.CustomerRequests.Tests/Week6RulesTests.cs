using RentCar.Models;
using RentCar.Services;
using RentCar.Domain.Common;
using Xunit;
public class Week6RulesTests
{
    private static VehicleSearchResult Car(decimal day=100m,decimal week=600m,decimal discount=0) => new(){OriginalRate=day,OriginalWeeklyRate=week,DailyRate=day*(1-discount),WeeklyRate=week*(1-discount)};
    [Fact]public void SevenDaysUseWeeklyRate(){var q=new RentalPricing().Quote([Car()],0,7);Assert.Equal(600m,q.Payable);}
    [Fact]public void NineDaysUseWeekAndTwoDays(){var q=new RentalPricing().Quote([Car()],0,9);Assert.Equal(800m,q.VehicleTotal);Assert.Equal(800m,q.Payable);}
    [Fact]public void NoWeeklyPriceFallsBackToDaily(){Assert.Equal(800m,new RentalPricing().Quote([Car(100,0)],0,8).Payable);}
    [Fact]public void TaxIsAlreadyIncluded(){var q=new RentalPricing().Quote([Car()],0,1);Assert.Equal(0,q.Vat);Assert.Equal(0,q.VatRate);Assert.Equal(100,q.Payable);}
    [Fact]public void BestPromotionIsNotStackedWithBulk(){var q=new RentalPricing().Quote(Enumerable.Range(0,6).Select(_=>Car(100,600,.20m)).ToArray(),0,1);Assert.Equal(120m,q.Discount);Assert.Equal(480m,q.Payable);}
    [Fact]public void SixCarsKeepSixPercent(){var q=new RentalPricing().Quote(Enumerable.Range(0,6).Select(_=>Car()).ToArray(),0,1);Assert.Equal(36m,q.Discount);}
    [Fact]public void TenCarsKeepTenPercent(){var q=new RentalPricing().Quote(Enumerable.Range(0,10).Select(_=>Car()).ToArray(),0,1);Assert.Equal(100m,q.Discount);}
    [Fact]public void DriverIsNotDiscounted(){var q=new RentalPricing().Quote([Car(100,600,.5m)],1,7);Assert.Equal(300m,q.Discount);Assert.Equal(7000000m,q.DriverTotal);Assert.Equal(7000300m,q.Payable);}
    [Fact]public void InvalidPageSizeRejected()=>Assert.Throws<DomainException>(()=>new CatalogSearch {Size=101}.Validate());
    [Fact]public void InvalidPriceRangeRejected()=>Assert.Throws<DomainException>(()=>new CatalogSearch {MinPrice=200,MaxPrice=100}.Validate());
    [Fact]public void UnknownSortRejected()=>Assert.Throws<DomainException>(()=>new CatalogSearch {Sort="sql"}.Validate());
    [Fact]public void QueryIsTrimmed(){var q=new CatalogSearch {Q="  BMW  "};q.Validate();Assert.Equal("BMW",q.Q);}
    [Fact]public void DifferentPromotionsAreNotStackedInOneQuote(){var a=Car(100,600,.20m);a.PromotionId=1;var b=Car(100,600,.30m);b.PromotionId=2;var q=new RentalPricing().Quote([a,b],0,1);Assert.Equal(30m,q.Discount);Assert.Equal(170m,q.Payable);}
}
