using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Application.Rentals;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
using RentCar.Web.Helpers;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>Khu vực khách hàng: gửi yêu cầu thuê và xem hợp đồng của chính mình (FR-3.1, FR-6.2a).</summary>
[Authorize(Roles = RoleCodes.Customer)]
public sealed class RentalsController(RentalService rentals) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var vm = new MyRentalsVm
        {
            Requests = await rentals.GetRequestsForCustomerAsync(CurrentUserId, ct),
            Contracts = await rentals.GetContractsForCustomerAsync(CurrentUserId, ct)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RentalRequestForm form, CancellationToken ct)
    {
        try
        {
            var request = await rentals.CreateRequestAsync(CurrentUserId, null, form.VehicleId,
                form.StartAt, form.EndAt, form.PickupLocationId, form.ReturnLocationId, ct);
            TempData["Success"] = $"Đã gửi yêu cầu thuê. Xe được giữ chỗ đến {Fmt.When(request.HoldUntil)}.";
            return RedirectToAction(nameof(Mine));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Catalog",
                new { id = form.VehicleId, startAt = form.StartAt, endAt = form.EndAt });
        }
    }

    /// <summary>FR-3.5: khách hủy yêu cầu của chính mình khi chưa lập hợp đồng.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id, CancellationToken ct)
    {
        try
        {
            await rentals.CancelRequestAsync(id, CurrentUserId, isStaff: false, reason: null, ct);
            TempData["Success"] = "Đã hủy yêu cầu thuê. Xe không còn được giữ chỗ cho bạn.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    public async Task<IActionResult> Contract(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, CurrentUserId, ct);
        if (contract is null) return NotFound();

        var paid = contract.Payments
            .Where(p => p.Type == PaymentType.Deposit && p.StatusCode == 1)
            .Sum(p => p.Amount);

        return View("~/Areas/QuanLy/Views/Staff/Contract.cshtml", new ContractVm
        {
            Contract = contract,
            IsStaff = false,
            Paid = paid,
            Balance = SettlementRules.Balance(contract)
        });
    }

    /// <summary>v8 (#13): khách in hợp đồng của chính mình.</summary>
    [HttpGet]
    public async Task<IActionResult> ContractPrint(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, CurrentUserId, ct);
        if (contract is null) return NotFound();
        return View("~/Areas/QuanLy/Views/Staff/ContractPrint.cshtml", new ContractVm
        {
            Contract = contract,
            IsStaff = false,
            Paid = contract.Payments.Where(p => p.Type == PaymentType.Deposit && p.StatusCode == 1).Sum(p => p.Amount),
            Balance = SettlementRules.Balance(contract)
        });
    }

    /// <summary>v8 (#13): khách xem biên nhận đặt cọc của chính mình.</summary>
    [HttpGet]
    public async Task<IActionResult> DepositReceipt(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, CurrentUserId, ct);
        if (contract is null) return NotFound();
        var lines = await rentals.GetDepositReceiptsAsync(id, ct);
        return View("~/Areas/QuanLy/Views/Staff/DepositReceipt.cshtml", new DepositReceiptVm { Contract = contract, IsStaff = false, Lines = lines });
    }
}
