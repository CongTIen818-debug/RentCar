namespace RentCar.Models;

public record VehicleDetailsViewModel(
    VehicleSearchResult Car, IReadOnlyList<string> Images, VehicleProfile? Profile,
    DateTime? PickupAt, DateTime? ReturnAt, bool WithDriver = false, bool CanHaveDriver = false)
{
    public IReadOnlyList<PickupLocationOption> Locations { get; init; } = [];
    public int? PickupLocationId { get; init; }
}

public class VehicleProfile
{
    public string Name { get; set; } = "";
    public string Edition { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Specifications { get; set; } = [];
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public class VehiclePhoto
{
    public string ImageUrl { get; set; } = "";
}

