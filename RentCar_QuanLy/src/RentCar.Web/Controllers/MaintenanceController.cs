using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>Lịch bảo trì xe (FR-1.4, BR-11): chỉ Quản trị viên và Nhân viên kinh doanh.</summary>
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Sales)]
public sealed class MaintenanceController(IAppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int vehicleId, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vehicleId, ct);
        if (vehicle is null) return NotFound();
        ViewBag.Vehicle = vehicle;
        var items = await db.MaintenanceSchedules.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.StartAt).ToListAsync(ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int vehicleId, CancellationToken ct)
    {
        if (!await PrepareAsync(vehicleId, ct)) return NotFound();
        var start = VietnamTime.Now.Date.AddDays(1).AddHours(8);
        return View("Form", new MaintenanceForm { VehicleId = vehicleId, StartAt = start, EndAt = start.AddHours(8) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceForm form, CancellationToken ct)
    {
        if (!await PrepareAsync(form.VehicleId, ct)) return NotFound();
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid) return View("Form", form);

        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            VehicleId = form.VehicleId, Type = form.Type.Trim(), Description = Clean(form.Description),
            StartAt = form.StartAt!.Value, EndAt = form.EndAt!.Value, Cost = form.Cost
        });
        if (!await TrySaveAsync(form, ct)) return View("Form", form);
        TempData["Success"] = "Đã thêm lịch bảo trì.";
        return RedirectToAction(nameof(Index), new { vehicleId = form.VehicleId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var m = await db.MaintenanceSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null || !await PrepareAsync(m.VehicleId, ct)) return NotFound();
        return View("Form", new MaintenanceForm
        {
            Id = m.Id, VehicleId = m.VehicleId, Type = m.Type, Description = m.Description,
            StartAt = m.StartAt, EndAt = m.EndAt, Cost = m.Cost
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaintenanceForm form, CancellationToken ct)
    {
        var m = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == form.Id, ct);
        if (m is null) return NotFound();
        form.VehicleId = m.VehicleId; // không cho đổi xe qua form
        if (!await PrepareAsync(m.VehicleId, ct)) return NotFound();
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid) return View("Form", form);

        m.Type = form.Type.Trim();
        m.Description = Clean(form.Description);
        m.StartAt = form.StartAt!.Value;
        m.EndAt = form.EndAt!.Value;
        m.Cost = form.Cost;
        if (!await TrySaveAsync(form, ct)) return View("Form", form);
        TempData["Success"] = "Đã cập nhật lịch bảo trì.";
        return RedirectToAction(nameof(Index), new { vehicleId = m.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var m = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        db.MaintenanceSchedules.Remove(m);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã xóa lịch bảo trì.";
        return RedirectToAction(nameof(Index), new { vehicleId = m.VehicleId });
    }

    private async Task<bool> PrepareAsync(int vehicleId, CancellationToken ct)
    {
        var v = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vehicleId, ct);
        if (v is null) return false;
        ViewBag.Vehicle = v;
        return true;
    }

    private async Task ValidateAsync(MaintenanceForm f, CancellationToken ct)
    {
        if (f.StartAt is null || f.EndAt is null) return;
        if (f.EndAt <= f.StartAt)
        {
            ModelState.AddModelError(nameof(f.EndAt), "Thời gian kết thúc phải sau thời gian bắt đầu.");
            return;
        }
        // Báo lỗi rõ trước khi chạm trigger. Điều kiện khớp trg_lichBaoTri_KhongChongHopDong và view vLichXe:
        // hợp đồng Đã xác nhận (đến EndAt) hoặc Đang thuê (đến ngày nhận lại, chưa trả thì không giới hạn).
        var s = f.StartAt.Value; var e = f.EndAt.Value;
        var clash = await db.Contracts.AsNoTracking()
            .Where(c => c.Lines.Any(l => l.VehicleId == f.VehicleId)
                && (c.Status == ContractStatus.Confirmed || c.Status == ContractStatus.Active)
                && c.StartAt < e
                && ((c.Handover != null && c.Handover.ReturnedAt != null && s < c.Handover.ReturnedAt)
                    || ((c.Handover == null || c.Handover.ReturnedAt == null)
                        && (c.Status == ContractStatus.Active || s < c.EndAt))))
            .OrderBy(c => c.StartAt).Select(c => new { c.Code, c.StartAt, c.EndAt }).FirstOrDefaultAsync(ct);
        if (clash is not null)
            ModelState.AddModelError(nameof(f.StartAt),
                $"Lịch bảo trì trùng hợp đồng {clash.Code} ({clash.StartAt:dd/MM/yyyy HH:mm} – {clash.EndAt:dd/MM/yyyy HH:mm}). Hãy chọn khoảng thời gian khác.");
    }

    /// <summary>Lưu và chuyển lỗi trigger (DomainException 409) thành thông báo thân thiện ngay trên form.</summary>
    private async Task<bool> TrySaveAsync(MaintenanceForm form, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DomainException)
        {
            ModelState.AddModelError(nameof(form.StartAt), "Khoảng thời gian này đang có hợp đồng đã xác nhận hoặc đang thuê. Hãy chọn thời gian khác.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không lưu được lịch bảo trì. Hãy kiểm tra thời gian và chi phí.");
        }
        return false;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
