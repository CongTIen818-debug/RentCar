using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RentalController(VehicleCatalog catalog, RentalSelection selection) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var ids = selection.Read();
        var cars = await catalog.Query(false).Where(car => ids.Contains(car.Id))
            .OrderBy(car => car.Id).ToListAsync(cancellationToken);
        return View(cars);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int id, DateTime? pickupAt, DateTime? returnAt,
        CancellationToken cancellationToken)
    {
        if (!await catalog.Query().AnyAsync(car => car.Id == id, cancellationToken))
            return NotFound();
        selection.Add(id);
        TempData["CustomerMessage"] = "Đã thêm xe vào danh sách thuê.";
        return RedirectToAction("Details", "Cars", new { id, pickupAt, returnAt });
    }

    [HttpPost]
    public IActionResult Remove(int id)
    {
        selection.Remove(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Review(int? id, DateTime? pickupAt, DateTime? returnAt,
        CancellationToken cancellationToken)
    {
        var ids = id.HasValue ? new List<int> { id.Value } : selection.Read();
        var cars = await catalog.Query(false).Where(car => ids.Contains(car.Id))
            .OrderBy(car => car.Id).ToListAsync(cancellationToken);
        if (id.HasValue && cars.Count == 0)
            return NotFound();

        var error = cars.Count == 0 || cars.Count != ids.Count ? "Vui lòng chọn xe trong danh sách thuê." : null;
        var days = 0;
        var total = 0m;
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
            else if (error is null)
            {
                var start = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified), zone);
                var end = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified), zone);
                var available = await catalog.AvailableAsync(catalog.Query(), start, end, cancellationToken);
                var count = await available.CountAsync(car => ids.Contains(car.Id), cancellationToken);
                if (count != cars.Count)
                    error = "Có xe không còn trống trong thời gian này. Vui lòng đổi lịch hoặc chọn xe khác.";
                else
                {
                    days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
                    total = cars.Sum(car => car.DailyRate * days);
                }
            }
        }
        return View(new RentalReviewViewModel(cars, pickupAt, returnAt, days, total, error));
    }
}

