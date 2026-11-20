namespace RentCar.Models;

public record FilterOption<T>(T Value, int Count);

public class VehicleSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Brand { get; set; }
    public int? Year { get; set; }
    public bool IsReady { get; set; }
    public string VehicleType { get; set; } = "";
    public string? Color { get; set; }
    public string? Edition { get; set; }
    public decimal OriginalRate { get; set; }
    public decimal DailyRate { get; set; }
    public string? ImageUrl { get; set; }
}

public record VehicleFilterViewModel(
    IReadOnlyList<FilterOption<string>> Types,
    string[] SelectedTypes,
    decimal HighestPrice,
    decimal SelectedPrice,
    int ResultCount,
    DateTime? PickupAt,
    DateTime? ReturnAt,
    bool IsSearch,
    string? SearchError,
    IReadOnlyList<VehicleSearchResult> Results);
