using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Controllers;

public class HomeController(
    RentCarDbContext database, IConfiguration configuration, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string[] types, decimal? maxPrice,
        DateTime? pickupAt, DateTime? returnAt, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || maxPrice is < 0)
            return BadRequest("Bộ lọc không hợp lệ.");

        var isSearch = pickupAt.HasValue || returnAt.HasValue;
        string? error = null;
        var blockingStatuses = configuration.GetSection("Rental:BlockingContractStatuses").Get<int[]>();
        if (isSearch)
        {
            if (pickupAt is null || returnAt is null || returnAt <= pickupAt)
                error = "Vui lòng chọn thời điểm trả sau thời điểm nhận xe.";
            else if (blockingStatuses is null || blockingStatuses.Length == 0)
                error = "Tra cứu lịch thuê đang được cập nhật. Vui lòng liên hệ để xác nhận xe trống.";
        }

        try
        {
            var now = DateTime.UtcNow;
            // Promotions are selected by booking time. Prefer the lowest applicable rate
            // if management data contains overlapping promotions.
            var cars = database.Database.SqlQuery<VehicleSearchResult>($"""
                SELECT x.idXe AS Id, x.tenXe AS Name, l.tenLoai AS VehicleType,
                    x.mauSac AS Color, x.phienBan AS Edition, x.giaThueNgay AS OriginalRate,
                    CAST(x.giaThueNgay - COALESCE(p.Discount, 0) AS decimal(18,2)) AS DailyRate,
                    COALESCE(NULLIF(x.hinhAnhDaiDien, ''), a.ImageUrl) AS ImageUrl
                FROM dbo.xe AS x
                JOIN dbo.loaiXe AS l ON l.idLoaiXe = x.idLoaiXe
                OUTER APPLY (
                    SELECT TOP (1) h.duongDanAnh AS ImageUrl
                    FROM dbo.hinhAnhXe AS h
                    WHERE h.idXe = x.idXe AND NULLIF(h.duongDanAnh, '') IS NOT NULL
                    ORDER BY h.idHinhAnhXe
                ) AS a
                OUTER APPLY (
                    SELECT TOP (1)
                        CASE WHEN k.kieuGiam = 1
                            THEN ROUND(x.giaThueNgay * k.giaTriGiam / 100, 2)
                            WHEN k.giaTriGiam > x.giaThueNgay THEN x.giaThueNgay
                            ELSE k.giaTriGiam END AS Discount
                    FROM dbo.khuyenMaiXe AS kx
                    JOIN dbo.khuyenMai AS k ON k.idKhuyenMai = kx.idKhuyenMai
                    WHERE kx.idXe = x.idXe AND k.trangThai = 1
                        AND k.ngayBatDau <= {now} AND {now} < k.ngayKetThuc
                    ORDER BY Discount DESC, k.idKhuyenMai
                ) AS p
                WHERE x.trangThai = 1
                """);

            if (isSearch && error is null)
            {
                var start = pickupAt!.Value.AddHours(-7);
                var end = returnAt!.Value.AddHours(-7);
                var occupied = await database.Database.SqlQuery<OccupiedVehicle>($"""
                    SELECT c.idXe AS VehicleId, h.trangThai AS Status
                    FROM dbo.chiTietHopDong AS c
                    JOIN dbo.hopDong AS h ON h.idHopDong = c.idHopDong
                    WHERE h.ngayBatDauThue < {end} AND {start} < h.ngayKetThucThue
                    """).ToListAsync(cancellationToken);
                var ids = occupied.Where(item => blockingStatuses!.Contains(item.Status))
                    .Select(item => item.VehicleId).Distinct().ToArray();
                if (ids.Length > 0)
                    cars = cars.Where(car => !ids.Contains(car.Id));
            }

            var options = await cars.GroupBy(car => car.VehicleType).OrderBy(group => group.Key)
                .Select(group => new FilterOption<string>(group.Key, group.Count()))
                .ToListAsync(cancellationToken);
            var highest = await cars.MaxAsync(car => (decimal?)car.DailyRate, cancellationToken) ?? 0;
            var selected = Math.Min(maxPrice ?? highest, highest);
            if (types.Length > 0)
                cars = cars.Where(car => types.Contains(car.VehicleType));
            cars = cars.Where(car => car.DailyRate <= selected);
            var count = await cars.CountAsync(cancellationToken);
            var results = error is null
                ? await cars.OrderBy(car => car.DailyRate).ThenBy(car => car.Id)
                    .Take(12).ToListAsync(cancellationToken)
                : [];
            return View(new VehicleFilterViewModel(options, types, highest, selected,
                count, pickupAt, returnAt, isSearch, error, results));
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "Could not load the vehicle catalogue.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View();
        }
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public class OccupiedVehicle
{
    public int VehicleId { get; set; }
    public int Status { get; set; }
}
