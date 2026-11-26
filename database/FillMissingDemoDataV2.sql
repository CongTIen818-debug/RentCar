-- Fill missing descriptive demo data only. Random values are NOT verified manufacturer specifications.
-- Existing nonempty values, rental/payment amounts and workflow state are preserved.
USE RentCarDb_V2;
GO
SET ANSI_NULLS ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET QUOTED_IDENTIFIER ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF; SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
UPDATE dbo.xe
SET namSanXuat=2019+CONVERT(int,ABS(CONVERT(bigint,CHECKSUM(NEWID())))%8)
WHERE namSanXuat IS NULL;
UPDATE dbo.xe
SET mauSac=COALESCE(NULLIF(LTRIM(RTRIM(mauSac)),N''),
    CASE idXe%8 WHEN 0 THEN N'Đen' WHEN 1 THEN N'Trắng' WHEN 2 THEN N'Bạc'
    WHEN 3 THEN N'Xám' WHEN 4 THEN N'Đỏ' WHEN 5 THEN N'Xanh dương'
    WHEN 6 THEN N'Vàng' ELSE N'Xanh lá' END),
    phienBan=COALESCE(NULLIF(LTRIM(RTRIM(phienBan)),N''),N'Tiêu chuẩn')
WHERE NULLIF(LTRIM(RTRIM(mauSac)),N'') IS NULL
   OR NULLIF(LTRIM(RTRIM(phienBan)),N'') IS NULL;
UPDATE dbo.xe
SET moTa=CONCAT(tenXe,N' ',namSanXuat,N', màu ',mauSac,N', ',soCho,
               N' chỗ ngồi. Hộp số ',LOWER(hopSo),N', sử dụng ',LOWER(nhienLieu),
               N'. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.')
WHERE NULLIF(LTRIM(RTRIM(moTa)),N'') IS NULL;

-- Demo birth dates for existing profiles; do not populate lock/reset-token or handover dates.
UPDATE dbo.nguoiDung
SET ngaySinh=DATEFROMPARTS(1980+CONVERT(int,ABS(CONVERT(bigint,CHECKSUM(NEWID())))%22),
                          1+CONVERT(int,ABS(CONVERT(bigint,CHECKSUM(NEWID())))%12),
                          1+CONVERT(int,ABS(CONVERT(bigint,CHECKSUM(NEWID())))%28))
WHERE ngaySinh IS NULL;
COMMIT TRANSACTION;
GO
