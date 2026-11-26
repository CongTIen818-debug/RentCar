using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Application.Catalog;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>FR-2.1 đến FR-2.4: tra cứu xe khả dụng và xem chi tiết. Không yêu cầu đăng nhập.</summary>
public sealed class CatalogController(CatalogService catalog, IAppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CatalogSearchForm form, CancellationToken ct)
    {
        var start = form.StartAt ?? DefaultStart();
        var end = form.EndAt ?? start.AddDays(3);
        form.StartAt = start;
        form.EndAt = end;

        var vm = new CatalogIndexVm
        {
            Form = form,
            Types = await TypeOptionsAsync(form.VehicleTypeId, ct)
        };

        try
        {
            vm.Page = await catalog.SearchAsync(new CatalogFilter(
                start, end, form.VehicleTypeId, form.MinSeats, form.Transmission, form.FuelType,
                form.MaxDailyRate, Math.Max(1, form.Page), Keyword: form.Keyword), ct);
        }
        catch (DomainException ex)
        {
            vm.Error = ex.Message;
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? startAt, DateTime? endAt, string? customer, CancellationToken ct)
    {
        var start = startAt ?? DefaultStart();
        var end = endAt ?? start.AddDays(3);

        try
        {
            var detail = await catalog.GetDetailAsync(id, start, end, ct);
            if (detail is null) return NotFound();

            var locations = await db.PickupLocations.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Id)
                .ToListAsync(ct);

            // Chỉ nhân viên bán hàng dùng ô khách; không đưa giá trị này ra cho vai trò khác.
            var canBookForCustomer = User.IsInRole(RoleCodes.Sales) || User.IsInRole(RoleCodes.Admin);
            return View(new VehicleDetailVm
            {
                Detail = detail, StartAt = start, EndAt = end, Locations = locations,
                CustomerKey = canBookForCustomer ? customer : null
            });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>Mặc định: 09:00 ngày mai, thuê 3 ngày.</summary>
    private static DateTime DefaultStart()
    {
        var t = VietnamTime.Now.AddDays(1);
        return new DateTime(t.Year, t.Month, t.Day, 9, 0, 0);
    }

    private async Task<List<SelectListItem>> TypeOptionsAsync(int? selected, CancellationToken ct)
    {
        var types = await db.VehicleTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return types.Select(t => new SelectListItem(t.Name, t.Id.ToString(), t.Id == selected)).ToList();
    }
}
