using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CarsController(VehicleCatalog catalog, RentCarDbContext database) : Controller
{
    public async Task<IActionResult> Details(int id, DateTime? pickupAt, DateTime? returnAt,
        CancellationToken cancellationToken)
    {
        var car = await catalog.Query(false).SingleOrDefaultAsync(car => car.Id == id, cancellationToken);
        if (car is null)
            return NotFound();

        var photos = await database.Database.SqlQuery<VehiclePhoto>($"""
            SELECT duongDanAnh AS ImageUrl FROM dbo.hinhAnhXe
            WHERE idXe = {id} AND NULLIF(duongDanAnh, '') IS NOT NULL
            """).ToListAsync(cancellationToken);
        var images = photos.Select(photo => photo.ImageUrl).Prepend(car.ImageUrl ?? "")
            .Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        return View(new VehicleDetailsViewModel(car, images, catalog.Profile(car), pickupAt, returnAt));
    }
}

