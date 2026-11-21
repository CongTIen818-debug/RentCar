using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;
using System.Security.Claims;
using RentCar.Data;
namespace RentCar.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RentalController(
    VehicleCatalog catalog, RentalSelection selection, RentalPricing pricing, RentCarDbContext db) : Controller
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
    //mơi
    [HttpGet]
    public async Task<IActionResult> Checkout(int? id, int[]? carIds, DateTime? pickupAt, DateTime? returnAt,
    CancellationToken cancellationToken)
    {
        var ids = carIds is { Length: > 0 }
            ? carIds.Distinct().ToList()
            : id.HasValue
                ? [id.Value]
                : selection.Read();

        if (ids.Count == 0)
        {
            TempData["CustomerMessage"] = "Vui lòng chọn ít nhất một xe trước khi đặt thuê.";
            return RedirectToAction(nameof(Index));
        }

        if (User.Identity?.IsAuthenticated != true ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            var returnUrl = Url.Action(nameof(Checkout), "Rental", new
            {
                id,
                carIds = ids.ToArray(),
                pickupAt = pickupAt?.ToString("yyyy-MM-ddTHH:mm"),
                returnAt = returnAt?.ToString("yyyy-MM-ddTHH:mm")
            });
            var loginUrl = Url.Action("Login", "Account", new { returnUrl })!;
            TempData["CustomerMessage"] = "Vui lòng đăng nhập trước khi đặt thuê.";
            ViewBag.LoginUrl = loginUrl;
            return View("RequireLogin");
        }

        var customer = await db.CustomerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId && u.IsActive, cancellationToken);
        if (customer is null)
            return RedirectToAction("Login", "Account");

        var cars = await catalog.Query(false)
            .Where(car => ids.Contains(car.Id))
            .OrderBy(car => car.Id)
            .ToListAsync(cancellationToken);

        if (cars.Count != ids.Count)
        {
            TempData["CustomerMessage"] = "Có xe không tồn tại trong danh sách chọn.";
            return RedirectToAction(nameof(Index));
        }

        var days = 0;
        var total = 0m;
        string? scheduleError = null;
        var isScheduleValid = false;

        if (pickupAt.HasValue || returnAt.HasValue)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;

            if (pickupAt is null || returnAt is null)
                scheduleError = "Vui lòng nhập đầy đủ ngày giờ nhận và trả xe.";
            else if (pickupAt.Value.Date <= today)
                scheduleError = "Ngày nhận xe phải sau ngày hôm nay.";
            else if (returnAt <= pickupAt)
                scheduleError = "Ngày giờ trả xe phải sau ngày giờ nhận.";
            else
            {
                var start = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified), zone);
                var end = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified), zone);
                var available = await catalog.AvailableAsync(catalog.Query(), start, end, cancellationToken);
                var availableCount = await available.CountAsync(car => ids.Contains(car.Id), cancellationToken);

                if (availableCount != cars.Count)
                    scheduleError = "Có xe không còn trống trong khoảng thời gian này. Vui lòng đổi lịch.";
                else
                {
                    days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
                    total = cars.Sum(car => car.DailyRate * days);
                    isScheduleValid = true;
                }
            }
        }

        ViewBag.Days = days;
        ViewBag.Total = total;
        ViewBag.PickupAt = pickupAt;
        ViewBag.ReturnAt = returnAt;
        ViewBag.Customer = customer;
        ViewBag.ScheduleError = scheduleError;
        ViewBag.IsScheduleValid = isScheduleValid;
        return View(cars);
    }
    [HttpPost]
    public async Task<IActionResult> Checkout(int? id, string? carIds, DateTime? pickupAt, DateTime? returnAt,
        string? paymentMethod, string? note, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        List<int> ids;
        if (id.HasValue && id.Value > 0)
            ids = [id.Value];
        else if (!string.IsNullOrWhiteSpace(carIds))
            ids = carIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? v : 0)
                .Where(v => v > 0).Distinct().ToList();
        else
            ids = selection.Read().ToList();

        if (ids.Count == 0 || pickupAt is null || returnAt is null || returnAt <= pickupAt)
        {
            TempData["CustomerMessage"] = "Thông tin xe hoặc thời gian thuê không hợp lệ.";
            return RedirectToAction(nameof(Review), new { id, pickupAt, returnAt });
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified), zone);

        var cars = await catalog.Query(false)
            .Where(car => ids.Contains(car.Id))
            .OrderBy(car => car.Id)
            .ToListAsync(cancellationToken);

        var available = await catalog.AvailableAsync(catalog.Query(), start, end, cancellationToken);
        var availableCount = await available.CountAsync(car => ids.Contains(car.Id), cancellationToken);
        if (cars.Count != ids.Count || availableCount != cars.Count)
        {
            TempData["CustomerMessage"] = "Có xe không còn trống trong khoảng thời gian đã chọn.";
            return RedirectToAction(nameof(Review), new { id, pickupAt, returnAt });
        }

        var days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
        var total = cars.Sum(car => car.DailyRate * days);

        // TODO: Tạo bản ghi đơn thuê / chi tiết thuê / thanh toán trên `db` (RentCarDbContext) rồi SaveChangesAsync

        foreach (var carId in ids)
            selection.Remove(carId);

        TempData["CustomerMessage"] = $"Đặt thuê thành công ({days} ngày, tổng {total:N0} ₫).";
        return RedirectToAction(nameof(Index));
    }
    //moi
}
