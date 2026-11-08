using RentCar.Models.Database;

namespace RentCar.Models;

public record FilterOption<T>(T Value, int Count);
public record VehicleSearchResult(string Brand, string Model, string VehicleType, byte Seats,
    string Transmission, decimal DailyRate, string? ImageUrl);

public record VehicleFilterViewModel(
    IReadOnlyList<FilterOption<string>> Types,
    IReadOnlyList<FilterOption<byte>> Seats,
    string[] SelectedTypes,
    int[] SelectedSeats,
    decimal HighestPrice,
    decimal SelectedPrice,
    int ResultCount,
    IReadOnlyList<PickupLocation> Locations,
    int? PickupId,
    int? ReturnId,
    DateTime? PickupAt,
    DateTime? ReturnAt,
    bool IsSearch,
    string? SearchError,
    IReadOnlyList<VehicleSearchResult> Results);
