using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Controllers;

public class HomeController(
    RentCar.Services.VehicleCatalog catalog, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string[] types, decimal? maxPrice,
        DateTime? pickupAt, DateTime? returnAt, CancellationToken cancellationToken)
    {
        if (maxPrice is < 0 || (ModelState.TryGetValue(nameof(maxPrice), out var priceState) && priceState.Errors.Count > 0))
            return BadRequest("Giá lọc không hợp lệ.");

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
        }
        try
        {
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

            var options = await cars.GroupBy(car => car.VehicleType).OrderBy(group => group.Key)
                .Select(group => new FilterOption<string>(group.Key, group.Count()))
                .ToListAsync(cancellationToken);
            var highest = await cars.MaxAsync(car => (decimal?)car.DailyRate, cancellationToken) ?? 0;
            var selected = Math.Min(maxPrice ?? highest, highest);
            if (types.Length > 0)
                cars = cars.Where(car => types.Contains(car.VehicleType));
            cars = cars.Where(car => car.DailyRate <= selected);
            var count = await cars.CountAsync(cancellationToken);
            var results = error is null
                ? await cars.OrderBy(car => car.DailyRate).ThenBy(car => car.Id)
                    .Take(12).ToListAsync(cancellationToken)
                : [];
            return View(new VehicleFilterViewModel(options, types, highest, selected,
                count, pickupAt, returnAt, isSearch, error, results));
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

