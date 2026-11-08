using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[LocalManagement]
[AutoValidateAntiforgeryToken]
public class EmployeesController(RentCarDbContext db) : Controller
{
    private IQueryable<AppUser> Records => db.AppUsers.Where(x => x.CustomerId == null && x.Role != "Khách hàng");

    public async Task<IActionResult> Index() =>
        View(await Records.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return View(new EmployeesForm());
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        return View(new EmployeesForm
        {
            FullName = item.FullName,
            Email = item.Email,
            Role = item.Role
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int? id, EmployeesForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var item = id is null ? new AppUser() : await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        if (form.Role is not ("Nhân viên kinh doanh" or "Nhân viên giao nhận" or "Kế toán" or "Quản trị viên"))
            return Invalid(form, "Vai trò không hợp lệ.");
        if (await db.AppUsers.AnyAsync(user => user.Id != item.Id && user.Email == form.Email) ||
            await db.Customers.AnyAsync(customer => customer.Email == form.Email))
            return Invalid(form, "Email đã được sử dụng.");
        if (id is null)
            item.IsActive = false;
        item.FullName = form.FullName;
        item.Email = form.Email;
        item.Role = form.Role;
        if (id is null)
            db.AppUsers.Add(item);

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

        item.IsActive = false;
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

    private IActionResult Invalid(EmployeesForm form, string message)
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
