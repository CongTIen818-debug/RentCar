namespace RentCar.Models;

public record FilterOption<T>(T Value, int Count);

public class VehicleSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Brand { get; set; }
    public int? Year { get; set; }
    public bool IsReady { get; set; }
    public int Status { get; set; }
    public int Seats { get; set; }
    public string Transmission { get; set; } = "";
    public string Fuel { get; set; } = "";
    public decimal WeeklyRate { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string VehicleType { get; set; } = "";
    public string? Color { get; set; }
    public string? Edition { get; set; }
    public decimal OriginalWeeklyRate { get; set; }
    public int? PromotionId { get; set; }
    public decimal PromotionPercent { get; set; }
    public decimal OriginalRate { get; set; }
    public decimal DailyRate { get; set; }
    public string? ImageUrl { get; set; }
}

public record VehicleFilterViewModel(
    IReadOnlyList<FilterOption<string>> Brands,
    string[] SelectedBrands,
    IReadOnlyList<FilterOption<int>> Seats,
    int[] SelectedSeats,
    IReadOnlyList<FilterOption<string>> Types,
    string[] SelectedTypes,
    decimal HighestPrice,
    decimal SelectedPrice,
    int ResultCount,
    DateTime? PickupAt,
    DateTime? ReturnAt,
    bool IsSearch,
    string? SearchError,
    IReadOnlyList<VehicleSearchResult> Results)
{
    public IReadOnlyList<PickupLocationOption> Locations { get; init; } = [];
    public int? LocationId { get; init; }
    public CatalogSearch Search { get; init; } = new();
    public int Page { get; init; } = 1;
    public int Pages { get; init; } = 1;
    public IReadOnlyList<FilterOption<string>> Transmissions { get; init; } = [];
    public IReadOnlyList<FilterOption<string>> Fuels { get; init; } = [];
}

public sealed class PickupLocationOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
}

public sealed record PickupLocationSelection(IReadOnlyList<PickupLocationOption> Locations, int? SelectedId);
