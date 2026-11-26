using RentCar.Models;

namespace RentCar.Services;

public class RentalPricing(IConfiguration configuration)
{
    public const decimal DriverDailyRate = 1_000_000m;
    public decimal VatRate => configuration.GetValue<decimal>("RentalPricing:VatRate", .08m);

    public RentalPriceBreakdown Quote(IReadOnlyList<VehicleSearchResult> cars, int driverCount, int days) =>
        Calculate(cars.Select(car => car.DailyRate).ToArray(), driverCount, days, VatRate);

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
