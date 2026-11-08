using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[LocalManagement]
[AutoValidateAntiforgeryToken]
public class BrandsController(RentCarDbContext db) : Controller
{
    private IQueryable<Brand> Records => db.Brands;

    public async Task<IActionResult> Index() =>
        View(await Records.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return View(new BrandsForm());
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        return View(new BrandsForm
        {
            Name = item.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int? id, BrandsForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var item = id is null ? new Brand() : await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        form.Name = form.Name.Trim();
        if (await db.Brands.AnyAsync(x => x.Id != item.Id && x.Name == form.Name))
            return Invalid(form, "Tên hãng đã tồn tại.");
        item.Name = form.Name;
        if (id is null)
            db.Brands.Add(item);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Invalid(form, "Không lưu được. Kiểm tra dữ liệu trùng hoặc dữ liệu liên quan.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        if (await db.Vehicles.AnyAsync(x => x.Brand == item.Name))
            return Message("Hãng đang được xe sử dụng.");
        db.Brands.Remove(item);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Message("Không lưu được thao tác. Dữ liệu có thể đang được sử dụng.");
        }
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Invalid(BrandsForm form, string message)
    {
        ModelState.AddModelError("", message);
        return View("Edit", form);
    }

    private IActionResult Message(string message)
    {
        TempData["ManagementMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
