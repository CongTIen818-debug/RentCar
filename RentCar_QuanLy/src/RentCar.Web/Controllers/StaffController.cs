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

/// <summary>
/// Khu vực nhân viên: lập, xác nhận hợp đồng và ghi nhận đặt cọc (UC-02, FR-3.3).
/// Phân quyền kiểm tra ở máy chủ theo ma trận mục 2.2 (NFR-03).
/// </summary>
[Authorize(Roles = StaffViewRoles)]
public sealed class StaffController(RentalService rentals) : Controller
{
    private const string StaffViewRoles = RoleCodes.Sales + "," + RoleCodes.Accounting + "," + RoleCodes.Admin;
    private const string SalesRoles = RoleCodes.Sales + "," + RoleCodes.Admin;
    private const string AccountingRoles = RoleCodes.Accounting + "," + RoleCodes.Admin;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new StaffIndexVm
        {
            Requests = await rentals.GetRequestsForStaffAsync(ct),
            Contracts = await rentals.GetContractsForStaffAsync(ct)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Contract(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        if (contract is null) return NotFound();

        var checks = await rentals.EvaluateChecksAsync(contract, ct);
        var vm = new ContractVm
        {
            Contract = contract,
            IsStaff = true,
            Checks = checks,
            CanConfirm = ConfirmationRules.CanConfirm(checks),
            Paid = contract.Payments
                .Where(p => p.Type == PaymentType.Deposit && p.StatusCode == 1)
                .Sum(p => p.Amount),
            Balance = SettlementRules.Balance(contract),
            NoShowAvailable = contract.Status == ContractStatus.Confirmed
                              && contract.Handover is null
                              && VietnamTime.Now >= contract.StartAt.AddHours(BookingRules.NoShowHours)
        };
        return View(vm);
    }

    /// <summary>UC-02 A7: nhân viên đặt xe hộ khách đã có hồ sơ. Gửi từ trang chi tiết xe.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> CreateRequest(RentalRequestForm form, string? customerKey, CancellationToken ct)
    {
        try
        {
            var request = await rentals.CreateRequestForCustomerAsync(CurrentUserId, customerKey, form.VehicleId,
                form.StartAt, form.EndAt, form.PickupLocationId, form.ReturnLocationId, ct);
            TempData["Success"] = $"Đã đặt xe hộ khách {request.Customer.FullName}. Xe được giữ chỗ đến {Fmt.When(request.HoldUntil)}; hãy lập hợp đồng trước thời điểm này.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Catalog", new
            {
                id = form.VehicleId,
                startAt = form.StartAt.ToString("yyyy-MM-ddTHH:mm"),
                endAt = form.EndAt.ToString("yyyy-MM-ddTHH:mm"),
                customer = customerKey
            });
        }
    }

    /// <summary>FR-3.5: nhân viên hủy yêu cầu thuê chưa lập hợp đồng; bắt buộc có lý do.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> CancelRequest(int id, string? reason, CancellationToken ct)
    {
        try
        {
            await rentals.CancelRequestAsync(id, CurrentUserId, isStaff: true, reason, ct);
            TempData["Success"] = $"Đã hủy yêu cầu thuê #{id}. Xe không còn bị giữ chỗ.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> CreateContract(int requestId, string? promotionCode, CancellationToken ct)
    {
        try
        {
            var contract = await rentals.CreateContractFromRequestAsync(requestId, CurrentUserId, promotionCode, ct);
            TempData["Success"] = $"Đã lập hợp đồng {contract.Code} ở trạng thái Chờ xác nhận.";
            return RedirectToAction(nameof(Contract), new { id = contract.Id });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = SalesRoles)]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        try
        {
            var contract = await rentals.ConfirmContractAsync(id, CurrentUserId, ct);
            TempData["Success"] = $"Đã xác nhận hợp đồng {contract.Code}.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Contract), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AccountingRoles)]
    public async Task<IActionResult> RecordPayment(PaymentForm form, CancellationToken ct)
    {
        try
        {
            var payment = await rentals.RecordPaymentAsync(form.ContractId, form.Type, form.Amount,
                form.Method ?? "Tiền mặt", CurrentUserId, ct);
            TempData["Success"] = $"Đã ghi nhận giao dịch {payment.Code}.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (FieldValidationException ex)
        {
            TempData["Error"] = string.Join(" ", ex.Errors.Values);
        }
        return RedirectToAction(nameof(Contract), new { id = form.ContractId });
    }

    /// <summary>v8 (#13, FR-3.4): bản in hợp đồng.</summary>
    [HttpGet]
    public async Task<IActionResult> ContractPrint(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        if (contract is null) return NotFound();
        return View(new ContractVm
        {
            Contract = contract,
            IsStaff = true,
            Paid = contract.Payments.Where(p => p.Type == PaymentType.Deposit && p.StatusCode == 1).Sum(p => p.Amount),
            Balance = SettlementRules.Balance(contract)
        });
    }

    /// <summary>v8 (#13, FR-3.4): biên nhận các khoản đặt cọc.</summary>
    [HttpGet]
    public async Task<IActionResult> DepositReceipt(int id, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(id, null, ct);
        if (contract is null) return NotFound();
        var lines = await rentals.GetDepositReceiptsAsync(id, ct);
        return View(new DepositReceiptVm { Contract = contract, IsStaff = true, Lines = lines });
    }
}
