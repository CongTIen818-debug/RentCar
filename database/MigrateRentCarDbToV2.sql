-- Copy existing RentCarDb data into the EMPTY RentCarDb_V2.
-- Preserves IDs, passwords, images, original money and UTC schedules.
-- Old states: contract 0/1/2/3 -> 1/2/3/4; vehicle 0 -> 4.
USE RentCarDb_V2;
GO
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
IF EXISTS(SELECT 1 FROM dbo.nguoiDung) OR EXISTS(SELECT 1 FROM dbo.xe)
    THROW 50002,N'Database V2 phải trống trước khi chuyển dữ liệu.',1;
IF EXISTS(SELECT 1 FROM RentCarDb.dbo.thanhToan)
    THROW 50003,N'Có giao dịch cũ: cần đối chiếu người thực hiện và loại giao dịch trước khi chuyển.',1;
IF EXISTS(SELECT 1 FROM RentCarDb.dbo.bienBanBanGiao b
          JOIN RentCarDb.dbo.chiTietHopDong c ON c.idChiTietHopDong=b.idChiTietHopDong
          GROUP BY c.idHopDong HAVING COUNT(*)>1)
    THROW 50004,N'Hợp đồng cũ có nhiều biên bản: cần tổng hợp trước khi chuyển.',1;
GO
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @disable nvarchar(max)=N'';
SELECT @disable+=N'DISABLE TRIGGER ALL ON '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N';'
FROM sys.tables;
EXEC sp_executesql @disable;

SET IDENTITY_INSERT dbo.vaiTro ON;
INSERT dbo.vaiTro(idVaiTro,tenVaiTro,maVaiTro) SELECT idVaiTro,tenVaiTro,maVaiTro FROM RentCarDb.dbo.vaiTro;
SET IDENTITY_INSERT dbo.vaiTro OFF;

SET IDENTITY_INSERT dbo.nguoiDung ON;
INSERT dbo.nguoiDung(idNguoiDung,hoTen,email,matKhau,soDienThoai,cccd,diaChi,trangThai,idVaiTro)
SELECT idNguoiDung,hoTen,email,matKhau,soDienThoai,cccd,diaChi,trangThai,idVaiTro FROM RentCarDb.dbo.nguoiDung;
SET IDENTITY_INSERT dbo.nguoiDung OFF;

SET IDENTITY_INSERT dbo.loaiXe ON;
INSERT dbo.loaiXe(idLoaiXe,tenLoai,moTa) SELECT idLoaiXe,tenLoai,moTa FROM RentCarDb.dbo.loaiXe;
SET IDENTITY_INSERT dbo.loaiXe OFF;

-- Seat counts follow the member's supplied fleet data. Unknown years remain NULL.
DECLARE @spec TABLE(idXe int PRIMARY KEY,soCho tinyint,tienDatCoc decimal(18,2));
INSERT @spec VALUES (1,5,3000000),(3,5,3000000),(4,5,5000000),(6,4,5000000),(10,2,3000000),
(11,4,5000000),(12,2,5000000),(13,2,5000000),(14,4,5000000),(15,5,5000000),(16,4,5000000),
(17,7,5000000),(18,5,5000000),(19,2,5000000),(20,5,5000000),(21,5,5000000),(22,2,5000000),
(23,5,5000000),(24,5,3000000);
IF EXISTS(SELECT 1 FROM RentCarDb.dbo.xe x LEFT JOIN @spec s ON s.idXe=x.idXe WHERE s.idXe IS NULL)
    THROW 50005,N'Có xe chưa có số ghế. Cần bổ sung bảng ánh xạ thông số trước khi chuyển.',1;
SET IDENTITY_INSERT dbo.xe ON;
INSERT dbo.xe(idXe,tenXe,bienSo,hangXe,phienBan,namSanXuat,mauSac,soCho,hopSo,nhienLieu,
              giaThueNgay,giaThueTuan,tienDatCoc,trangThai,hinhAnhDaiDien,idLoaiXe)
