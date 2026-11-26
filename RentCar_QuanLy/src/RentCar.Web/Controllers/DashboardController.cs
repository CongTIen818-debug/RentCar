using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Application.Dashboard;
using RentCar.Domain.Common;
using RentCar.Domain.Enums;

namespace RentCar.Web.Controllers;

/// <summary>v9: trang Tổng quan (KPI và biểu đồ doanh thu), chỉ Quản trị viên.</summary>
[Authorize(Roles = RoleCodes.Admin)]
public sealed class DashboardController(DashboardService dashboard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await dashboard.GetAsync(VietnamTime.Now, ct));
}
