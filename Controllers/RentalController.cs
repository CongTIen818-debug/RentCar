using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RentalController(VehicleCatalog catalog, RentalSelection selection, RentalPricing pricing) : Controller
{
    [HttpGet]
    public Task<IActionResult> Index(DateTime? pickupAt, DateTime? returnAt, CancellationToken cancellationToken) =>
        Build(null, null, pickupAt, returnAt, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Add(int id, bool withDriver, string? intent,
        DateTime? pickupAt, DateTime? returnAt, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var car = await catalog.Query().SingleOrDefaultAsync(car => car.Id == id, cancellationToken);
        if (car is null) return NotFound();
        if (withDriver && catalog.Seats(car) <= 2)
            return BadRequest("Xe này không hỗ trợ thuê kèm tài xế.");
        selection.Add(id, withDriver);
        if (intent == "review")
            return RedirectToAction(nameof(Index), new { pickupAt, returnAt });
        TempData["CustomerMessage"] = "Đã thêm xe và lựa chọn tài xế vào danh sách thuê.";
        return RedirectToAction("Details", "Cars", new { id, pickupAt, returnAt, withDriver });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDriver(int id, bool withDriver, DateTime? pickupAt,
        DateTime? returnAt, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!selection.Read().Contains(id)) return NotFound();
        var car = await catalog.Query(false).SingleOrDefaultAsync(car => car.Id == id, cancellationToken);
        if (car is null) return NotFound();
        if (withDriver && catalog.Seats(car) <= 2)
            return BadRequest("Xe này không hỗ trợ thuê kèm tài xế.");
        selection.SetDriver(id, withDriver);
        return RedirectToAction(nameof(Index), new { pickupAt, returnAt });
    }

    [HttpPost]
    public IActionResult Remove(int id, DateTime? pickupAt, DateTime? returnAt)
    {
        selection.Remove(id);
        return RedirectToAction(nameof(Index), new { pickupAt, returnAt });
    }

    [HttpGet]
    public Task<IActionResult> Review(int? id, bool? withDriver, DateTime? pickupAt, DateTime? returnAt,
        CancellationToken cancellationToken) => Build(id, withDriver, pickupAt, returnAt, cancellationToken);

    private async Task<IActionResult> Build(int? id, bool? withDriver, DateTime? pickupAt, DateTime? returnAt,
        CancellationToken cancellationToken)
    {
        var ids = id.HasValue ? new List<int> { id.Value } : selection.Read();
        var cars = await catalog.Query(false).Where(car => ids.Contains(car.Id))
            .OrderBy(car => car.Id).ToListAsync(cancellationToken);
        if (id.HasValue && cars.Count == 0) return NotFound();
        var driverIds = selection.DriverIds();
        if (id.HasValue && withDriver.HasValue)
        {
            if (withDriver.Value) driverIds.Add(id.Value); else driverIds.Remove(id.Value);
        }
        var lines = cars.Select(car => new RentalLine(car, catalog.Seats(car) > 2,
            catalog.Seats(car) > 2 && driverIds.Contains(car.Id))).ToList();
        var error = cars.Count != ids.Count ? "Có xe không còn trong danh sách. Vui lòng chọn lại." :
            cars.Any(car => !car.IsReady) ? "Có xe hiện không sẵn sàng. Vui lòng bỏ xe đó khỏi danh sách." : null;
        var days = 0;
        if (pickupAt.HasValue || returnAt.HasValue ||
            Request.Query.ContainsKey(nameof(pickupAt)) || Request.Query.ContainsKey(nameof(returnAt)))
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
            if (!ModelState.IsValid || pickupAt is null || returnAt is null)
                error = "Vui lòng nhập đầy đủ ngày giờ nhận và trả hợp lệ.";
            else if (pickupAt.Value.Date <= today)
                error = "Ngày nhận xe phải sau ngày hôm nay.";
            else if (returnAt <= pickupAt)
                error = "Ngày giờ trả phải sau ngày giờ nhận.";
            else if (error is null && cars.Count > 0)
            {
                var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified), zone);
                var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified), zone);
                var available = await catalog.AvailableAsync(catalog.Query(), start, end, cancellationToken);
                if (await available.CountAsync(car => ids.Contains(car.Id), cancellationToken) != cars.Count)
                    error = "Có xe không còn trống trong thời gian này. Vui lòng đổi lịch hoặc chọn xe khác.";
                else days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
            }
        }
        var price = pricing.Quote(cars, lines.Count(line => line.WithDriver), Math.Max(1, days));
        return View("Index", new RentalReviewViewModel(lines, pickupAt, returnAt, days, price, error, id));
    }
}