SELECT x.idXe,x.tenXe,x.bienSo,x.hangXe,x.phienBan,x.namSanXuat,x.mauSac,s.soCho,N'Tự động',N'Xăng',
       x.giaThueNgay,x.giaThueNgay*6,s.tienDatCoc,CASE WHEN x.trangThai=0 THEN 4 ELSE 1 END,x.hinhAnhDaiDien,x.idLoaiXe
FROM RentCarDb.dbo.xe x JOIN @spec s ON s.idXe=x.idXe;
SET IDENTITY_INSERT dbo.xe OFF;

SET IDENTITY_INSERT dbo.hinhAnhXe ON;
INSERT dbo.hinhAnhXe(idHinhAnhXe,duongDanAnh,idXe) SELECT idHinhAnhXe,duongDanAnh,idXe FROM RentCarDb.dbo.hinhAnhXe;
SET IDENTITY_INSERT dbo.hinhAnhXe OFF;

-- Percentage-only promotions in V2. Current fixed-amount discounts correspond exactly to 10%.
-- Reject ambiguous conversions rather than silently changing prices.
IF EXISTS(
 SELECT k.idKhuyenMai FROM RentCarDb.dbo.khuyenMai k
 JOIN RentCarDb.dbo.khuyenMaiXe kx ON kx.idKhuyenMai=k.idKhuyenMai
 JOIN RentCarDb.dbo.xe x ON x.idXe=kx.idXe
 WHERE k.kieuGiam=2
 GROUP BY k.idKhuyenMai
 HAVING MIN(CASE WHEN x.giaThueNgay=0 THEN 100 ELSE k.giaTriGiam*100/NULLIF(x.giaThueNgay,0) END)
       <>MAX(CASE WHEN x.giaThueNgay=0 THEN 100 ELSE k.giaTriGiam*100/NULLIF(x.giaThueNgay,0) END))
    THROW 50006,N'Khuyến mãi số tiền cố định cần chuyển riêng theo từng xe.',1;
SET IDENTITY_INSERT dbo.uuDai ON;
INSERT dbo.uuDai(idUuDai,maUuDai,tenUuDai,moTa,phanTramGiam,ngayBatDau,ngayKetThuc,trangThai)
SELECT k.idKhuyenMai,CONCAT('UD-CU-',k.idKhuyenMai),k.tenKhuyenMai,k.moTa,
       CASE WHEN k.kieuGiam=1 THEN k.giaTriGiam
            ELSE (SELECT TOP(1) CASE WHEN x.giaThueNgay=0 OR k.giaTriGiam>=x.giaThueNgay THEN 100
                                    ELSE k.giaTriGiam*100/x.giaThueNgay END
                  FROM RentCarDb.dbo.khuyenMaiXe kx JOIN RentCarDb.dbo.xe x ON x.idXe=kx.idXe
                  WHERE kx.idKhuyenMai=k.idKhuyenMai ORDER BY x.idXe) END,
       k.ngayBatDau,k.ngayKetThuc,k.trangThai
FROM RentCarDb.dbo.khuyenMai k;
SET IDENTITY_INSERT dbo.uuDai OFF;
INSERT dbo.uuDaiXe(idUuDai,idXe) SELECT idKhuyenMai,idXe FROM RentCarDb.dbo.khuyenMaiXe;

-- Historical contracts had no pickup/return location. Use an explicit unknown-location placeholder.
INSERT dbo.diemGiaoNhan(tenDiem,diaChi) VALUES(N'Chưa xác định - hợp đồng cũ',N'Chưa có thông tin địa điểm trong dữ liệu cũ');
DECLARE @point int=SCOPE_IDENTITY();

SET IDENTITY_INSERT dbo.hopDong ON;
INSERT dbo.hopDong(idHopDong,maHopDong,ngayTao,ngayBatDauThue,ngayKetThucThue,tongTienGoc,
                  idUuDai,tenUuDaiApDung,phanTramApDung,tienGiamGia,tienDatCoc,trangThai,idNguoiDung,idDiemNhan,idDiemTra)
