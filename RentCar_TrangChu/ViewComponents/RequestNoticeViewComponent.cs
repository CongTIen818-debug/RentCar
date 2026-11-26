using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RentCar.Models;
using RentCar.Services;

namespace RentCar.ViewComponents;

public sealed class RequestNoticeViewComponent(CustomerRequests requests, ILogger<RequestNoticeViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = 0;
        if (UserClaimsPrincipal.IsInRole("KHACHHANG") && int.TryParse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            try { count = await requests.UnreadAsync(id, HttpContext.RequestAborted); }
            catch (SqlException ex) { logger.LogWarning(ex,"Request notification count unavailable."); }
        return View(new RequestNoticeState(count));
    }
}
