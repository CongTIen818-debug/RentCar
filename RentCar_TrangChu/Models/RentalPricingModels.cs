namespace RentCar.Models;

public record RentalPriceBreakdown(decimal VehicleTotal, decimal DiscountRate, decimal Discount,
    int DriverCount, decimal DriverTotal, decimal VatRate, decimal Vat, decimal Payable) { public string DiscountLabel { get; init; } = "Ưu đãi thuê xe"; }
public record RentalLine(VehicleSearchResult Car, bool CanHaveDriver, bool WithDriver);
public record RentalReviewViewModel(IReadOnlyList<RentalLine> Lines, DateTime? PickupAt, DateTime? ReturnAt,
    int Days, RentalPriceBreakdown Price, string? Error, int? SingleId = null)
{
    public IReadOnlyList<PickupLocationOption> Locations { get; init; } = [];
    public int? PickupLocationId { get; init; }
}
public record VehicleListViewModel(IReadOnlyList<VehicleSearchResult> Cars,
    DateTime? PickupAt = null, DateTime? ReturnAt = null, bool WithDriver = false);
public record ServiceFeature(string Title, string Text);
public record ServicePageViewModel(string Kind, string Title, string Subtitle, string Image,
    string Introduction, IReadOnlyList<ServiceFeature> Features, IReadOnlyList<ServiceFeature> Faq,
    string ListTitle, IReadOnlyList<VehicleSearchResult> Cars) { public int Page {get;init;}=1; public int Pages {get;init;}=1; public int Total {get;init;} }
