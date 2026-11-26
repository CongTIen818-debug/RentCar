-- Bo sung du lieu hop dong nhieu xe, khong xoa/tao lai database.
USE [RentCarDb_V2];
GO
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF; SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.hopDong',N'phiTaiXe') IS NULL
    ALTER TABLE dbo.hopDong ADD phiTaiXe DECIMAL(18,2) NOT NULL CONSTRAINT DF_hopDong_phiTaiXe DEFAULT(0) WITH VALUES;
IF COL_LENGTH(N'dbo.hopDong',N'thueSuatVat') IS NULL
    ALTER TABLE dbo.hopDong ADD thueSuatVat DECIMAL(5,4) NOT NULL CONSTRAINT DF_hopDong_thueSuatVat DEFAULT(0) WITH VALUES;
IF EXISTS(SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID(N'dbo.hopDong') AND name=N'tongTienThanhToan' AND definition NOT LIKE N'%phiTaiXe%')
BEGIN
    ALTER TABLE dbo.hopDong DROP COLUMN tongTienThanhToan;
    EXEC(N'ALTER TABLE dbo.hopDong ADD tongTienThanhToan AS CONVERT(DECIMAL(18,2), ROUND((tongTienGoc-tienGiamGia+phiTaiXe)*(1+thueSuatVat),0)) PERSISTED NOT NULL;');
END;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_hopDong_phiTaiXeVat')
    EXEC(N'ALTER TABLE dbo.hopDong ADD CONSTRAINT CK_hopDong_phiTaiXeVat CHECK(phiTaiXe>=0 AND thueSuatVat BETWEEN 0 AND 1);');
ALTER TABLE dbo.hopDong DROP CONSTRAINT CK_hopDong_uuDai;
ALTER TABLE dbo.hopDong ADD CONSTRAINT CK_hopDong_uuDai CHECK
((idUuDai IS NULL AND tenUuDaiApDung IS NULL AND phanTramApDung IS NULL AND tienGiamGia=0)
 OR (tenUuDaiApDung IS NOT NULL AND phanTramApDung BETWEEN 0.01 AND 100));
IF COL_LENGTH(N'dbo.bienBanBanGiao',N'chiTietGiaoXe') IS NULL
    ALTER TABLE dbo.bienBanBanGiao ADD chiTietGiaoXe NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.bienBanBanGiao',N'chiTietNhanXe') IS NULL
    ALTER TABLE dbo.bienBanBanGiao ADD chiTietNhanXe NVARCHAR(MAX) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_bienBan_chiTietXeJson')
    EXEC(N'ALTER TABLE dbo.bienBanBanGiao ADD CONSTRAINT CK_bienBan_chiTietXeJson CHECK((chiTietGiaoXe IS NULL OR ISJSON(chiTietGiaoXe)=1) AND (chiTietNhanXe IS NULL OR ISJSON(chiTietNhanXe)=1));');
IF OBJECT_ID(N'dbo.DF_maDangKyNhanVien_ngayCap',N'D') IS NOT NULL
BEGIN
    ALTER TABLE dbo.maDangKyNhanVien DROP CONSTRAINT DF_maDangKyNhanVien_ngayCap;
    ALTER TABLE dbo.maDangKyNhanVien ADD CONSTRAINT DF_maDangKyNhanVien_ngayCap DEFAULT(SYSUTCDATETIME()) FOR ngayCap;
END;
COMMIT TRANSACTION;
GO
