using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Domain.Common;
using RentCar.Models;

namespace RentCar.Services;

// Database lưu giờ Việt Nam; DTO *Utc chỉ chuyển sang UTC tại biên JSON/đếm ngược.
// Chỉ đọc yêu cầu của khách; chỉ hủy yêu cầu đang chờ và ghi sự kiện đã xem. Không lập/sửa hợp đồng.
public sealed class CustomerRequests(RentCarDbContext database, TimeProvider clock)
{
    public const int PageSize = 8;
    private DateTime NowUtc => clock.GetUtcNow().UtcDateTime;

    private static FormattableString Query(int customerId) => $"""
        SELECT r.idYeuCau AS Id, r.trangThai AS Status, DATEADD(HOUR,-7,r.ngayTao) AS CreatedAtUtc,
               DATEADD(HOUR,-7,r.ngayNhan) AS PickupAtUtc, DATEADD(HOUR,-7,r.ngayTra) AS ReturnAtUtc, DATEADD(HOUR,-7,r.hanGiuCho) AS DeadlineUtc,
               pn.tenDiem AS PickupLocation, pt.tenDiem AS ReturnLocation,
               CONVERT(decimal(18,2), COALESCE(h.tienDatCoc,
                   (SELECT SUM(x.tienDatCoc) FROM dbo.chiTietYeuCauThue d JOIN dbo.xe x ON x.idXe=d.idXe WHERE d.idYeuCau=r.idYeuCau), 0)) AS RequiredDeposit,
               CONVERT(decimal(18,2),COALESCE(paid.Amount,0)) AS PaidDeposit,
               CAST(CASE WHEN r.trangThai=1 AND r.hanGiuCho<=DATEADD(HOUR,7,SYSUTCDATETIME()) THEN 1 ELSE 0 END AS bit) AS ExpiredPending,
               CAST(CASE WHEN COALESCE(paid.Amount,0)>0 AND paid.Amount>=COALESCE(h.tienDatCoc,0) THEN 1 ELSE 0 END AS bit) AS HasDeposit,
               CAST(CASE WHEN r.trangThai=2 AND NOT EXISTS
                   (SELECT 1 FROM dbo.nhatKyKiemToan a WHERE a.idNguoiThucHien=r.idNguoiDung
                    AND a.doiTuong='YeuCauThue' AND a.idDoiTuong=CONVERT(varchar(50),r.idYeuCau)
                    AND a.hanhDong='KHACH_XEM_DUYET'
                    AND a.giaTriMoi=CONCAT(N'deadline=',CONVERT(nvarchar(33),DATEADD(HOUR,-7,r.hanGiuCho),126))) THEN 1 ELSE 0 END AS bit) AS HasUnreadApproval
        FROM dbo.yeuCauThue r
        JOIN dbo.diemGiaoNhan pn ON pn.idDiem=r.idDiemNhan
        JOIN dbo.diemGiaoNhan pt ON pt.idDiem=r.idDiemTra
        LEFT JOIN dbo.hopDong h ON h.idYeuCau=r.idYeuCau
        OUTER APPLY (SELECT SUM(t.soTien) AS Amount FROM dbo.thanhToan t
                     WHERE t.idHopDong=h.idHopDong AND t.loaiThanhToan=1 AND t.trangThai=1) paid
        WHERE r.idNguoiDung={customerId}
        """;

