using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[LocalManagement]
[AutoValidateAntiforgeryToken]
public class VehiclesController(RentCarDbContext database) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await database.Vehicles.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        await LoadBrands();
        if (id is null)
            return View(new VehicleForm());

        var item = await database.Vehicles.FindAsync(id.Value);
        if (item is null)
            return NotFound();

        return View(new VehicleForm
        {
            PlateNumber = item.PlateNumber,
            Brand = item.Brand,
            Model = item.Model,
            VehicleType = item.VehicleType,
            Seats = item.Seats,
            Transmission = item.Transmission,
            FuelType = item.FuelType,
            ManufactureYear = item.ManufactureYear,
            DailyRate = item.DailyRate,
            WeeklyRate = item.WeeklyRate,
            DepositAmount = item.DepositAmount
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int? id, VehicleForm form)
    {
        await LoadBrands();
        if (!ModelState.IsValid)
            return View(form);

        if (!await database.Brands.AnyAsync(brand => brand.Name == form.Brand))
        {
            ModelState.AddModelError(nameof(form.Brand), "Vui lòng chọn hãng trong danh mục.");
            return View(form);
        }

        var item = id is int key ? await database.Vehicles.FindAsync(key) : new Vehicle();
        if (item is null)
            return NotFound();

        var plate = form.PlateNumber.Trim().ToUpperInvariant();
        if (await database.Vehicles.AnyAsync(vehicle => vehicle.Id != item.Id && vehicle.PlateNumber == plate))
        {
            ModelState.AddModelError(nameof(form.PlateNumber), "Biển số đã tồn tại.");
            return View(form);
        }

        item.PlateNumber = plate;
        item.Brand = form.Brand;
        item.Model = form.Model;
        item.VehicleType = form.VehicleType;
        item.Seats = form.Seats;
        item.Transmission = form.Transmission;
        item.FuelType = form.FuelType;
        item.ManufactureYear = form.ManufactureYear;
        item.DailyRate = form.DailyRate;
        item.WeeklyRate = form.WeeklyRate;
        item.DepositAmount = form.DepositAmount;
        if (id is null)
        {
            item.Status = "Sẵn sàng";
            database.Vehicles.Add(item);
        }

        try
        {
            await database.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 or 2601 or 2627 })
        {
            ModelState.AddModelError("", "Không lưu được xe. Kiểm tra biển số và hãng xe.");
            return View(form);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadBrands()
    {
        ViewBag.Brands = await database.Brands.AsNoTracking().OrderBy(brand => brand.Name)
            .Select(brand => brand.Name).ToListAsync();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await database.Vehicles.FindAsync(id);
        if (item is null)
            return NotFound();

        await using var transaction = await database.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await database.RentalContracts.AnyAsync(contract => contract.VehicleId == id &&
            (contract.Status == "Đã xác nhận" || contract.Status == "Đang thuê")))
        {
            TempData["ManagementMessage"] = "Xe còn hợp đồng đã xác nhận hoặc đang thuê.";
            return RedirectToAction(nameof(Index));
        }

        item.Status = "Ngừng hoạt động";
        await database.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToAction(nameof(Index));
    }
}
