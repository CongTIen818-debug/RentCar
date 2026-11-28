using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Domain.Common;
namespace RentCar.Services;
public sealed class CustomerNotice
{
 public string Key {get;set;}="";public int? ContractId {get;set;}public string Message {get;set;}="";public int? SendStatus {get;set;}public DateTime AtUtc {get;set;}public bool IsRead {get;set;}
}
public sealed record CustomerNoticePage(IReadOnlyList<CustomerNotice> Items,int Total,int Unread,int Page,int Pages);
// Read existing notifications and derive reminders; never send emails or change request/contract status.
public sealed class CustomerNotifications(RentCarDbContext db)
{
 private static FormattableString Query(int userId)=>$"""
 SELECT e.[Key],e.ContractId,e.Message,e.SendStatus,DATEADD(HOUR,-7,e.AtUtc) AtUtc,
 CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.nhatKyKiemToan a WHERE a.idNguoiThucHien={userId} AND a.doiTuong='ThongBaoKhach' AND a.idDoiTuong=e.[Key] AND a.hanhDong='KHACH_DOC_THONG_BAO') THEN 1 ELSE 0 END AS bit) IsRead
 FROM (
 SELECT CONCAT('n-',n.idThongBao) AS [Key],n.idHopDong ContractId,n.noiDung Message,n.trangThaiGui SendStatus,n.thoiDiem AtUtc
 FROM dbo.thongBao n WHERE n.idNguoiDung={userId}
 UNION ALL
 SELECT CONCAT('h-',s.idLichSu),h.idHopDong,
 CONCAT(N'Hợp đồng ',h.maHopDong,N': ',CASE s.trangThaiMoi WHEN 0 THEN N'Chờ xác nhận' WHEN 1 THEN N'Đã xác nhận' WHEN 2 THEN N'Đang thuê' WHEN 3 THEN N'Hoàn thành' ELSE N'Đã hủy' END,N'. ',COALESCE(s.ghiChu,N'')),CAST(NULL AS int),s.thoiDiem
 FROM dbo.lichSuHopDong s JOIN dbo.hopDong h ON h.idHopDong=s.idHopDong WHERE h.idNguoiDung={userId}
 AND NOT EXISTS(SELECT 1 FROM dbo.thongBao n WHERE n.idNguoiDung={userId} AND n.idHopDong=h.idHopDong
 AND ((s.trangThaiMoi=1 AND n.loai='XAC_NHAN_HOP_DONG') OR(s.trangThaiMoi=4 AND n.loai='HUY_HOP_DONG')) AND ABS(DATEDIFF_BIG(second,n.thoiDiem,s.thoiDiem))<=60)
 UNION ALL
 SELECT CONCAT(CASE WHEN h.ngayKetThucThue<DATEADD(HOUR,7,SYSUTCDATETIME()) THEN 'late-' ELSE 'due-' END,h.idHopDong,'-',CONVERT(varchar(19),DATEADD(HOUR,-7,h.ngayKetThucThue),126)),h.idHopDong,
 CONCAT(N'Hợp đồng ',h.maHopDong,CASE WHEN h.ngayKetThucThue<DATEADD(HOUR,7,SYSUTCDATETIME()) THEN N' đã quá giờ trả xe.' ELSE N' sắp đến giờ trả xe.' END,N' Giờ trả dự kiến ',CONVERT(varchar(16),h.ngayKetThucThue,120),N' (giờ Việt Nam). Vui lòng liên hệ nhân viên nếu cần hỗ trợ.'),CAST(NULL AS int),DATEADD(hour,-24,h.ngayKetThucThue)
 FROM dbo.hopDong h LEFT JOIN dbo.bienBanBanGiao b ON b.idHopDong=h.idHopDong
 WHERE h.idNguoiDung={userId} AND h.trangThai IN(1,2) AND b.ngayNhanLai IS NULL
 AND h.ngayKetThucThue<=DATEADD(hour,24,DATEADD(HOUR,7,SYSUTCDATETIME())) AND h.ngayKetThucThue>=DATEADD(day,-30,DATEADD(HOUR,7,SYSUTCDATETIME()))
 AND (h.ngayKetThucThue>=DATEADD(HOUR,7,SYSUTCDATETIME()) OR h.trangThai=2)
 ) e
 """;
 public async Task<CustomerNoticePage> ListAsync(int userId,int page,CancellationToken ct)
 {
  var q=db.Database.SqlQuery<CustomerNotice>(Query(userId));var count=await q.CountAsync(ct);var pages=Math.Max(1,(int)Math.Ceiling(count/20d));page=Math.Clamp(page,1,pages);
  return new(await q.OrderByDescending(n=>n.AtUtc).ThenByDescending(n=>n.Key).Skip((page-1)*20).Take(20).ToListAsync(ct),count,await q.CountAsync(n=>!n.IsRead,ct),page,pages);
 }
 public async Task MarkReadAsync(int userId,string key,CancellationToken ct)
 {
  if(key.Length>50 || !await db.Database.SqlQuery<CustomerNotice>(Query(userId)).AnyAsync(n=>n.Key==key,ct))throw new DomainException("Không tìm thấy thông báo.",404);
  await db.Database.ExecuteSqlInterpolatedAsync($"""
   INSERT dbo.nhatKyKiemToan(idNguoiThucHien,doiTuong,idDoiTuong,hanhDong,thoiDiem)
   SELECT {userId},'ThongBaoKhach',{key},'KHACH_DOC_THONG_BAO',DATEADD(HOUR,7,SYSUTCDATETIME())
   WHERE NOT EXISTS(SELECT 1 FROM dbo.nhatKyKiemToan WITH(UPDLOCK,HOLDLOCK) WHERE idNguoiThucHien={userId} AND doiTuong='ThongBaoKhach' AND idDoiTuong={key} AND hanhDong='KHACH_DOC_THONG_BAO')
   """,ct);
 }
}
