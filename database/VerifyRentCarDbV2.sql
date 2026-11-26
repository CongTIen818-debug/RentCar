-- Regression checks only. All temporary records are rolled back; never run a payment.
USE RentCarDb_V2;
GO
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
GO
-- 10 cars per request and contract, per-car driver, one handover
BEGIN TRANSACTION;
DECLARE @r int,@h int;
INSERT dbo.yeuCauThue(idNguoiDung,ngayNhan,ngayTra,idDiemNhan,idDiemTra)
VALUES(5,'2027-01-10','2027-01-12',1,1);
SET @r=SCOPE_IDENTITY();
INSERT dbo.chiTietYeuCauThue(idYeuCau,idXe,coTaiXe,phiTaiXeNgay)
SELECT TOP(10) @r,idXe,CASE WHEN idXe=11 THEN 1 ELSE 0 END,CASE WHEN idXe=11 THEN 1000000 ELSE 0 END
FROM dbo.xe WHERE trangThai=1 ORDER BY idXe;
IF (SELECT COUNT(*) FROM dbo.vLichXe WHERE nguon=N'YeuCau' AND idYeuCau=@r)<>10
    THROW 50901,N'Request must hold all 10 cars.',1;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idYeuCau,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-MANY','2027-01-10','2027-01-12',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,@r,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue,coTaiXe,phiTaiXeNgay)
SELECT @h,c.idXe,x.giaThueNgay,x.giaThueTuan,2,c.coTaiXe,c.phiTaiXeNgay
FROM dbo.chiTietYeuCauThue c JOIN dbo.xe x ON x.idXe=c.idXe WHERE c.idYeuCau=@r;
UPDATE dbo.yeuCauThue SET trangThai=2 WHERE idYeuCau=@r;
INSERT dbo.bienBanBanGiao(idHopDong) VALUES(@h);
IF (SELECT COUNT(*) FROM dbo.chiTietHopDong WHERE idHopDong=@h)<>10
 OR (SELECT COUNT(*) FROM dbo.bienBanBanGiao WHERE idHopDong=@h)<>1
 OR (SELECT SUM(CASE WHEN coTaiXe=1 THEN 1 ELSE 0 END) FROM dbo.chiTietHopDong WHERE idHopDong=@h)<>1
    THROW 50902,N'Multi-car data does not match.',1;

ROLLBACK TRANSACTION;
PRINT N'PASS: 10 cars per request and contract, per-car driver, one handover';
GO
-- duplicate car within one contract rejected
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
VALUES(@h,11,1000000,6000000,2);

IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW 50999,N'Expected rejection did not happen: duplicate car within one contract rejected',1;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
IF NOT(ERROR_NUMBER() IN(2601,2627)) THROW;
PRINT N'PASS: duplicate car within one contract rejected';
END CATCH;
GO
-- overlapping contracts rejected
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-B','2027-02-02','2027-02-04',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
DECLARE @b int=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
VALUES(@b,11,1000000,6000000,2);

IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW 50999,N'Expected rejection did not happen: overlapping contracts rejected',1;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
IF NOT(ERROR_NUMBER()=50000 AND ERROR_MESSAGE() LIKE N'%409%') THROW;
PRINT N'PASS: overlapping contracts rejected';
END CATCH;
GO
-- adjacent schedules accepted
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-B','2027-02-03','2027-02-05',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
DECLARE @b int=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
VALUES(@b,11,1000000,6000000,2);

ROLLBACK TRANSACTION;
PRINT N'PASS: adjacent schedules accepted';
GO
-- overlapping pending requests rejected
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @r int,@b int;
INSERT dbo.yeuCauThue(idNguoiDung,ngayNhan,ngayTra,idDiemNhan,idDiemTra)
VALUES(5,'2027-03-01','2027-03-03',1,1);
SET @r=SCOPE_IDENTITY();
INSERT dbo.chiTietYeuCauThue(idYeuCau,idXe) VALUES(@r,11);
INSERT dbo.yeuCauThue(idNguoiDung,ngayNhan,ngayTra,idDiemNhan,idDiemTra)
VALUES(5,'2027-03-02','2027-03-04',1,1);
SET @b=SCOPE_IDENTITY();
INSERT dbo.chiTietYeuCauThue(idYeuCau,idXe) VALUES(@b,11);

IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW 50999,N'Expected rejection did not happen: overlapping pending requests rejected',1;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
IF NOT(ERROR_NUMBER()=50000 AND ERROR_MESSAGE() LIKE N'%409%') THROW;
PRINT N'PASS: overlapping pending requests rejected';
END CATCH;
GO
-- driver for a two-seat car rejected
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue,coTaiXe,phiTaiXeNgay)
VALUES(@h,12,1000000,6000000,2,1,1000000);

IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW 50999,N'Expected rejection did not happen: driver for a two-seat car rejected',1;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
IF NOT(ERROR_NUMBER()=50000 AND ERROR_MESSAGE() LIKE N'%2 chỗ%') THROW;
PRINT N'PASS: driver for a two-seat car rejected';
END CATCH;
GO
-- invalid contract state transition rejected
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

UPDATE dbo.hopDong SET trangThai=3 WHERE idHopDong=@h;

IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW 50999,N'Expected rejection did not happen: invalid contract state transition rejected',1;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
IF NOT(ERROR_NUMBER()=50000 AND ERROR_MESSAGE() LIKE N'%trạng thái%') THROW;
PRINT N'PASS: invalid contract state transition rejected';
END CATCH;
GO
-- valid contract state transitions
BEGIN TRANSACTION;
DECLARE @h int;
INSERT dbo.hopDong(maHopDong,ngayBatDauThue,ngayKetThucThue,trangThai,hanGiuCho,idNguoiDung,idDiemNhan,idDiemTra)
VALUES('VERIFY-V2-A','2027-02-01','2027-02-03',0,DATEADD(HOUR,24,SYSUTCDATETIME()),5,1,1);
SET @h=SCOPE_IDENTITY();
INSERT dbo.chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue)
SELECT @h,11,giaThueNgay,giaThueTuan,2 FROM dbo.xe WHERE idXe=11;

UPDATE dbo.hopDong SET trangThai=1 WHERE idHopDong=@h;
UPDATE dbo.hopDong SET trangThai=2 WHERE idHopDong=@h;
UPDATE dbo.hopDong SET trangThai=3 WHERE idHopDong=@h;

ROLLBACK TRANSACTION;
PRINT N'PASS: valid contract state transitions';
GO
