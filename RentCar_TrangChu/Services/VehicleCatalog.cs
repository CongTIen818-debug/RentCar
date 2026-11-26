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
                    x.hangXe AS Brand, x.namSanXuat AS Year, x.trangThai AS Status,
                    CAST(CASE WHEN x.trangThai IN (1,2) THEN 1 ELSE 0 END AS bit) AS IsReady,
                    CAST(x.soCho AS int) AS Seats, x.hopSo AS Transmission, x.nhienLieu AS Fuel,
                    x.giaThueTuan AS WeeklyRate, x.tienDatCoc AS SecurityDeposit,
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
                        ROUND(x.giaThueNgay * u.phanTramGiam / 100, 2) AS Discount
                    FROM dbo.uuDaiXe AS ux
                    JOIN dbo.uuDai AS u ON u.idUuDai = ux.idUuDai
                    WHERE ux.idXe = x.idXe AND u.trangThai = 1
                        AND u.ngayBatDau <= {now} AND {now} < u.ngayKetThuc
                    ORDER BY Discount DESC, u.idUuDai
                ) AS p
                WHERE ({readyOnly} = CAST(0 AS bit) OR x.trangThai IN (1,2))
                """);
    }

    public async Task<IQueryable<VehicleSearchResult>> AvailableAsync(
        IQueryable<VehicleSearchResult> cars, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var occupied = await database.Database.SqlQuery<OccupiedVehicle>($"""
            SELECT DISTINCT idXe AS VehicleId
            FROM dbo.vLichXe
            WHERE {start} < ketThuc AND batDau < {end}
            """).ToListAsync(cancellationToken);
        var ids = occupied.Select(item => item.VehicleId).ToArray();
        return ids.Length == 0 ? cars : cars.Where(car => !ids.Contains(car.Id));
    }

    // Location catalogue shared by request forms; keep historical placeholders out of new bookings.
    public Task<List<PickupLocationOption>> LocationsAsync(CancellationToken ct) =>
        database.Database.SqlQuery<PickupLocationOption>($"""
            SELECT idDiem AS Id, tenDiem AS Name, diaChi AS Address FROM dbo.diemGiaoNhan
            WHERE trangThai=1 AND tenDiem<>N'Chưa xác định - hợp đồng cũ'
            """).OrderBy(l => l.Name).ToListAsync(ct);

    public int Seats(VehicleSearchResult car) => car.Seats;

    public VehicleProfile? Profile(VehicleSearchResult car)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "vehicle-profiles.json");
        var profiles = JsonSerializer.Deserialize<List<VehicleProfile>>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var profile = profiles.FirstOrDefault(item =>
            string.Equals(item.Name, car.Name, StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(item.Edition ?? "", car.Edition ?? "", StringComparison.OrdinalIgnoreCase) ||
             (car.Edition == "Tiêu chuẩn" && string.IsNullOrWhiteSpace(item.Edition))));
        // Structured seat counts come from dbo.xe, not the descriptive profile file.
        profile?.Specifications.Remove("Số chỗ tham khảo");
        return profile;
    }
}

public class OccupiedVehicle
{
    public int VehicleId { get; set; }
}
