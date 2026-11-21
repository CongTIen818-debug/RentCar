namespace RentCar.Models;

public record RentalPriceBreakdown(decimal VehicleTotal, decimal DiscountRate, decimal Discount,
    int DriverCount, decimal DriverTotal, decimal VatRate, decimal Vat, decimal Payable);
public record RentalLine(VehicleSearchResult Car, bool CanHaveDriver, bool WithDriver);
public record RentalReviewViewModel(IReadOnlyList<RentalLine> Lines, DateTime? PickupAt, DateTime? ReturnAt,
    int Days, RentalPriceBreakdown Price, string? Error, int? SingleId = null);
public record VehicleListViewModel(IReadOnlyList<VehicleSearchResult> Cars,
    DateTime? PickupAt = null, DateTime? ReturnAt = null, bool WithDriver = false);
public record ServiceFeature(string Title, string Text);
public record ServicePageViewModel(string Kind, string Title, string Subtitle, string Image,
    string Introduction, IReadOnlyList<ServiceFeature> Features, IReadOnlyList<ServiceFeature> Faq,
    string ListTitle, IReadOnlyList<VehicleSearchResult> Cars);
