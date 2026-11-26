/* Địa chỉ tham khảo cho đồ án, không khẳng định liên kết kinh doanh với các showroom.
Nguồn tra cứu 26/11/2026:
https://www.bmw.vn/vi/find-us/
https://www.mercedes-benz.com.vn/content/dam/vietnam/passengercars/the-brand/mbv/MBV_CS_booklet_2025updated.pdf
Chạy lại an toàn: chỉ thêm địa điểm chưa có, không xóa/sửa dữ liệu lịch sử.
*/
USE [RentCarDb_V2];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Locations TABLE (tenDiem nvarchar(150), diaChi nvarchar(255));
INSERT @Locations VALUES
(N'BMW Long Biên', N'01 Ngô Gia Tự, phường Bồ Đề, Hà Nội'),
(N'BMW Lê Duẩn', N'132 Lê Duẩn, phường Văn Miếu – Quốc Tử Giám, Hà Nội'),
(N'BMW Lê Văn Lương', N'68 Lê Văn Lương, phường Thanh Xuân, Hà Nội'),
(N'Mercedes-Benz Haxaco Láng Hạ', N'46 Láng Hạ, phường Láng, Hà Nội'),
(N'Mercedes-Benz An Du Phạm Hùng', N'11 Phạm Hùng, phường Từ Liêm, Hà Nội');
INSERT dbo.diemGiaoNhan (tenDiem, diaChi, trangThai)
SELECT s.tenDiem, s.diaChi, 1 FROM @Locations s
WHERE NOT EXISTS (SELECT 1 FROM dbo.diemGiaoNhan d WITH (UPDLOCK,HOLDLOCK) WHERE d.tenDiem=s.tenDiem);
COMMIT TRANSACTION;
SELECT idDiem,tenDiem,diaChi,trangThai FROM dbo.diemGiaoNhan
WHERE tenDiem IN (N'BMW Long Biên',N'BMW Lê Duẩn',N'BMW Lê Văn Lương',N'Mercedes-Benz Haxaco Láng Hạ',N'Mercedes-Benz An Du Phạm Hùng');
