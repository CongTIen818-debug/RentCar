using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Services;
using RentCar.Domain.Common;

namespace RentCar.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CarsController(VehicleCatalog catalog, RentCarDbContext database, RentalSelection selection, RentalPricing pricing) : Controller
{
    public async Task<IActionResult> Details(int id, DateTime? pickupAt, DateTime? returnAt, bool? withDriver,
        int? pickupLocationId, CancellationToken cancellationToken)
    {
        var car = await catalog.Query(false, pickupAt.HasValue ? pickupAt.Value : null).SingleOrDefaultAsync(car => car.Id == id, cancellationToken);
        if (car is null)
            return NotFound();

        var photos = await database.Database.SqlQuery<VehiclePhoto>($"""
            SELECT duongDanAnh AS ImageUrl FROM dbo.hinhAnhXe
            WHERE idXe = {id} AND NULLIF(duongDanAnh, '') IS NOT NULL
            """).ToListAsync(cancellationToken);
        var images = photos.Select(photo => photo.ImageUrl).Prepend(car.ImageUrl ?? "")
            .Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        var locations = await catalog.LocationsAsync(cancellationToken);
        var selectedLocation = pickupLocationId ?? selection.LocationId();
        if (!locations.Any(l => l.Id == selectedLocation)) selectedLocation = null;
        return View(new VehicleDetailsViewModel(car, images, catalog.Profile(car), pickupAt, returnAt,
            catalog.Seats(car) > 2 && (withDriver ?? selection.DriverIds().Contains(id)), catalog.Seats(car) > 2) { Locations = locations, PickupLocationId = selectedLocation });
    }
    // Read-only quote for the member's existing estimate fields; never creates a request or contract.
    [HttpGet]
    public async Task<IActionResult> Quote(int id, DateTime? pickupAt, DateTime? returnAt, bool withDriver,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid || pickupAt is null || returnAt is null)
                throw new DomainException("Vui lòng nhập đầy đủ ngày giờ nhận và trả hợp lệ.");
            CustomerRequestSubmission.ValidateSchedule(pickupAt.Value, returnAt.Value, VietnamTime.Now);
            var car = await catalog.Query(atVietnam: pickupAt.Value).SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new DomainException("Xe không tồn tại hoặc hiện không sẵn sàng.", 409);
            if (withDriver && car.Seats <= 2)
                throw new DomainException("Xe từ 2 chỗ trở xuống chỉ hỗ trợ tự lái.");
            var available = await catalog.AvailableAsync(catalog.Query(), pickupAt.Value, returnAt.Value, cancellationToken);
            if (!await available.AnyAsync(c => c.Id == id, cancellationToken))
                throw new DomainException("Xe không còn trống trong khoảng thời gian này. Vui lòng chọn lịch khác.", 409);
            var days = (int)Math.Ceiling((returnAt.Value - pickupAt.Value).TotalDays);
            var quote = await pricing.QuoteAsync([car], withDriver ? 1 : 0, days, pickupAt.Value, cancellationToken);
            return Json(new { days, dailyTotal = car.DailyRate + (withDriver ? RentalPricing.DriverDailyRate : 0), total = quote.Payable, taxIncluded = true });
        }
        catch (DomainException error) { return StatusCode(error.StatusCode, new { message = error.Message }); }
    }

}

