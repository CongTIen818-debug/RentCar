using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.WebUtilities;
using RentCar.Domain.Common;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

[Authorize(Roles = "KHACHHANG")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("yeu-cau-cua-toi")]
public sealed class MyRequestsController(CustomerRequests requests, IConfiguration configuration, ILogger<MyRequestsController> logger) : Controller
{
    private int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool WantsJson => Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, int? status = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || (status.HasValue && status is not (1 or 2 or 3))) return BadRequest("Bộ lọc không hợp lệ.");
        return View(await LoadAsync(page, status, cancellationToken));
    }
    [HttpGet("danh-sach")]
    public async Task<IActionResult> Cards(int page = 1, int? status = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || (status.HasValue && status is not (1 or 2 or 3))) return BadRequest();
        var model = await LoadAsync(page, status, cancellationToken);
        if (model.Error is not null) return StatusCode(503, new { message = model.Error });
        return PartialView("_Cards", model);
    }
    private async Task<CustomerRequestsPage> LoadAsync(int page, int? status, CancellationToken ct)
    {
        try { return await requests.ListAsync(CustomerId, page, status, ct); }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Unable to load customer request history.");
            Response.StatusCode = 503;
            return new([], new(), 1, 1, status, 0, "Chưa tải được yêu cầu của bạn. Vui lòng thử lại sau.");
        }
    }
    [HttpGet("thong-bao-chua-doc")]
    public async Task<IActionResult> Unread(CancellationToken cancellationToken)
    {
        try { return Json(new { count = await requests.UnreadAsync(CustomerId, cancellationToken) }); }
        catch (SqlException ex) { logger.LogWarning(ex, "Unable to load request notice count."); return StatusCode(503); }
    }
    [HttpPost("{id:int}/huy")]
    public async Task<IActionResult> Cancel(int id, int page = 1, int? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await requests.CancelAsync(CustomerId, id, cancellationToken);
            if (WantsJson) return Json(new { message = "Đã hủy yêu cầu. Yêu cầu vẫn được lưu trong lịch sử." });
            TempData["RequestSuccess"] = "Đã hủy yêu cầu. Yêu cầu vẫn được lưu trong lịch sử.";
        }
        catch (DomainException ex)
        {
            if (WantsJson) return StatusCode(ex.StatusCode, new { message = ex.Message });
            if (ex.StatusCode == 404) return NotFound();
            TempData["RequestError"] = ex.Message;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Unable to cancel customer request.");
            if (WantsJson) return StatusCode(503, new { message = "Chưa hủy được yêu cầu. Vui lòng thử lại." });
            TempData["RequestError"] = "Chưa hủy được yêu cầu. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(Index), new { page = Math.Max(1,page), status = status is 1 or 2 or 3 ? status : null });
    }
    [HttpPost("{id:int}/da-xem")]
    public async Task<IActionResult> Read(int id, string version, int page = 1, int? status = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(version)) return BadRequest();
        try
        {
            await requests.MarkReadAsync(CustomerId, id, version, cancellationToken);
            if (WantsJson) return Json(new { count = await requests.UnreadAsync(CustomerId, cancellationToken) });
            return RedirectToAction(nameof(Index), new { page, status });
        }
        catch (DomainException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (SqlException ex) { logger.LogWarning(ex,"Unable to mark approval read."); return StatusCode(503); }
    }
    [HttpGet("{id:int}/thanh-toan")]
    public async Task<IActionResult> Payment(int id, CancellationToken cancellationToken)
    {
        try
        {
            var card = await requests.FindAsync(CustomerId, id, cancellationToken);
            if (card is null) return NotFound();
            if (!card.CanPay)
            {
                TempData["RequestError"] = card.IsExpired
                    ? "Đã quá thời hạn thanh toán cọc 24 tiếng, vui lòng gửi lại yêu cầu."
                    : card.Request.HasDeposit ? "Yêu cầu đã được ghi nhận thanh toán cọc." : "Yêu cầu chưa đủ điều kiện thanh toán cọc.";
                return RedirectToAction(nameof(Index));
            }
            var paymentPath = configuration["CustomerRequests:PaymentPath"];
            if (Url.IsLocalUrl(paymentPath) && !paymentPath!.StartsWith("/yeu-cau-cua-toi", StringComparison.OrdinalIgnoreCase))
                return LocalRedirect(QueryHelpers.AddQueryString(paymentPath, "requestId", id.ToString()));
            return View(card);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,"Unable to load request for deposit.");
            TempData["RequestError"]="Chưa tải được thông tin đặt cọc. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }
    }
}
