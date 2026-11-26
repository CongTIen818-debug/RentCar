using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RentCar.Domain.Enums;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.Controllers;

[Authorize(Roles = "KHACHHANG")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("hop-dong-cua-toi")]
public sealed class MyContractsController(CustomerContracts contracts, ILogger<MyContractsController> logger) : Controller
{
    private int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, ContractStatus? status = null, string? q = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || (status.HasValue && !Enum.IsDefined(status.Value)) || q?.Length > 60) return BadRequest("Bộ lọc hợp đồng không hợp lệ.");
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        try { return View(await contracts.ListAsync(CustomerId, page, status, q, cancellationToken)); }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Unable to load customer contracts."); Response.StatusCode = 503;
            return View(new CustomerContractsPage([], [], 0, 1, 1, status, q, "Chưa tải được hợp đồng. Vui lòng thử lại sau."));
        }
    }
    [HttpGet("{id:int}")]
    public Task<IActionResult> Details(int id, CancellationToken cancellationToken) => Show(id, "Details", cancellationToken);
    [HttpGet("{id:int}/in")]
    public Task<IActionResult> Print(int id, CancellationToken cancellationToken) => Show(id, "Print", cancellationToken);
    private async Task<IActionResult> Show(int id, string viewName, CancellationToken ct)
    {
        try
        {
            var model = await contracts.FindAsync(CustomerId, id, ct);
            return model is null ? NotFound() : View(viewName, model);
        }
        catch (SqlException ex) { logger.LogError(ex, "Unable to load customer contract {Id}.", id); return StatusCode(503, "Chưa tải được hợp đồng. Vui lòng thử lại sau."); }
    }
    [HttpGet("{id:int}/bien-nhan-coc")]
    public async Task<IActionResult> Receipt(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Check ownership before querying receipt lines, including when opening a copied direct link.
            var model = await contracts.FindAsync(CustomerId, id, cancellationToken);
            if (model is null) return NotFound();
            return View(new CustomerDepositReceipt(model.Contract, await contracts.ReceiptsAsync(id, cancellationToken)));
        }
        catch (SqlException ex) { logger.LogError(ex, "Unable to load customer deposit receipt {Id}.", id); return StatusCode(503, "Chưa tải được biên nhận. Vui lòng thử lại sau."); }
    }
}
