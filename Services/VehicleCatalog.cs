using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Services;

public class VehicleCatalog(RentCarDbContext database, IWebHostEnvironment environment)
{
    public IQueryable<VehicleSearchResult> Query(bool readyOnly = true)
    {
        var now = DateTime.UtcNow;
        return database.Database.SqlQuery<VehicleSearchResult>($"""
                SELECT x.idXe AS Id, x.tenXe AS Name, l.tenLoai AS VehicleType,
                    x.hangXe AS Brand, x.namSanXuat AS Year, x.trangThai AS IsReady,
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
                WHERE ({readyOnly} = CAST(0 AS bit) OR x.trangThai = 1)
                """);
    }

    public async Task<IQueryable<VehicleSearchResult>> AvailableAsync(
        IQueryable<VehicleSearchResult> cars, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var deposited = (int)TrangThaiHopDong.DaDatCoc;
        var received = (int)TrangThaiHopDong.DaNhanXe;
        var occupied = await database.Database.SqlQuery<OccupiedVehicle>($"""
            SELECT DISTINCT c.idXe AS VehicleId
            FROM dbo.chiTietHopDong AS c
            JOIN dbo.hopDong AS h ON h.idHopDong = c.idHopDong
            WHERE h.ngayBatDauThue < {end}
                AND (
                    (h.trangThai = {deposited} AND {start} < h.ngayKetThucThue)
                    OR (h.trangThai = {received}
                        AND ({start} < h.ngayKetThucThue OR h.ngayKetThucThue <= {now}))
                )
            """).ToListAsync(cancellationToken);
        var ids = occupied.Select(item => item.VehicleId).ToArray();
        return ids.Length == 0 ? cars : cars.Where(car => !ids.Contains(car.Id));
    }

    public VehicleProfile? Profile(VehicleSearchResult car)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "vehicle-profiles.json");
        var profiles = JsonSerializer.Deserialize<List<VehicleProfile>>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return profiles.FirstOrDefault(profile =>
            string.Equals(profile.Name, car.Name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(profile.Edition, car.Edition, StringComparison.OrdinalIgnoreCase));
    }
}

public class OccupiedVehicle
{
    public int VehicleId { get; set; }
}
