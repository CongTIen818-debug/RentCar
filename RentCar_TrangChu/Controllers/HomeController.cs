using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Controllers;

public class HomeController(
    RentCar.Services.VehicleCatalog catalog, RentCarDbContext database,
    RentCar.Services.RentalSelection selection, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string[] brands, string[] types, int[] seats, decimal? maxPrice,
        DateTime? pickupAt, DateTime? returnAt, int? locationId, CancellationToken cancellationToken)
    {
        if (maxPrice is < 0 || (ModelState.TryGetValue(nameof(maxPrice), out var priceState) && priceState.Errors.Count > 0))
            return BadRequest("Giá lọc không hợp lệ.");

        if (seats.Any(value => value < 1 || value > 99) ||
            (ModelState.TryGetValue(nameof(seats), out var seatState) && seatState.Errors.Count > 0))
            return BadRequest("Số chỗ lọc không hợp lệ.");
        brands = brands.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        types = types.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
        seats = seats.Distinct().ToArray();

        var isSearch = pickupAt.HasValue || returnAt.HasValue ||
            !string.IsNullOrWhiteSpace(Request.Query[nameof(pickupAt)]) ||
            !string.IsNullOrWhiteSpace(Request.Query[nameof(returnAt)]);
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = DateTime.UtcNow;
        var today = TimeZoneInfo.ConvertTimeFromUtc(now, vietnamTimeZone).Date;
        string? error = null;
        if (isSearch)
        {
            if (!ModelState.IsValid || pickupAt is null || returnAt is null)
                error = "Vui lòng nhập đầy đủ ngày giờ nhận và trả hợp lệ.";
            else if (pickupAt.Value.Date <= today)
                error = "Ngày nhận xe phải sau ngày hôm nay.";
            else if (returnAt <= pickupAt)
                error = "Ngày giờ trả phải sau ngày giờ nhận xe.";
            else if ((returnAt.Value - pickupAt.Value).TotalHours < 4 || (returnAt.Value - pickupAt.Value).TotalDays > 30)
                error = "Thời gian thuê cần từ 4 giờ đến 30 ngày.";
        }
        try
        {
            var locations = await database.Database.SqlQuery<PickupLocationOption>($"""
                SELECT idDiem AS Id, tenDiem AS Name, diaChi AS Address
                FROM dbo.diemGiaoNhan
                WHERE trangThai=1 AND tenDiem<>N'Chưa xác định - hợp đồng cũ'
                """).OrderBy(location => location.Name).ToListAsync(cancellationToken);
            // Query parameter takes priority; otherwise retain the customer's last search location.
            var selectedLocation = Request.Query.ContainsKey(nameof(locationId)) ? locationId : selection.LocationId();
            if (ModelState.TryGetValue(nameof(locationId), out var locationState) && locationState.Errors.Count > 0)
                error = "Điểm giao nhận không hợp lệ. Vui lòng chọn lại.";
            else if (selectedLocation.HasValue && !locations.Any(location => location.Id == selectedLocation))
            {
                error = "Điểm giao nhận không còn hoạt động. Vui lòng chọn lại.";
                selectedLocation = null;
            }
            else if (isSearch && selectedLocation is null)
                error = "Vui lòng chọn điểm giao nhận chung cho nhận và trả xe.";
            if (error is null && selectedLocation.HasValue)
                selection.SetLocation(selectedLocation.Value);

            // Promotions are selected by booking time. Prefer the lowest applicable rate
            // if management data contains overlapping promotions.
            var cars = catalog.Query();

            if (isSearch && error is null)
            {
                var start = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(pickupAt!.Value, DateTimeKind.Unspecified), vietnamTimeZone);
                var end = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(returnAt!.Value, DateTimeKind.Unspecified), vietnamTimeZone);
                cars = await catalog.AvailableAsync(cars, start, end, cancellationToken);
            }

            // Seat counts currently live in vehicle profiles. Load the available fleet once,
            // then combine the brand, type, seat and price selections on that same fleet.
            var fleet = await cars.ToListAsync(cancellationToken);
            var seatCounts = fleet.ToDictionary(car => car.Id, car => catalog.Seats(car));
            var brandOptions = fleet.Where(car => !string.IsNullOrWhiteSpace(car.Brand))
                .GroupBy(car => car.Brand!, StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key)
                .Select(group => new FilterOption<string>(group.Key, group.Count())).ToList();
            var options = fleet.GroupBy(car => car.VehicleType).OrderBy(group => group.Key)
                .Select(group => new FilterOption<string>(group.Key, group.Count())).ToList();
            var seatOptions = fleet.Where(car => seatCounts[car.Id] > 0)
                .GroupBy(car => seatCounts[car.Id]).OrderBy(group => group.Key)
                .Select(group => new FilterOption<int>(group.Key, group.Count())).ToList();
            var highest = fleet.Select(car => car.DailyRate).DefaultIfEmpty(0).Max();
            var selected = Math.Min(maxPrice ?? highest, highest);
            IEnumerable<VehicleSearchResult> filtered = fleet;
            if (brands.Length > 0)
                filtered = filtered.Where(car => car.Brand is not null &&
                    brands.Contains(car.Brand, StringComparer.OrdinalIgnoreCase));
            if (types.Length > 0)
                filtered = filtered.Where(car => types.Contains(car.VehicleType));
            if (seats.Length > 0)
                filtered = filtered.Where(car => seats.Contains(seatCounts[car.Id]));
            filtered = filtered.Where(car => car.DailyRate <= selected);
            var results = error is null
                ? (isSearch ? filtered.OrderBy(car => car.DailyRate).ThenBy(car => car.Id)
                    : filtered.OrderBy(car => car.Id)).ToList()
                : [];
            return View(new VehicleFilterViewModel(brandOptions, brands, seatOptions, seats,
                options, types, highest, selected, results.Count, pickupAt, returnAt, isSearch, error, results)
            { Locations = locations, LocationId = selectedLocation });
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "Could not load the vehicle catalogue.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View();
        }
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

