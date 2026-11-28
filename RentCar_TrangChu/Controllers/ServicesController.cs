using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

public class ServicesController(VehicleCatalog catalog) : Controller
{
    [HttpGet("/dich-vu/{slug}")]
    public async Task<IActionResult> Index(string slug, int page, CancellationToken cancellationToken)
    {
        if (slug is not ("thue-xe-tu-lai" or "thue-xe-kem-tai-xe" or "thue-xe-su-kien"))
            return NotFound();
        var query = catalog.Query();
        if(slug=="thue-xe-kem-tai-xe") query=query.Where(car=>car.Seats>2);
        var total=await query.CountAsync(cancellationToken);var pages=Math.Max(1,(int)Math.Ceiling(total/12d));page=Math.Clamp(page,1,pages);
        var ordered=slug=="thue-xe-tu-lai" ? query.OrderBy(car=>car.Seats).ThenByDescending(car=>car.DailyRate).ThenBy(car=>car.Id)
            :slug=="thue-xe-kem-tai-xe" ? query.OrderByDescending(car=>car.OriginalRate).ThenBy(car=>car.Id) : query.OrderBy(car=>car.Id).ThenBy(car=>car.Id);
        var cars=await ordered.Skip((page-1)*12).Take(12).ToListAsync(cancellationToken);
        return View(ServiceContent.Create(slug,cars) with {Page=page,Pages=pages,Total=total});
    }
}
