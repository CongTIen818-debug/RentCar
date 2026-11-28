using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Services;

public class RentalPricing(RentCarDbContext? database = null)
{
    public const decimal DriverDailyRate = 1_000_000m;
    public decimal VatRate => 0m; // SRS: displayed rental prices already include tax.

    public RentalPriceBreakdown Quote(IReadOnlyList<VehicleSearchResult> cars,int driverCount,int days) =>
        Build(cars,driverCount,days,cars.GroupBy(c=>c.PromotionId??0).Select(g=>(Amount:g.Sum(c=>Rental(c.OriginalRate,c.OriginalWeeklyRate,days)-Rental(c.DailyRate,c.WeeklyRate,days)),Name:"Ưu đãi xe đang áp dụng")).ToList());

    public async Task<RentalPriceBreakdown> QuoteAsync(IReadOnlyList<VehicleSearchResult> cars,int driverCount,int days,DateTime atVietnam,CancellationToken ct)
    {
        if(database is null)return Quote(cars,driverCount,days);
        var ids=cars.Select(c=>c.Id).ToArray();
        var discounts=await database.Database.SqlQuery<CampaignDiscount>($"""
            SELECT u.idUuDai CampaignId,x.idXe VehicleId,u.tenUuDai Name,u.phanTramGiam PercentOff
            FROM dbo.uuDai u JOIN dbo.uuDaiXe x ON x.idUuDai=u.idUuDai
            WHERE u.trangThai=1 AND u.ngayBatDau<={atVietnam} AND {atVietnam}<u.ngayKetThuc
            """).Where(d=>ids.Contains(d.VehicleId)).ToListAsync(ct);
        var candidates=discounts.GroupBy(d=>d.CampaignId).Select(g=>(Amount:g.Sum(d=>{var car=cars.Single(c=>c.Id==d.VehicleId);return Rental(car.OriginalRate,car.OriginalWeeklyRate,days)*d.PercentOff/100m;}),Name:g.First().Name)).ToList();
        return Build(cars,driverCount,days,candidates);
    }
    private static decimal Rental(decimal daily,decimal weekly,int days) => days>=7&&weekly>0?days/7*weekly+days%7*daily:days*daily;
    private static RentalPriceBreakdown Build(IReadOnlyList<VehicleSearchResult> cars,int driverCount,int days,IReadOnlyList<(decimal Amount,string Name)> candidates)
    {
        if(days<1||driverCount<0||driverCount>cars.Count)throw new ArgumentOutOfRangeException(nameof(days));
        var gross=cars.Sum(c=>Rental(c.OriginalRate,c.OriginalWeeklyRate,days));
        var promotion=candidates.OrderByDescending(c=>c.Amount).FirstOrDefault();
        var bulkRate=cars.Count>=10?.10m:cars.Count>=6?.06m:0m;
        var bulkDiscount=decimal.Round(gross*bulkRate,0,MidpointRounding.AwayFromZero);
        var discount=decimal.Round(Math.Max(promotion.Amount,bulkDiscount),0,MidpointRounding.AwayFromZero);
        var driver=driverCount*DriverDailyRate*days;
        return new(gross,gross==0?0:discount/gross,discount,driverCount,driver,0,0,gross-discount+driver)
        {DiscountLabel=bulkDiscount>=promotion.Amount&&bulkRate>0?$"Ưu đãi thuê từ {(cars.Count>=10?10:6)} xe ({bulkRate*100:0}%)":promotion.Name??"Ưu đãi thuê xe"};
    }
    public sealed class CampaignDiscount
    {public int CampaignId {get;set;}public int VehicleId {get;set;}public string Name {get;set;}="";public decimal PercentOff {get;set;}}

    public static RentalPriceBreakdown Calculate(IReadOnlyList<decimal> rates, int driverCount, int days, decimal vatRate)
    {
        if (days < 1 || driverCount < 0 || driverCount > rates.Count || vatRate < 0 || vatRate > 1 || rates.Any(rate => rate < 0))
            throw new ArgumentOutOfRangeException(nameof(days));
        var vehicleTotal = rates.Sum() * days;
        var discountRate = rates.Count >= 10 ? .10m : rates.Count >= 6 ? .06m : 0m;
        var discount = decimal.Round(vehicleTotal * discountRate, 0, MidpointRounding.AwayFromZero);
        var driverTotal = driverCount * DriverDailyRate * days;
        var taxable = vehicleTotal - discount + driverTotal;
        var vat = decimal.Round(taxable * vatRate, 0, MidpointRounding.AwayFromZero);
        return new(vehicleTotal, discountRate, discount, driverCount, driverTotal, vatRate, vat, taxable + vat);
    }
}
