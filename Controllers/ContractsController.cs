using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[LocalManagement]
[AutoValidateAntiforgeryToken]
public class ContractsController(RentCarDbContext db) : Controller
{
    private IQueryable<RentalContract> Records => db.RentalContracts;

    public async Task<IActionResult> Index() =>
        View(await Records.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            await LoadRequests();
            return View(new ContractsForm());
        }
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();
        if (item.Status != "Chờ xác nhận")
            return Conflict();

        return View(new ContractsForm
        {
            RentalRequestId = item.RentalRequestId,
            ContractCode = item.ContractCode,
            RentalPrice = item.RentalPrice,
            DiscountAmount = item.DiscountAmount,
            DepositAmount = item.DepositAmount,
            KmLimit = item.KmLimit
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int? id, ContractsForm form)
    {
        if (id is null)
            await LoadRequests();
        if (!ModelState.IsValid)
            return View(form);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var item = id is null ? new RentalContract() : await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        if (id.HasValue && (item.Status != "Chờ xác nhận" || item.RentalRequestId != form.RentalRequestId))
            return Invalid(form, "Chỉ sửa hợp đồng chờ xác nhận và giữ nguyên yêu cầu thuê.");
        if (form.DiscountAmount > form.RentalPrice)
            return Invalid(form, "Giảm giá không được vượt quá giá thuê.");
        if (id is null)
        {
            var now = DateTime.UtcNow;
            var request = await db.RentalRequests.SingleOrDefaultAsync(x =>
                x.Id == form.RentalRequestId && x.Status == "Chờ xử lý" && x.ExpiresAt > now);
            if (request is null || await db.RentalContracts.AnyAsync(x => x.RentalRequestId == form.RentalRequestId))
                return Invalid(form, "Yêu cầu thuê phải còn hiệu lực và chưa có hợp đồng.");

            var available = await db.Vehicles.AnyAsync(vehicle => vehicle.Id == request.VehicleId &&
                vehicle.Status != "Ngừng hoạt động" && vehicle.Status != "Bảo trì" &&
                !vehicle.MaintenanceRecords.Any(record => record.Status != "Đã hủy" &&
                    record.StartAt < request.EndAt && request.StartAt < record.EndAt) &&
                !vehicle.RentalRequests.Any(other => other.Id != request.Id && other.Status == "Chờ xử lý" &&
                    other.ExpiresAt > now && other.StartAt < request.EndAt && request.StartAt < other.EndAt) &&
                !vehicle.RentalContracts.Any(other => other.StartAt < request.EndAt &&
                    ((other.Status == "Đang thuê" && other.ActualReturnAt == null &&
                        (request.StartAt < other.EndAt || other.EndAt <= now)) ||
                     (request.StartAt < other.EndAt &&
                        (other.Status == "Đã xác nhận" ||
                         (other.Status == "Chờ xác nhận" && other.ExpiresAt > now))))));
            if (!available)
                return Invalid(form, "Xe không khả dụng trong khoảng thời gian thuê.");

            item.CustomerId = request.CustomerId;
            item.VehicleId = request.VehicleId;
            item.StartAt = request.StartAt;
            item.EndAt = request.EndAt;
            item.ExpiresAt = now.AddHours(24);
            item.Status = "Chờ xác nhận";
            request.Status = "Đã chuyển hợp đồng";
        }
        item.RentalRequestId = form.RentalRequestId;
        item.ContractCode = form.ContractCode;
        item.RentalPrice = form.RentalPrice;
        item.DiscountAmount = form.DiscountAmount;
        item.DepositAmount = form.DepositAmount;
        item.KmLimit = form.KmLimit;
        if (id is null)
            db.RentalContracts.Add(item);

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            return Invalid(form, "Không lưu được. Kiểm tra dữ liệu trùng hoặc dữ liệu liên quan.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRequests()
    {
        var now = DateTime.UtcNow;
        var requests = await db.RentalRequests.AsNoTracking()
            .Where(request => request.Status == "Chờ xử lý" && request.ExpiresAt > now &&
                !request.RentalContracts.Any())
            .OrderBy(request => request.Id)
            .Select(request => new { request.Id, request.Customer.FullName, request.Vehicle.PlateNumber })
            .ToListAsync();
        ViewBag.RentalRequests = requests.Select(request => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
            $"#{request.Id} · {request.FullName} · {request.PlateNumber}", request.Id.ToString())).ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var item = await Records.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null)
            return NotFound();

        if (item.Status != "Chờ xác nhận" || await db.PaymentTransactions.AnyAsync(x => x.ContractId == id))
            return Message("Chỉ hủy được hợp đồng chờ xác nhận chưa có giao dịch.");
        item.Status = "Đã hủy";
        item.CancelReason = "Hủy từ trang quản lý";
        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            return Message("Không lưu được thao tác. Dữ liệu có thể đang được sử dụng.");
        }
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Invalid(ContractsForm form, string message)
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
