using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;
using System.Security.Claims;
using RentCar.Data;
using Microsoft.Data.SqlClient;
using RentCar.Domain.Common;
using RentCar.Application.Rentals;
namespace RentCar.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RentalController(
    VehicleCatalog catalog, RentalSelection selection, RentalPricing pricing, RentCarDbContext db,
    CustomerRequestSubmission submission, ILogger<RentalController> logger) : Controller
{

    [HttpGet]
    public Task<IActionResult> Index(DateTime? pickupAt, DateTime? returnAt, int? pickupLocationId, CancellationToken cancellationToken) =>
        Build(null, null, pickupAt, returnAt, pickupLocationId, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Add(int id, bool withDriver, string? intent,
        DateTime? pickupAt, DateTime? returnAt, int? pickupLocationId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var car = await catalog.Query().SingleOrDefaultAsync(car => car.Id == id, cancellationToken);
        if (car is null) return NotFound();
        if (withDriver && catalog.Seats(car) <= 2)
            return BadRequest("Xe này không hỗ trợ thuê kèm tài xế.");
        if (pickupLocationId.HasValue)
        {
            if (!(await catalog.LocationsAsync(cancellationToken)).Any(l => l.Id == pickupLocationId)) return BadRequest("Điểm giao nhận không hợp lệ.");
            selection.SetLocation(pickupLocationId.Value);
        }
        selection.Add(id, withDriver);
        if (intent == "review")
            return RedirectToAction(nameof(Index), new { pickupAt, returnAt });
        if(Request.Headers.Accept.ToString().Contains("application/json",StringComparison.OrdinalIgnoreCase))
            return Json(new{count=selection.Read().Count,message="Đã thêm xe và lựa chọn tài xế vào đội xe."});
        TempData["CustomerMessage"] = "Đã thêm xe và lựa chọn tài xế vào đội xe.";
        return RedirectToAction("Details", "Cars", new { id, pickupAt, returnAt, withDriver, pickupLocationId });
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
        int? pickupLocationId, CancellationToken cancellationToken) => Build(id, withDriver, pickupAt, returnAt, pickupLocationId, cancellationToken);

    private async Task<IActionResult> Build(int? id, bool? withDriver, DateTime? pickupAt, DateTime? returnAt,
        int? pickupLocationId, CancellationToken cancellationToken)
    {
        var ids = id.HasValue ? new List<int> { id.Value } : selection.Read();
        var cars = await catalog.Query(false, pickupAt.HasValue ? pickupAt.Value : null).Where(car => ids.Contains(car.Id))
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
            else if ((returnAt.Value - pickupAt.Value).TotalHours < 4 || (returnAt.Value - pickupAt.Value).TotalDays > 30)
                error = "Thời gian thuê cần từ 4 giờ đến 30 ngày.";
            else if (error is null && cars.Count > 0)
            {
                var start = DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified);
                var end = DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified);
                var available = await catalog.AvailableAsync(catalog.Query(), start, end, cancellationToken);
                if (await available.CountAsync(car => ids.Contains(car.Id), cancellationToken) != cars.Count)
                    error = "Có xe không còn trống trong thời gian này. Vui lòng đổi lịch hoặc chọn xe khác.";
                else days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
            }
        }
        var price = await pricing.QuoteAsync(cars, lines.Count(line => line.WithDriver), Math.Max(1, days), pickupAt.HasValue?pickupAt.Value:VietnamTime.Now, cancellationToken);
        var locations = await catalog.LocationsAsync(cancellationToken);
        var selectedLocation = pickupLocationId ?? selection.LocationId();
        if (!locations.Any(l => l.Id == selectedLocation)) selectedLocation = null;
        if (selectedLocation.HasValue) selection.SetLocation(selectedLocation.Value);
        return View("Index", new RentalReviewViewModel(lines, pickupAt, returnAt, days, price, error, id)
        { Locations = locations, PickupLocationId = selectedLocation });
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

        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("KHACHHANG"))
            return Forbid();

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
                var start = DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified);
                var end = DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified);
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
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("KHACHHANG"))
            return Forbid();

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
        var start = DateTime.SpecifyKind(pickupAt.Value, DateTimeKind.Unspecified);
        var end = DateTime.SpecifyKind(returnAt.Value, DateTimeKind.Unspecified);

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
    // Selectively integrated from RentCar(yêu cầu): request creation, never contract/payment creation.
    [HttpPost]
    public Task<IActionResult> CreateRequest(int id, DateTime? pickupAt, DateTime? returnAt,
        int? pickupLocationId, bool withDriver, CancellationToken cancellationToken) =>
        SendRequest([new RequestedVehicle(id, withDriver)], false, id, pickupAt, returnAt, pickupLocationId, cancellationToken);

    [HttpPost]
    public Task<IActionResult> CreateListRequest(int[]? carIds, DateTime? pickupAt, DateTime? returnAt,
        int? pickupLocationId, CancellationToken cancellationToken)
    {
        var ids = carIds is { Length: > 0 } ? carIds : selection.Read().ToArray();
        var drivers = selection.DriverIds();
        return SendRequest(ids.Select(id => new RequestedVehicle(id, drivers.Contains(id))).ToArray(),
            true, null, pickupAt, returnAt, pickupLocationId, cancellationToken);
    }
    private async Task<IActionResult> SendRequest(IReadOnlyList<RequestedVehicle> vehicles, bool isList, int? id,
        DateTime? pickupAt, DateTime? returnAt, int? pickupLocationId, CancellationToken ct)
    {
        var locationId = pickupLocationId ?? selection.LocationId();
        var route = new { id, pickupAt=pickupAt?.ToString("s"), returnAt=returnAt?.ToString("s"), pickupLocationId=locationId, withDriver=vehicles.FirstOrDefault()?.WithDriver };
        var returnUrl = isList ? Url.Action(nameof(Index), "Rental", route) : Url.Action("Details", "Cars", route);
        if (User.Identity?.IsAuthenticated != true || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return RedirectToAction("Login", "Account", new { returnUrl });
        if (!User.IsInRole("KHACHHANG")) return Forbid();
        try
        {
            if (!ModelState.IsValid || pickupAt is null || returnAt is null || locationId is null)
                throw new DomainException("Vui lòng nhập đầy đủ ngày giờ nhận, trả và chọn điểm giao nhận hợp lệ.");
            var requestId = await submission.SubmitAsync(customerId, vehicles, pickupAt.Value, returnAt.Value, locationId.Value, ct);
            selection.SetLocation(locationId.Value);
            if (isList) foreach (var vehicle in vehicles) selection.Remove(vehicle.VehicleId);
            TempData["CustomerMessage"] = $"Đã gửi yêu cầu #{requestId} ({vehicles.Count} xe). Yêu cầu được giữ chỗ 24 giờ kể từ lúc gửi.";
            return Redirect(Url.Action("Index", "MyRequests") + "#yeu-cau-" + requestId);
        }
        catch (DomainException ex) { TempData["RequestError"] = ex.Message; }
        catch (SqlException ex)
        {
            logger.LogWarning(ex, "Unable to submit customer rental request.");
            TempData["RequestError"] = "Không thể gửi yêu cầu lúc này. Xe có thể vừa được giữ chỗ; vui lòng kiểm tra lịch và thử lại.";
        }
        return LocalRedirect(returnUrl!);
    }

}
