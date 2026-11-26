using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

public class ServicesController(VehicleCatalog catalog) : Controller
{
    [HttpGet("/dich-vu/{slug}")]
    public async Task<IActionResult> Index(string slug, CancellationToken cancellationToken)
    {
        if (slug is not ("thue-xe-tu-lai" or "thue-xe-kem-tai-xe" or "thue-xe-su-kien"))
            return NotFound();
        var cars = await catalog.Query().OrderBy(car => car.Id).ToListAsync(cancellationToken);
        if (slug == "thue-xe-tu-lai")
            cars = cars.OrderBy(car => catalog.Seats(car) is > 0 ? catalog.Seats(car) : int.MaxValue)
                .ThenByDescending(car => car.DailyRate).ThenBy(car => car.Id).ToList();
        else if (slug == "thue-xe-kem-tai-xe")
            cars = cars.Where(car => catalog.Seats(car) > 2)
                .OrderByDescending(car => car.OriginalRate).ThenBy(car => car.Id).ToList();
        return View(ServiceContent.Create(slug, cars));
    }
}
