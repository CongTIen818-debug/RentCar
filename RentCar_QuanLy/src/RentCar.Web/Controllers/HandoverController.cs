using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Application.Handover;
using RentCar.Application.Rentals;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;
using RentCar.Domain.Pricing;
using RentCar.Domain.Rules;
using RentCar.Web.Helpers;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>UC-03: bàn giao và nhận lại xe, kiểm tra xe sau trả, không đến nhận xe, thu chi sau thuê.</summary>
[Authorize(Roles = HandoverRoles)]
public sealed class HandoverController(HandoverService handover, RentalService rentals, IWebHostEnvironment env) : Controller
{
    private const string HandoverRoles = RoleCodes.Handover + "," + RoleCodes.Sales + "," + RoleCodes.Accounting + "," + RoleCodes.Admin;
    private const string InspectionRoles = RoleCodes.Handover + "," + RoleCodes.Admin;
    private const string SalesRoles = RoleCodes.Sales + "," + RoleCodes.Admin;
    private const string AccountingRoles = RoleCodes.Accounting + "," + RoleCodes.Admin;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await handover.GetDeskAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Delivery(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        return contract is null ? NotFound() : View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delivery(DeliveryForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin kiểm tra xe không hợp lệ. Vui lòng kiểm tra lại số km, nhiên liệu và tình trạng từng xe.";
            return RedirectToAction(nameof(Delivery), new { id = form.ContractId });
        }
        try
        {
            var paths = await UploadedImages.SaveAsync(form.Images, env, form.ContractId, ct);
            await handover.RecordDeliveryAsync(new DeliveryRequest(
                form.ContractId, CurrentUserId, form.DeliveredAt ?? VietnamTime.Now, form.Km, form.FuelPercent,
                form.Condition, form.Accessories, form.IdentityAndVehicleChecked, paths, form.Vehicles), ct);
            TempData["Success"] = "Đã bàn giao xe. Hợp đồng chuyển sang Đang thuê.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Delivery), new { id = form.ContractId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Return(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        return contract is null ? NotFound() : View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin kiểm tra xe không hợp lệ. Vui lòng kiểm tra lại số km, nhiên liệu và tình trạng từng xe.";
            return RedirectToAction(nameof(Return), new { id = form.ContractId });
        }
        try
        {
            var paths = await UploadedImages.SaveAsync(form.Images, env, form.ContractId, ct);
            var record = await handover.RecordReturnAsync(new ReturnRequest(
                form.ContractId, CurrentUserId, form.ReturnedAt ?? VietnamTime.Now, form.Km, form.FuelPercent,
                form.Condition, form.Accessories, form.DamageCharge, form.NeedsMaintenance, paths, form.Vehicles), ct);
            TempData["Success"] = $"Đã nhận lại xe. Khoản phát sinh: {Fmt.Vnd(record.ExtraCharges)}.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Return), new { id = form.ContractId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = InspectionRoles)]
    public async Task<IActionResult> Inspection(InspectionForm form, CancellationToken ct)
    {
        try
        {
            await handover.CompleteInspectionAsync(form.VehicleId, CurrentUserId, form.Passed, ct);
            TempData["Success"] = form.Passed ? "Xe kiểm tra đạt và trở về Sẵn sàng." : "Xe đã chuyển sang Bảo trì.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> NoShow(int id, CancellationToken ct)
    {
        try
        {
            var contract = await rentals.CancelContractAsync(id, CurrentUserId, CancellationKind.NoShow,
                "Khách không đến nhận xe (no-show)", ct);
            TempData["Success"] = $"Đã ghi nhận no-show cho {contract.Code}. Giữ 100% tiền cọc theo BR-14.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Contract", "Staff", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> CancelContract(CancelForm form, CancellationToken ct)
    {
        try
        {
            var contract = await rentals.CancelContractAsync(form.ContractId, CurrentUserId, form.Kind, form.Reason, ct);
            TempData["Success"] = $"Đã hủy hợp đồng {contract.Code}. Tiền cọc xử lý theo BR-14.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Contract", "Staff", new { id = form.ContractId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AccountingRoles)]
    public async Task<IActionResult> RecordSettlement(PaymentForm form, CancellationToken ct)
    {
        try
        {
            var payment = await handover.RecordSettlementPaymentAsync(form.ContractId, form.Type, form.Amount,
                form.Method, CurrentUserId, ct);
            TempData["Success"] = $"Đã ghi nhận giao dịch {payment.Code}.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Contract", "Staff", new { id = form.ContractId });
    }

    /// <summary>v8 (#13, FR-4.5): biên bản bàn giao và nhận xe, in được.</summary>
    [HttpGet]
    public async Task<IActionResult> Print(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        if (contract is null || contract.Handover is null) return NotFound();
        return View(new ContractVm
        {
            Contract = contract,
            IsStaff = true,
            Paid = contract.Payments.Where(p => p.Type == PaymentType.Deposit && p.StatusCode == 1).Sum(p => p.Amount),
            Balance = SettlementRules.Balance(contract)
        });
    }
}
