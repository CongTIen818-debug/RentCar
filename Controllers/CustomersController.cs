using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[LocalManagement]
[AutoValidateAntiforgeryToken]
public class CustomersController(RentCarDbContext db) : Controller
{
    private IQueryable<Customer> Records => db.Customers.Include(x => x.DriverLicense);

    public async Task<IActionResult> Index() =>
        View(await Records.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return View(new CustomersForm());
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        return View(new CustomersForm
        {
            FullName = item.FullName,
            DateOfBirth = item.DateOfBirth,
            Phone = item.Phone,
            Email = item.Email,
            IdentityNumber = item.IdentityNumber,
            LicenseNumber = item.DriverLicense?.LicenseNumber ?? "",
            LicenseClass = item.DriverLicense?.LicenseClass ?? "",
            ExpiryDate = item.DriverLicense?.ExpiryDate ?? default
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int? id, CustomersForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var item = id is null ? new Customer() : await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        if (form.DateOfBirth == default || form.ExpiryDate == default)
            return Invalid(form, "Vui lòng nhập ngày sinh và hạn GPLX.");
        if (form.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)))
            return Invalid(form, "Ngày sinh không được ở tương lai.");
        var linkedAccount = id.HasValue
            ? await db.AppUsers.SingleOrDefaultAsync(user => user.CustomerId == id)
            : null;
        if (await db.Customers.AnyAsync(customer => customer.Id != item.Id && customer.Email == form.Email) ||
            await db.AppUsers.AnyAsync(user => user.Email == form.Email && user.Id != (linkedAccount == null ? 0 : linkedAccount.Id)))
            return Invalid(form, "Email đã được sử dụng.");

        item.DriverLicense ??= new DriverLicense();
        item.FullName = form.FullName;
        item.DateOfBirth = form.DateOfBirth;
        item.Phone = form.Phone;
        item.Email = form.Email;
        if (linkedAccount is not null)
        {
            linkedAccount.FullName = form.FullName;
            linkedAccount.Email = form.Email;
        }
        item.IdentityNumber = form.IdentityNumber;
        item.DriverLicense.LicenseNumber = form.LicenseNumber;
        item.DriverLicense.LicenseClass = form.LicenseClass;
        item.DriverLicense.ExpiryDate = form.ExpiryDate;
        if (id is null)
            db.Customers.Add(item);

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

        if (await db.AppUsers.AnyAsync(x => x.CustomerId == id) ||
            await db.RentalRequests.AnyAsync(x => x.CustomerId == id) ||
            await db.RentalContracts.AnyAsync(x => x.CustomerId == id))
            return Message("Khách hàng đã có tài khoản hoặc lịch sử thuê.");
        if (item.DriverLicense is not null)
            db.DriverLicenses.Remove(item.DriverLicense);
        db.Customers.Remove(item);
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

    private IActionResult Invalid(CustomersForm form, string message)
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