SELECT h.idHopDong,h.maHopDong,h.ngayTao,h.ngayBatDauThue,h.ngayKetThucThue,h.tongTienGoc,
       p.idUuDai,CASE WHEN p.idUuDai IS NOT NULL THEN p.tenUuDai END,
       CASE WHEN p.idUuDai IS NOT NULL THEN p.phanTramGiam END,
       h.tienGiamGia,h.tienDatCoc,h.trangThai+1,h.idNguoiDung,@point,@point
FROM RentCarDb.dbo.hopDong h
OUTER APPLY(SELECT TOP(1) u.idUuDai,u.tenUuDai,u.phanTramGiam
            FROM RentCarDb.dbo.chiTietHopDong c JOIN dbo.uuDai u ON u.idUuDai=c.idKhuyenMai
            WHERE c.idHopDong=h.idHopDong ORDER BY c.idChiTietHopDong) p;
SET IDENTITY_INSERT dbo.hopDong OFF;

SET IDENTITY_INSERT dbo.chiTietHopDong ON;
INSERT dbo.chiTietHopDong(idChiTietHopDong,idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue,soTuanThue,coTaiXe,phiTaiXeNgay)
SELECT idChiTietHopDong,idHopDong,idXe,giaThueTaiThoiDiem,giaThueTaiThoiDiem*6,soNgayThue,0,0,0
FROM RentCarDb.dbo.chiTietHopDong;
SET IDENTITY_INSERT dbo.chiTietHopDong OFF;

SET IDENTITY_INSERT dbo.bienBanBanGiao ON;
INSERT dbo.bienBanBanGiao(idBienBanBanGiao,idHopDong,ngayBanGiao,tinhTrangLucGiao,ngayNhanLai,tinhTrangLucNhan,chiPhiPhatSinh)
SELECT b.idBienBanBanGiao,c.idHopDong,b.ngayBanGiao,b.tinhTrangLucGiao,b.ngayNhanLai,b.tinhTrangLucNhan,b.chiPhiPhatSinh
FROM RentCarDb.dbo.bienBanBanGiao b JOIN RentCarDb.dbo.chiTietHopDong c ON c.idChiTietHopDong=b.idChiTietHopDong;
SET IDENTITY_INSERT dbo.bienBanBanGiao OFF;

SET IDENTITY_INSERT dbo.danhGia ON;
INSERT dbo.danhGia(idDanhGia,soSao,noiDung,ngayDanhGia,idHopDong,idXe)
SELECT idDanhGia,soSao,noiDung,ngayDanhGia,idHopDong,idXe FROM RentCarDb.dbo.danhGia;
SET IDENTITY_INSERT dbo.danhGia OFF;

IF (SELECT COUNT(*) FROM dbo.nguoiDung)<>(SELECT COUNT(*) FROM RentCarDb.dbo.nguoiDung)
 OR (SELECT COUNT(*) FROM dbo.xe)<>(SELECT COUNT(*) FROM RentCarDb.dbo.xe)
 OR (SELECT COUNT(*) FROM dbo.hopDong)<>(SELECT COUNT(*) FROM RentCarDb.dbo.hopDong)
    THROW 50007,N'Số lượng dữ liệu chuyển không khớp.',1;
IF EXISTS(SELECT 1 FROM dbo.hopDong n JOIN RentCarDb.dbo.hopDong o ON o.idHopDong=n.idHopDong
          WHERE n.tongTienThanhToan<>o.tongTienThanhToan OR n.tienDatCoc<>o.tienDatCoc)
    THROW 50008,N'Số tiền hợp đồng cũ bị thay đổi. Dừng chuyển dữ liệu.',1;
IF EXISTS(SELECT 1 FROM dbo.nguoiDung n JOIN RentCarDb.dbo.nguoiDung o ON o.idNguoiDung=n.idNguoiDung
          WHERE n.matKhau<>o.matKhau OR n.email<>o.email)
    THROW 50009,N'Tài khoản hoặc mật khẩu không khớp.',1;

DECLARE @enable nvarchar(max)=N'';
SELECT @enable+=N'ENABLE TRIGGER ALL ON '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N';'
FROM sys.tables;
EXEC sp_executesql @enable;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
