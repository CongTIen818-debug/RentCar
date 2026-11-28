-- Chi bo sung maDangKyNhanVien theo database/RentCarDb_BanChuan.sql.
-- Khong xoa/tao lai database, khong sua du lieu cac bang hien co.
USE [RentCarDb_V2];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.maDangKyNhanVien', N'U') IS NULL
BEGIN
CREATE TABLE dbo.maDangKyNhanVien (
    idMa INT IDENTITY(1,1) NOT NULL,
    maBam VARCHAR(64) NOT NULL,  -- SHA-256 (dạng hex) của mã đã chuẩn hóa; duy nhất
    email VARCHAR(100) NOT NULL,  -- Email nhân viên phải dùng khi đăng ký; luôn lưu chữ thường
    hoTen NVARCHAR(100) NOT NULL,  -- Họ tên gắn với mã; tài khoản được tạo sẽ lấy họ tên này
    idVaiTro INT NOT NULL,  -- Vai trò của tài khoản được tạo từ mã
    hetHan DATETIME2(0) NOT NULL,  -- Hết hạn sau 7 ngày kể từ lúc cấp
    ngayDung DATETIME2(0) NULL,  -- Thời điểm nhân viên đã dùng mã; NULL = chưa dùng
    idNguoiCap INT NOT NULL,  -- Quản trị viên đã cấp mã
    ngayCap DATETIME2(0) NOT NULL CONSTRAINT DF_maDangKyNhanVien_ngayCap DEFAULT (DATEADD(HOUR,7,SYSUTCDATETIME())),
    thuHoi BIT NOT NULL CONSTRAINT DF_maDangKyNhanVien_thuHoi DEFAULT (0),  -- 1 = đã thu hồi, hoặc tự thu hồi khi cấp mã mới cho mã đã hết hạn
    CONSTRAINT PK_maDangKyNhanVien PRIMARY KEY CLUSTERED (idMa),
    CONSTRAINT UQ_maDangKyNhanVien_maBam UNIQUE (maBam),
    CONSTRAINT FK_maDangKyNhanVien_vaiTro FOREIGN KEY (idVaiTro) REFERENCES dbo.vaiTro (idVaiTro),
    CONSTRAINT FK_maDangKyNhanVien_nguoiCap FOREIGN KEY (idNguoiCap) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT CK_maDangKyNhanVien_email CHECK (LEN(LTRIM(RTRIM(email))) > 0 AND CHARINDEX('@', email) > 1),
    CONSTRAINT CK_maDangKyNhanVien_hetHan CHECK (hetHan > ngayCap),
    CONSTRAINT CK_maDangKyNhanVien_ngayDung CHECK (ngayDung IS NULL OR ngayDung >= ngayCap)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_maDangKyNhanVien_email_chuaDung ON dbo.maDangKyNhanVien (email) WHERE ngayDung IS NULL AND thuHoi = 0;
CREATE NONCLUSTERED INDEX IX_maDangKyNhanVien_idVaiTro ON dbo.maDangKyNhanVien (idVaiTro);
CREATE NONCLUSTERED INDEX IX_maDangKyNhanVien_idNguoiCap ON dbo.maDangKyNhanVien (idNguoiCap);
END;
COMMIT TRANSACTION;
GO
