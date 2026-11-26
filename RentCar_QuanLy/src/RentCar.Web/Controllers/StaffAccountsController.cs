using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Application.Accounts;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>
/// Must #3 (FR-6.3, UC-06): quản lý tài khoản nhân viên, chỉ Quản trị viên.
/// v4: nhân viên tự tạo tài khoản bằng mã đăng ký do quản trị viên cấp (mục Mã đăng ký nhân viên).
/// </summary>
[Authorize(Roles = RoleCodes.Admin)]
public sealed class StaffAccountsController(StaffAccountService staff, StaffInvitationService invitations) : Controller
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, string? role, string? state, CancellationToken ct)
    {
        bool? active = state switch { "active" => true, "locked" => false, _ => null };
        ViewBag.Keyword = keyword;
        ViewBag.Role = role;
        ViewBag.State = state;
        return View(await staff.ListAsync(keyword, role, active, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var item = await staff.GetAsync(id, ct);
        if (item is null) return NotFound();
        return View(new StaffEditForm
        {
            Id = item.Id, FullName = item.FullName, Email = item.Email,
            Phone = item.Phone, RoleCode = item.RoleCode
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StaffEditForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            await staff.UpdateAsync(new StaffUpdateRequest(form.Id, form.FullName, form.Phone, form.RoleCode), ActorId, ct);
            TempData["Success"] = "Đã cập nhật tài khoản nhân viên.";
            return RedirectToAction(nameof(Index));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            return View(form);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active, CancellationToken ct)
    {
        try
        {
            await staff.SetActiveAsync(id, active, ActorId, ct);
            TempData["Success"] = active
                ? "Đã mở khóa tài khoản."
                : "Đã khóa tài khoản. Nhân viên này sẽ không đăng nhập được và phiên đang mở sẽ bị đăng xuất ở yêu cầu tiếp theo.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Invitations(string? state, CancellationToken ct)
    {
        // Mã thô chỉ đi qua TempData một lần: đọc xong là xóa, tải lại trang sẽ không còn thấy.
        ViewBag.IssuedCode = TempData["IssuedCode"] as string;
        ViewBag.IssuedEmail = TempData["IssuedEmail"] as string;
        ViewBag.IssuedExpires = TempData["IssuedExpires"] as string;
        ViewBag.State = state ?? "";
        return View(await invitations.ListAsync(state, ct));
    }

    [HttpGet]
    public IActionResult Issue() => View(new StaffInvitationForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(StaffInvitationForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var issued = await invitations.IssueAsync(form.Email, form.FullName, form.RoleCode, ActorId, ct);
            TempData["IssuedCode"] = issued.Code;
            TempData["IssuedEmail"] = issued.Email;
            TempData["IssuedExpires"] = issued.ExpiresAt.ToString("dd/MM/yyyy HH:mm");
            TempData["Success"] = "Đã cấp mã đăng ký.";
            return RedirectToAction(nameof(Invitations));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            return View(form);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int id, CancellationToken ct)
    {
        try
        {
            await invitations.RevokeAsync(id, ActorId, ct);
            TempData["Success"] = "Đã thu hồi mã đăng ký. Mã này không còn dùng được.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Invitations));
    }
}
