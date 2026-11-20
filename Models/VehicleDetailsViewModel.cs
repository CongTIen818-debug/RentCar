namespace RentCar.Models;

public record VehicleDetailsViewModel(
    VehicleSearchResult Car, IReadOnlyList<string> Images, VehicleProfile? Profile,
    DateTime? PickupAt, DateTime? ReturnAt);

public class VehicleProfile
{
    public string Name { get; set; } = "";
    public string Edition { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Specifications { get; set; } = [];
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public record RentalReviewViewModel(
    IReadOnlyList<VehicleSearchResult> Cars, DateTime? PickupAt, DateTime? ReturnAt,
    int Days, decimal Total, string? Error);

public class VehiclePhoto
{
    public string ImageUrl { get; set; } = "";
}