    public async Task<CustomerRequestsPage> ListAsync(int customerId, int page, int? status, CancellationToken ct)
    {
        var counts = await database.Database.SqlQuery<CustomerRequestCounts>($"""
            SELECT COUNT(*) AS Total,
                COALESCE(SUM(CASE WHEN trangThai=1 THEN 1 ELSE 0 END),0) AS Pending,
                COALESCE(SUM(CASE WHEN trangThai=2 THEN 1 ELSE 0 END),0) AS Approved,
                COALESCE(SUM(CASE WHEN trangThai=3 THEN 1 ELSE 0 END),0) AS Cancelled
            FROM dbo.yeuCauThue WHERE idNguoiDung={customerId}
            """).SingleAsync(ct);
        var query = database.Database.SqlQuery<CustomerRequestRow>(Query(customerId));
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        var total = status switch { 1 => counts.Pending, 2 => counts.Approved, 3 => counts.Cancelled, _ => counts.Total };
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);
        var vehicles = await VehiclesAsync(customerId, rows.Select(r => r.Id).ToArray(), ct);
        var now = NowUtc;
        return new(rows.Select(r => new CustomerRequestCard(r, vehicles.Where(v => v.RequestId == r.Id).ToList(), now)).ToList(),
            counts, page, pages, status, await UnreadAsync(customerId, ct));
    }
    public async Task<CustomerRequestCard?> FindAsync(int customerId, int id, CancellationToken ct)
    {
        var row = await database.Database.SqlQuery<CustomerRequestRow>(Query(customerId)).SingleOrDefaultAsync(r => r.Id == id, ct);
        return row is null ? null : new(row, await VehiclesAsync(customerId, [id], ct), NowUtc);
    }
    private async Task<List<CustomerRequestVehicle>> VehiclesAsync(int customerId, int[] requestIds, CancellationToken ct)
    {
        if (requestIds.Length == 0) return [];
        return await database.Database.SqlQuery<CustomerRequestVehicle>($"""
            SELECT d.idYeuCau AS RequestId, x.idXe AS VehicleId, x.tenXe AS Name, x.bienSo AS Plate,
                x.hinhAnhDaiDien AS ImageUrl, d.coTaiXe AS WithDriver
            FROM dbo.chiTietYeuCauThue d JOIN dbo.yeuCauThue r ON r.idYeuCau=d.idYeuCau
            JOIN dbo.xe x ON x.idXe=d.idXe WHERE r.idNguoiDung={customerId}
            """).Where(v => requestIds.Contains(v.RequestId)).OrderBy(v => v.RequestId).ThenBy(v => v.VehicleId).ToListAsync(ct);
    }
    public Task<int> UnreadAsync(int customerId, CancellationToken ct) =>
        database.Database.SqlQuery<CustomerRequestRow>(Query(customerId)).CountAsync(r => r.HasUnreadApproval, ct);

    public async Task CancelAsync(int customerId, int requestId, CancellationToken ct)
    {
        if (await FindAsync(customerId, requestId, ct) is null) throw new DomainException("Không tìm thấy yêu cầu.", 404);
        await using var tx = await database.Database.BeginTransactionAsync(ct);
        var result = await database.Database.SqlQuery<int>($"""
            SET NOCOUNT ON;
            DECLARE @changed TABLE(Id int);
            UPDATE dbo.yeuCauThue WITH (UPDLOCK)
            SET trangThai=3 OUTPUT inserted.idYeuCau INTO @changed
            WHERE idYeuCau={requestId} AND idNguoiDung={customerId} AND trangThai=1;
            INSERT dbo.nhatKyKiemToan (idNguoiThucHien,doiTuong,idDoiTuong,hanhDong,giaTriCu,giaTriMoi,thoiDiem)
            SELECT {customerId},'YeuCauThue',CONVERT(varchar(50),Id),'KHACH_HUY_YEU_CAU',N'trangThai=1',N'trangThai=3',DATEADD(HOUR,7,SYSUTCDATETIME()) FROM @changed;
            SELECT COUNT(*) AS [Value] FROM @changed;
            """).ToListAsync(ct);
        if (result.Single() == 0) throw new DomainException("Yêu cầu đã được xử lý. Bạn chỉ có thể hủy khi đang chờ duyệt.", 409);
        await tx.CommitAsync(ct);
    }
    public async Task MarkReadAsync(int customerId, int requestId, string version, CancellationToken ct)
    {
        var card = await FindAsync(customerId, requestId, ct) ?? throw new DomainException("Không tìm thấy yêu cầu.", 404);
        if (card.Request.Status != 2 || card.Version != version)
            throw new DomainException("Thông báo đã thay đổi. Vui lòng tải lại danh sách.", 409);
        // Dùng nhật ký sẵn có làm dấu đã xem bền vững, theo khách + yêu cầu + phiên bản hạn cọc.
        // Không đổi trạng thái yêu cầu, không thêm bảng và không sửa thông báo của quản lý.
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT dbo.nhatKyKiemToan (idNguoiThucHien,doiTuong,idDoiTuong,hanhDong,giaTriMoi,thoiDiem)
            SELECT {customerId},'YeuCauThue',CONVERT(varchar(50),r.idYeuCau),'KHACH_XEM_DUYET',
                   CONCAT(N'deadline=',CONVERT(nvarchar(33),DATEADD(HOUR,-7,r.hanGiuCho),126)),DATEADD(HOUR,7,SYSUTCDATETIME())
            FROM dbo.yeuCauThue r WHERE r.idNguoiDung={customerId} AND r.idYeuCau={requestId} AND r.trangThai=2
              AND CONVERT(nvarchar(33),DATEADD(HOUR,-7,r.hanGiuCho),126)={version}
              AND NOT EXISTS (SELECT 1 FROM dbo.nhatKyKiemToan a WITH (UPDLOCK,HOLDLOCK)
                  WHERE a.idNguoiThucHien={customerId} AND a.doiTuong='YeuCauThue'
                    AND a.idDoiTuong=CONVERT(varchar(50),r.idYeuCau) AND a.hanhDong='KHACH_XEM_DUYET'
                    AND a.giaTriMoi=CONCAT(N'deadline=',CONVERT(nvarchar(33),DATEADD(HOUR,-7,r.hanGiuCho),126)));
            """, ct);
    }
}
