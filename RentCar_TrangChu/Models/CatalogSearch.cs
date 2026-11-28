using RentCar.Domain.Common;
namespace RentCar.Models;
public sealed class CatalogSearch
{
    public string? Q { get; set; }
    public string[] Brands { get; set; } = [];
    public string[] Types { get; set; } = [];
    public int[] Seats { get; set; } = [];
    public string[] Transmissions { get; set; } = [];
    public string[] Fuels { get; set; } = [];
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? PickupAt { get; set; }
    public DateTime? ReturnAt { get; set; }
    public int? LocationId { get; set; }
    public string Sort { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 12;
    public void Validate()
    {
        if (Q?.Length > 100 || Brands.Length > 30 || Types.Length > 30 || Transmissions.Length > 20 || Fuels.Length > 20 ||
            Seats.Any(s => s < 1 || s > 99) || MinPrice is < 0 || MaxPrice is < 0 || MinPrice > MaxPrice || Size < 1 || Size > 100 || Page < 1 ||
            !new[] {"default","price-asc","price-desc","name","seats"}.Contains(Sort))
            throw new DomainException("Bộ lọc tìm xe không hợp lệ.");
        Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
    }
}
public sealed record CatalogPage(IReadOnlyList<VehicleSearchResult> Cars, int Total, int Page, int Pages,
    decimal HighestPrice, IReadOnlyList<FilterOption<string>> Brands, IReadOnlyList<FilterOption<string>> Types,
    IReadOnlyList<FilterOption<int>> Seats, IReadOnlyList<FilterOption<string>> Transmissions, IReadOnlyList<FilterOption<string>> Fuels);
