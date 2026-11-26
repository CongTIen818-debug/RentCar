using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>Quản lý bảng giá, ưu đãi và chính sách phụ phí theo FR-7.1 đến FR-7.3.</summary>
[Authorize(Roles = RoleCodes.Admin)]
public sealed class PricingController(IAppDbContext db) : Controller
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new PricingIndexVm
        {
            PriceRules = await db.PriceRules.AsNoTracking().OrderByDescending(x => x.StartAt).ToListAsync(ct),
            Promotions = await db.Promotions.AsNoTracking().OrderByDescending(x => x.StartAt).ToListAsync(ct),
            SurchargePolicies = await db.SurchargePolicies.AsNoTracking().ToListAsync(ct),
            VehicleTypes = await db.VehicleTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePrice(PriceRuleForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid || form.EndAt <= form.StartAt || form.Factor <= 0)
        {
            TempData["Error"] = "Vui lòng kiểm tra loại xe, hệ số và khoảng thời gian bảng giá.";
            return RedirectToAction(nameof(Index));
        }
        if (!await db.VehicleTypes.AnyAsync(x => x.Id == form.VehicleTypeId, ct))
        {
            TempData["Error"] = "Loại xe không tồn tại.";
            return RedirectToAction(nameof(Index));
        }
        var overlaps = await db.PriceRules.AnyAsync(x => x.VehicleTypeId == form.VehicleTypeId
            && x.IsActive && x.StartAt < form.EndAt && form.StartAt < x.EndAt && x.Id != form.Id, ct);
        if (overlaps)
        {
            TempData["Error"] = "Khoảng thời gian bị trùng với bảng giá đang hoạt động của loại xe này.";
            return RedirectToAction(nameof(Index));
        }

        PriceRule rule;
        if (form.Id > 0)
        {
            rule = await db.PriceRules.FirstOrDefaultAsync(x => x.Id == form.Id, ct)
                ?? throw new DomainException("Không tìm thấy bảng giá.", 404);
        }
        else
        {
            rule = new PriceRule();
            db.PriceRules.Add(rule);
        }
        rule.VehicleTypeId = form.VehicleTypeId;
        rule.StartAt = form.StartAt;
        rule.EndAt = form.EndAt;
        rule.Factor = form.Factor;
        rule.IsActive = true;
        db.AuditLogs.Add(Log("BANG_GIA", form.VehicleTypeId.ToString(), "CAP_NHAT_BANG_GIA"));
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã lưu bảng giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePrice(int id, CancellationToken ct)
    {
        var item = await db.PriceRules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.IsActive = !item.IsActive;
        db.AuditLogs.Add(Log("BANG_GIA", id.ToString(), item.IsActive ? "KICH_HOAT_BANG_GIA" : "NGUNG_BANG_GIA"));
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã cập nhật trạng thái bảng giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePromotion(PromotionForm form, CancellationToken ct)
    {
        form.Code = form.Code.Trim().ToUpperInvariant();
        if (!ModelState.IsValid || form.EndAt <= form.StartAt || form.DiscountPercent <= 0 || form.DiscountPercent > 100)
        {
            TempData["Error"] = "Vui lòng kiểm tra mã, mức giảm và thời hạn ưu đãi.";
            return RedirectToAction(nameof(Index));
        }
        if (await db.Promotions.AnyAsync(x => x.Code == form.Code, ct))
        {
            TempData["Error"] = "Mã ưu đãi đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }
        var item = new Promotion
        {
            Code = form.Code, Name = form.Name.Trim(), Description = form.Description?.Trim(),
            DiscountPercent = form.DiscountPercent, StartAt = form.StartAt, EndAt = form.EndAt, IsActive = true
        };
        db.Promotions.Add(item);
        db.AuditLogs.Add(Log("UU_DAI", form.Code, "TAO_UU_DAI"));
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã tạo ưu đãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePromotion(int id, CancellationToken ct)
    {
        var item = await db.Promotions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.IsActive = !item.IsActive;
        db.AuditLogs.Add(Log("UU_DAI", item.Code, item.IsActive ? "KICH_HOAT_UU_DAI" : "NGUNG_UU_DAI"));
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã cập nhật trạng thái ưu đãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSurcharge(SurchargePolicyForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid || !await db.VehicleTypes.AnyAsync(x => x.Id == form.VehicleTypeId, ct))
        {
            TempData["Error"] = "Vui lòng kiểm tra cấu hình phụ phí.";
            return RedirectToAction(nameof(Index));
        }
        var item = await db.SurchargePolicies.FirstOrDefaultAsync(x => x.VehicleTypeId == form.VehicleTypeId, ct);
        if (item is null)
        {
            item = new SurchargePolicy { VehicleTypeId = form.VehicleTypeId };
            db.SurchargePolicies.Add(item);
        }
        item.GraceMinutes = form.GraceMinutes;
        item.DailyKmLimit = form.DailyKmLimit;
        item.PricePerExtraKm = form.PricePerExtraKm;
        db.AuditLogs.Add(Log("CHINH_SACH_PHU_PHI", form.VehicleTypeId.ToString(), "CAP_NHAT_PHU_PHI"));
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Đã lưu cấu hình phụ phí.";
        return RedirectToAction(nameof(Index));
    }

    private AuditLog Log(string type, string id, string action) => new()
    {
        ActorId = ActorId, ObjectType = type, ObjectId = id, Action = action,
        At = VietnamTime.Now
    };
}
