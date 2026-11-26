USE RentCarDb;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Bỏ các mã hiển thị tự sinh, dùng ID để liên kết dữ liệu.
IF COL_LENGTH('dbo.xe', 'maXe') IS NOT NULL
    ALTER TABLE dbo.xe DROP COLUMN maXe;

IF COL_LENGTH('dbo.nguoiDung', 'maNguoiDung') IS NOT NULL
    ALTER TABLE dbo.nguoiDung DROP COLUMN maNguoiDung;

IF COL_LENGTH('dbo.loaiXe', 'maLoaiXe') IS NOT NULL
    ALTER TABLE dbo.loaiXe DROP COLUMN maLoaiXe;

COMMIT TRANSACTION;
GO
