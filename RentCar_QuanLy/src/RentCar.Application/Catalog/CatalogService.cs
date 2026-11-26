using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Entities;
using RentCar.Domain.Pricing;
using RentCar.Domain.Rules;

namespace RentCar.Application.Catalog;

public sealed record CatalogFilter(
    DateTime StartAt, DateTime EndAt, int? VehicleTypeId = null, int? MinSeats = null,
    string? Transmission = null, string? FuelType = null, decimal? MaxDailyRate = null,
    int Page = 1, int PageSize = 12, string? Keyword = null);

public sealed record VehicleCard(
    int Id, string Name, string Brand, string? Model, int ModelYear, int Seats, string Transmission,
    string FuelType, string TypeName, string? ImagePath, decimal DailyRate, decimal DepositAmount, PriceQuote Quote);

public sealed record CatalogPage(IReadOnlyList<VehicleCard> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record VehicleDetail(Vehicle Vehicle, bool IsAvailable, PriceQuote? Quote);

/// <summary>FR-2.1 đến FR-2.4: tra cứu xe khả dụng, lọc, xem chi tiết và báo giá dự kiến.</summary>
public sealed class CatalogService(IAppDbContext db)
{
    public async Task<CatalogPage> SearchAsync(CatalogFilter f, CancellationToken ct = default)
    {
        BookingRules.ValidateWindow(f.StartAt, f.EndAt);
        var availableIds = await db.AvailableVehicleIdsAsync(f.StartAt, f.EndAt, ct);

        IQueryable<Vehicle> query = db.Vehicles.AsNoTracking()
            .Include(v => v.VehicleType)
            .Where(v => availableIds.Contains(v.Id));

        if (f.VehicleTypeId is { } typeId) query = query.Where(v => v.VehicleTypeId == typeId);
        // Kẹp về 1-255 (đúng miền của cột TINYINT soCho) để giá trị lọc quá lớn không gây lỗi tràn số.
        if (f.MinSeats is { } seats) query = query.Where(v => v.Seats >= Math.Clamp(seats, 1, 255));
        if (!string.IsNullOrWhiteSpace(f.Transmission)) query = query.Where(v => v.Transmission == f.Transmission);
        if (!string.IsNullOrWhiteSpace(f.FuelType)) query = query.Where(v => v.FuelType == f.FuelType);
        if (f.MaxDailyRate is { } maxRate) query = query.Where(v => v.DailyRate <= maxRate);
        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var keyword = f.Keyword.Trim();
            query = query.Where(v => v.Name.Contains(keyword) || v.Brand.Contains(keyword));
        }

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 50);

        var vehicles = await query
            .OrderBy(v => v.DailyRate).ThenBy(v => v.Name)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        var typeIds = vehicles.Select(v => v.VehicleTypeId).Distinct().ToList();
        var factors = await GetPriceFactorsAsync(typeIds, f.StartAt, ct);

        var items = vehicles
            .Select(v => ToCard(v, f.StartAt, f.EndAt, factors.GetValueOrDefault(v.VehicleTypeId, 1m)))
            .ToList();

        return new CatalogPage(items, total, page, size);
    }

    public async Task<VehicleDetail?> GetDetailAsync(int id, DateTime? startAt, DateTime? endAt, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.VehicleType)
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vehicle is null) return null;
        if (startAt is null || endAt is null) return new VehicleDetail(vehicle, false, null);

        var start = startAt.Value;
        var end = endAt.Value;
        BookingRules.ValidateWindow(start, end);

        var available = (await db.AvailableVehicleIdsAsync(start, end, ct)).Contains(id);
        var factors = await GetPriceFactorsAsync([vehicle.VehicleTypeId], start, ct);
        var quote = PricingCalculator.Quote(start, end, vehicle.DailyRate, vehicle.WeeklyRate,
            factors.GetValueOrDefault(vehicle.VehicleTypeId, 1m));

        return new VehicleDetail(vehicle, available, quote);
    }

    /// <summary>BR-10a: hệ số bảng giá có hiệu lực tại thời điểm nhận xe, theo loại xe.</summary>
    private async Task<Dictionary<int, decimal>> GetPriceFactorsAsync(List<int> typeIds, DateTime at, CancellationToken ct)
    {
        var rules = await db.PriceRules.AsNoTracking()
            .Where(r => typeIds.Contains(r.VehicleTypeId) && r.IsActive && r.StartAt <= at && at < r.EndAt)
            .ToListAsync(ct);

        return rules.GroupBy(r => r.VehicleTypeId).ToDictionary(g => g.Key, g => g.First().Factor);
    }

    private static VehicleCard ToCard(Vehicle v, DateTime start, DateTime end, decimal factor)
    {
        var quote = PricingCalculator.Quote(start, end, v.DailyRate, v.WeeklyRate, factor);
        return new VehicleCard(v.Id, v.Name, v.Brand, v.Model, v.ModelYear, v.Seats, v.Transmission, v.FuelType,
            v.VehicleType.Name, v.PrimaryImagePath, v.DailyRate, v.DepositAmount, quote);
    }
}
