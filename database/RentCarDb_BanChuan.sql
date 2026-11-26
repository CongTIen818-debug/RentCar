USE master;
GO
IF DB_ID(N'RentCarDb') IS NOT NULL
BEGIN
    ALTER DATABASE [RentCarDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [RentCarDb];
END
GO
CREATE DATABASE [RentCarDb];
-- Tùy chọn: nếu muốn sắp xếp/so sánh chuỗi theo tiếng Việt, thay lệnh trên bằng:
-- CREATE DATABASE [RentCarDb] COLLATE Vietnamese_CI_AS;
GO
ALTER DATABASE [RentCarDb] SET ARITHABORT ON;
GO
USE [RentCarDb];
GO
/* Các tùy chọn phiên bắt buộc cho chỉ mục có lọc (UX_hopDong_idYeuCau) và cột tính toán PERSISTED. */
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* ===================== PHẦN 1: TẠO BẢNG (23 bảng) ===================== */

/* ---------- Bảng dbo.vaiTro: Vai trò người dùng: ADMIN, KINHDOANH, GIAONHAN, KETOAN, KHACHHANG ---------- */
CREATE TABLE dbo.vaiTro (
    idVaiTro INT IDENTITY(1,1) NOT NULL,
    tenVaiTro NVARCHAR(50) NOT NULL,
    maVaiTro VARCHAR(20) NOT NULL,
    CONSTRAINT PK_vaiTro PRIMARY KEY CLUSTERED (idVaiTro),
    CONSTRAINT UQ_vaiTro_maVaiTro UNIQUE (maVaiTro),
    CONSTRAINT CK_vaiTro_tenVaiTro CHECK (LEN(LTRIM(RTRIM(tenVaiTro))) > 0)
);
GO

/* ---------- Bảng dbo.nguoiDung: Tài khoản và hồ sơ: khách hàng, nhân viên, quản trị viên (Customer và AppUser trong SRS) ---------- */
CREATE TABLE dbo.nguoiDung (
    idNguoiDung INT IDENTITY(1,1) NOT NULL,
    hoTen NVARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,  -- Duy nhất (UC-05 A2)
    matKhau VARCHAR(512) NOT NULL,  -- Chuỗi băm PBKDF2 (ASP.NET Core Identity), không lưu mật khẩu gốc
    ngaySinh DATE NULL,  -- Khách hàng phải đủ 21 tuổi (BR-12)
    soDienThoai VARCHAR(20) NULL,
    cccd VARCHAR(20) NULL,  -- Số giấy tờ tùy thân giả lập; giao diện chỉ hiện 4 số cuối (NFR-13)
    diaChi NVARCHAR(255) NULL,
    trangThai BIT NOT NULL CONSTRAINT DF_nguoiDung_trangThai DEFAULT (1),  -- 1 = đang hoạt động; 0 = bị quản trị viên khóa (FR-6.4)
    soLanSai INT NOT NULL CONSTRAINT DF_nguoiDung_soLanSai DEFAULT (0),  -- Số lần đăng nhập sai liên tiếp; đạt 5 thì khóa tạm
    khoaDen DATETIME2(0) NULL,  -- Khóa tạm đến thời điểm này; NULL = không khóa tạm
    maDatLaiMatKhau VARCHAR(512) NULL,  -- Mã đặt lại mật khẩu đã băm (UC-07)
    hanMaDatLai DATETIME2(0) NULL,  -- Mã đặt lại hết hạn sau 30 phút, dùng một lần
    idVaiTro INT NOT NULL,
    CONSTRAINT PK_nguoiDung PRIMARY KEY CLUSTERED (idNguoiDung),
    CONSTRAINT UQ_nguoiDung_email UNIQUE (email),
    CONSTRAINT FK_nguoiDung_vaiTro FOREIGN KEY (idVaiTro) REFERENCES dbo.vaiTro (idVaiTro),
    CONSTRAINT CK_nguoiDung_email CHECK (LEN(LTRIM(RTRIM(email))) > 0 AND CHARINDEX('@', email) > 1),
    CONSTRAINT CK_nguoiDung_soLanSai CHECK (soLanSai >= 0)
);
GO

/* ---------- Bảng dbo.giayPhepLaiXe: Giấy phép lái xe của khách hàng; quan hệ 1-1 với nguoiDung (FR-6.5, BR-03c) ---------- */
CREATE TABLE dbo.giayPhepLaiXe (
    idGPLX INT IDENTITY(1,1) NOT NULL,
    idNguoiDung INT NOT NULL,  -- Mỗi khách một GPLX (UNIQUE)
    soGPLX VARCHAR(30) NOT NULL,  -- Số GPLX giả lập, duy nhất
    hang VARCHAR(10) NOT NULL,  -- Hạng GPLX, ví dụ B, C
    ngayHetHan DATE NOT NULL,
    CONSTRAINT PK_giayPhepLaiXe PRIMARY KEY CLUSTERED (idGPLX),
    CONSTRAINT UQ_giayPhepLaiXe_idNguoiDung UNIQUE (idNguoiDung),
    CONSTRAINT UQ_giayPhepLaiXe_soGPLX UNIQUE (soGPLX),
    CONSTRAINT FK_giayPhepLaiXe_nguoiDung FOREIGN KEY (idNguoiDung) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT CK_giayPhepLaiXe_hang CHECK (LEN(LTRIM(RTRIM(hang))) > 0)
);
GO

/* ---------- Bảng dbo.loaiXe: Loại xe (VehicleType); hạng GPLX tối thiểu theo bảng ánh xạ mục 3.7 ---------- */
CREATE TABLE dbo.loaiXe (
    idLoaiXe INT IDENTITY(1,1) NOT NULL,
    tenLoai NVARCHAR(50) NOT NULL,
    moTa NVARCHAR(MAX) NULL,
    hangGPLXToiThieu VARCHAR(5) NOT NULL CONSTRAINT DF_loaiXe_hangGPLXToiThieu DEFAULT ('B'),  -- Ánh xạ BR-03(c): xe con = B, xe tải = C
    CONSTRAINT PK_loaiXe PRIMARY KEY CLUSTERED (idLoaiXe),
    CONSTRAINT UQ_loaiXe_tenLoai UNIQUE (tenLoai),
    CONSTRAINT CK_loaiXe_tenLoai CHECK (LEN(LTRIM(RTRIM(tenLoai))) > 0)
);
GO

/* ---------- Bảng dbo.xe: Xe cho thuê (Vehicle); trạng thái vận hành theo mục 3.8, không chứa lịch đặt ---------- */
CREATE TABLE dbo.xe (
    idXe INT IDENTITY(1,1) NOT NULL,
    tenXe NVARCHAR(100) NOT NULL,
    bienSo VARCHAR(20) NOT NULL,  -- Biển số duy nhất (FR-1.1a)
    hangXe NVARCHAR(50) NOT NULL,
    phienBan NVARCHAR(100) NULL,  -- Dòng xe / phiên bản
    namSanXuat INT NOT NULL,
    mauSac NVARCHAR(30) NULL,
    soCho TINYINT NOT NULL,  -- Số chỗ ngồi (FR-1.1a)
    hopSo NVARCHAR(20) NOT NULL,  -- Hộp số: Tự động, Số sàn
    nhienLieu NVARCHAR(20) NOT NULL,  -- Nhiên liệu: Xăng, Dầu, Điện, Hybrid
    giaThueNgay DECIMAL(18,2) NOT NULL,  -- DailyRate (VND/ngày)
    giaThueTuan DECIMAL(18,2) NOT NULL,  -- WeeklyRate: giá cho 7 ngày liên tiếp (BR-10b)
    tienDatCoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_xe_tienDatCoc DEFAULT (0),  -- DepositAmount; được sao chép sang hợp đồng (BR-14)
    trangThai INT NOT NULL CONSTRAINT DF_xe_trangThai DEFAULT (1),  -- 1 Sẵn sàng; 2 Chờ kiểm tra; 3 Bảo trì; 4 Ngừng hoạt động
    moTa NVARCHAR(MAX) NULL,
    hinhAnhDaiDien VARCHAR(255) NULL,
    idLoaiXe INT NOT NULL,
    CONSTRAINT PK_xe PRIMARY KEY CLUSTERED (idXe),
    CONSTRAINT UQ_xe_bienSo UNIQUE (bienSo),
    CONSTRAINT FK_xe_loaiXe FOREIGN KEY (idLoaiXe) REFERENCES dbo.loaiXe (idLoaiXe),
    CONSTRAINT CK_xe_bienSo CHECK (LEN(LTRIM(RTRIM(bienSo))) > 0),
    CONSTRAINT CK_xe_namSanXuat CHECK (namSanXuat BETWEEN 1886 AND 2100),
    CONSTRAINT CK_xe_soCho CHECK (soCho > 0),
    CONSTRAINT CK_xe_giaThueNgay CHECK (giaThueNgay >= 0),
    CONSTRAINT CK_xe_giaThueTuan CHECK (giaThueTuan >= 0),
    CONSTRAINT CK_xe_tienDatCoc CHECK (tienDatCoc >= 0),
    CONSTRAINT CK_xe_trangThai CHECK (trangThai IN (1, 2, 3, 4)),
    CONSTRAINT CK_xe_hopSo CHECK (hopSo IN (N'Tự động', N'Số sàn')),
    CONSTRAINT CK_xe_nhienLieu CHECK (nhienLieu IN (N'Xăng', N'Dầu', N'Điện', N'Hybrid'))
);
GO

/* ---------- Bảng dbo.hinhAnhXe: Ảnh của xe; tối đa 10 ảnh mỗi xe, kiểm tra ở tầng ứng dụng (FR-1.2a) ---------- */
CREATE TABLE dbo.hinhAnhXe (
    idHinhAnhXe INT IDENTITY(1,1) NOT NULL,
    duongDanAnh VARCHAR(255) NOT NULL,
    idXe INT NOT NULL,
    CONSTRAINT PK_hinhAnhXe PRIMARY KEY CLUSTERED (idHinhAnhXe),
    CONSTRAINT UQ_hinhAnhXe_idXe_duongDan UNIQUE (idXe, duongDanAnh),
    CONSTRAINT FK_hinhAnhXe_xe FOREIGN KEY (idXe) REFERENCES dbo.xe (idXe)
);
GO

/* ---------- Bảng dbo.diemGiaoNhan: Điểm nhận và trả xe (PickupLocation, FR-3.1) ---------- */
CREATE TABLE dbo.diemGiaoNhan (
    idDiem INT IDENTITY(1,1) NOT NULL,
    tenDiem NVARCHAR(150) NOT NULL,
    diaChi NVARCHAR(255) NOT NULL,
    trangThai BIT NOT NULL CONSTRAINT DF_diemGiaoNhan_trangThai DEFAULT (1),
    CONSTRAINT PK_diemGiaoNhan PRIMARY KEY CLUSTERED (idDiem),
    CONSTRAINT UQ_diemGiaoNhan_tenDiem UNIQUE (tenDiem)
);
GO

/* ---------- Bảng dbo.yeuCauThue: Yêu cầu thuê xe (RentalRequest); giữ chỗ 24 giờ (BR-15) ---------- */
CREATE TABLE dbo.yeuCauThue (
    idYeuCau INT IDENTITY(1,1) NOT NULL,
    idNguoiDung INT NOT NULL,  -- Khách hàng của yêu cầu
    idNguoiTao INT NULL,  -- Nhân viên kinh doanh tạo hộ; NULL nếu khách tự gửi
    idXe INT NOT NULL,
    ngayNhan DATETIME2(0) NOT NULL,  -- StartAt
    ngayTra DATETIME2(0) NOT NULL,  -- EndAt
    idDiemNhan INT NOT NULL,
    idDiemTra INT NOT NULL,
    trangThai INT NOT NULL CONSTRAINT DF_yeuCauThue_trangThai DEFAULT (1),  -- 1 Chờ xử lý; 2 Đã chuyển hợp đồng; 3 Đã hủy
    ngayTao DATETIME2(0) NOT NULL CONSTRAINT DF_yeuCauThue_ngayTao DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    hanGiuCho DATETIME2(0) NOT NULL CONSTRAINT DF_yeuCauThue_hanGiuCho DEFAULT (DATEADD(HOUR, 24, DATEADD(HOUR, 7, SYSUTCDATETIME()))),  -- Ngày tạo + 24 giờ (BR-15)
    CONSTRAINT PK_yeuCauThue PRIMARY KEY CLUSTERED (idYeuCau),
    CONSTRAINT FK_yeuCauThue_khach FOREIGN KEY (idNguoiDung) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT FK_yeuCauThue_nguoiTao FOREIGN KEY (idNguoiTao) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT FK_yeuCauThue_xe FOREIGN KEY (idXe) REFERENCES dbo.xe (idXe),
    CONSTRAINT FK_yeuCauThue_diemNhan FOREIGN KEY (idDiemNhan) REFERENCES dbo.diemGiaoNhan (idDiem),
    CONSTRAINT FK_yeuCauThue_diemTra FOREIGN KEY (idDiemTra) REFERENCES dbo.diemGiaoNhan (idDiem),
    CONSTRAINT CK_yeuCauThue_thoiGian CHECK (ngayTra > ngayNhan AND DATEDIFF(SECOND, ngayNhan, ngayTra) >= 14400 AND ngayTra <= DATEADD(DAY, 30, ngayNhan)),
    CONSTRAINT CK_yeuCauThue_trangThai CHECK (trangThai IN (1, 2, 3)),
    CONSTRAINT CK_yeuCauThue_hanGiuCho CHECK (hanGiuCho > ngayTao)
);
GO

/* ---------- Bảng dbo.bangGia: Bảng giá theo loại xe và khoảng thời gian: hệ số nhân giá ngày và giá tuần (PriceRule, BR-10a) ---------- */
CREATE TABLE dbo.bangGia (
    idBangGia INT IDENTITY(1,1) NOT NULL,
    idLoaiXe INT NOT NULL,
    ngayBatDau DATETIME2(0) NOT NULL,
    ngayKetThuc DATETIME2(0) NOT NULL,
    heSo DECIMAL(5,2) NOT NULL CONSTRAINT DF_bangGia_heSo DEFAULT (1),
    trangThai BIT NOT NULL CONSTRAINT DF_bangGia_trangThai DEFAULT (1),
    CONSTRAINT PK_bangGia PRIMARY KEY CLUSTERED (idBangGia),
    CONSTRAINT FK_bangGia_loaiXe FOREIGN KEY (idLoaiXe) REFERENCES dbo.loaiXe (idLoaiXe),
    CONSTRAINT CK_bangGia_thoiGian CHECK (ngayKetThuc > ngayBatDau),
    CONSTRAINT CK_bangGia_heSo CHECK (heSo > 0)
);
GO

/* ---------- Bảng dbo.uuDai: Ưu đãi giảm theo phần trăm (Promotion, FR-7.2, BR-10c) ---------- */
CREATE TABLE dbo.uuDai (
    idUuDai INT IDENTITY(1,1) NOT NULL,
    maUuDai VARCHAR(30) NOT NULL,
    tenUuDai NVARCHAR(150) NOT NULL,
    moTa NVARCHAR(MAX) NULL,
    phanTramGiam DECIMAL(5,2) NOT NULL,
    ngayBatDau DATETIME2(0) NOT NULL,
    ngayKetThuc DATETIME2(0) NOT NULL,
    trangThai BIT NOT NULL CONSTRAINT DF_uuDai_trangThai DEFAULT (1),
    CONSTRAINT PK_uuDai PRIMARY KEY CLUSTERED (idUuDai),
    CONSTRAINT UQ_uuDai_maUuDai UNIQUE (maUuDai),
    CONSTRAINT CK_uuDai_phanTramGiam CHECK (phanTramGiam > 0 AND phanTramGiam <= 100),
    CONSTRAINT CK_uuDai_thoiGian CHECK (ngayKetThuc > ngayBatDau)
);
GO

/* ---------- Bảng dbo.uuDaiXe: Ưu đãi áp dụng cho từng xe (bảng nối; bản CSDL cũ gọi là khuyenMaiXe) ---------- */
CREATE TABLE dbo.uuDaiXe (
    idUuDai INT NOT NULL,
    idXe INT NOT NULL,
    CONSTRAINT PK_uuDaiXe PRIMARY KEY CLUSTERED (idUuDai, idXe),
    CONSTRAINT FK_uuDaiXe_uuDai FOREIGN KEY (idUuDai) REFERENCES dbo.uuDai (idUuDai),
    CONSTRAINT FK_uuDaiXe_xe FOREIGN KEY (idXe) REFERENCES dbo.xe (idXe)
);
GO

/* ---------- Bảng dbo.chinhSachPhuPhi: Cấu hình phụ phí theo loại xe (SurchargePolicy, SP-01 và SP-02) ---------- */
CREATE TABLE dbo.chinhSachPhuPhi (
    idChinhSach INT IDENTITY(1,1) NOT NULL,
    idLoaiXe INT NOT NULL,
    anHanPhut INT NOT NULL CONSTRAINT DF_chinhSachPhuPhi_anHanPhut DEFAULT (120),  -- Ân hạn trả muộn (phút)
    hanMucKmNgay INT NOT NULL CONSTRAINT DF_chinhSachPhuPhi_hanMucKmNgay DEFAULT (200),  -- Hạn mức km mỗi ngày thuê
    donGiaKm DECIMAL(18,2) NOT NULL CONSTRAINT DF_chinhSachPhuPhi_donGiaKm DEFAULT (5000),  -- Đơn giá km vượt hạn mức (VND/km)
    CONSTRAINT PK_chinhSachPhuPhi PRIMARY KEY CLUSTERED (idChinhSach),
    CONSTRAINT UQ_chinhSachPhuPhi_idLoaiXe UNIQUE (idLoaiXe),
    CONSTRAINT FK_chinhSachPhuPhi_loaiXe FOREIGN KEY (idLoaiXe) REFERENCES dbo.loaiXe (idLoaiXe),
    CONSTRAINT CK_chinhSachPhuPhi_anHanPhut CHECK (anHanPhut >= 0),
    CONSTRAINT CK_chinhSachPhuPhi_hanMucKmNgay CHECK (hanMucKmNgay >= 0),
    CONSTRAINT CK_chinhSachPhuPhi_donGiaKm CHECK (donGiaKm >= 0)
);
GO

/* ---------- Bảng dbo.lichBaoTri: Lịch bảo dưỡng và kiểm định (mức Could, FR-1.4, BR-11) ---------- */
CREATE TABLE dbo.lichBaoTri (
    idLichBaoTri INT IDENTITY(1,1) NOT NULL,
    idXe INT NOT NULL,
    loai NVARCHAR(50) NOT NULL,
    moTa NVARCHAR(255) NULL,
    tgBatDau DATETIME2(0) NOT NULL,
    tgKetThuc DATETIME2(0) NOT NULL,
    chiPhi DECIMAL(18,2) NOT NULL CONSTRAINT DF_lichBaoTri_chiPhi DEFAULT (0),
    CONSTRAINT PK_lichBaoTri PRIMARY KEY CLUSTERED (idLichBaoTri),
    CONSTRAINT FK_lichBaoTri_xe FOREIGN KEY (idXe) REFERENCES dbo.xe (idXe),
    CONSTRAINT CK_lichBaoTri_thoiGian CHECK (tgKetThuc > tgBatDau),
    CONSTRAINT CK_lichBaoTri_chiPhi CHECK (chiPhi >= 0)
);
GO

/* ---------- Bảng dbo.hopDong: Hợp đồng thuê xe (RentalContract); mỗi hợp đồng có đúng một xe qua chiTietHopDong ---------- */
CREATE TABLE dbo.hopDong (
    idHopDong INT IDENTITY(1,1) NOT NULL,
    maHopDong VARCHAR(50) NOT NULL,  -- Mã hợp đồng duy nhất (ContractCode)
    ngayTao DATETIME2(0) NOT NULL CONSTRAINT DF_hopDong_ngayTao DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    ngayBatDauThue DATETIME2(0) NOT NULL,  -- StartAt
    ngayKetThucThue DATETIME2(0) NOT NULL,  -- EndAt
    tongTienGoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_hopDong_tongTienGoc DEFAULT (0),  -- Tổng tiền thuê trước ưu đãi = chiTietHopDong.thanhTienGoc
    idUuDai INT NULL,  -- Ưu đãi áp dụng; tối đa một ưu đãi (BR-10c)
    tenUuDaiApDung NVARCHAR(150) NULL,  -- Lưu ảnh tên ưu đãi tại thời điểm lập
    phanTramApDung DECIMAL(5,2) NULL,  -- Lưu ảnh phần trăm giảm tại thời điểm lập
    tienGiamGia DECIMAL(18,2) NOT NULL CONSTRAINT DF_hopDong_tienGiamGia DEFAULT (0),  -- = ROUND(tongTienGoc * phanTramApDung / 100, 0) (BR-10e)
    tongTienThanhToan AS (tongTienGoc - tienGiamGia) PERSISTED NOT NULL,  -- Tính toán: tiền thuê sau ưu đãi, chưa gồm phụ phí
    tienDatCoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_hopDong_tienDatCoc DEFAULT (0),  -- Sao chép từ xe.tienDatCoc lúc lập hợp đồng (BR-14)
    trangThai INT NOT NULL CONSTRAINT DF_hopDong_trangThai DEFAULT (0),  -- 0 Chờ xác nhận; 1 Đã xác nhận; 2 Đang thuê; 3 Hoàn thành; 4 Đã hủy
    lyDoHuy NVARCHAR(255) NULL,  -- Lý do hủy (UC-02 A8)
    hanGiuCho DATETIME2(0) NULL,  -- Hết hạn giữ chỗ khi Chờ xác nhận (BR-15)
    idNguoiDung INT NOT NULL,  -- Khách hàng (người thuê)
    idNguoiTao INT NULL,  -- Nhân viên kinh doanh lập hợp đồng
    idYeuCau INT NULL,  -- Yêu cầu thuê gốc, nếu có; duy nhất khi có giá trị
    idDiemNhan INT NOT NULL,
    idDiemTra INT NOT NULL,
    CONSTRAINT PK_hopDong PRIMARY KEY CLUSTERED (idHopDong),
    CONSTRAINT UQ_hopDong_maHopDong UNIQUE (maHopDong),
    CONSTRAINT FK_hopDong_uuDai FOREIGN KEY (idUuDai) REFERENCES dbo.uuDai (idUuDai),
    CONSTRAINT FK_hopDong_khach FOREIGN KEY (idNguoiDung) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT FK_hopDong_nguoiTao FOREIGN KEY (idNguoiTao) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT FK_hopDong_yeuCau FOREIGN KEY (idYeuCau) REFERENCES dbo.yeuCauThue (idYeuCau),
    CONSTRAINT FK_hopDong_diemNhan FOREIGN KEY (idDiemNhan) REFERENCES dbo.diemGiaoNhan (idDiem),
    CONSTRAINT FK_hopDong_diemTra FOREIGN KEY (idDiemTra) REFERENCES dbo.diemGiaoNhan (idDiem),
    CONSTRAINT CK_hopDong_thoiGian CHECK (ngayKetThucThue > ngayBatDauThue AND DATEDIFF(SECOND, ngayBatDauThue, ngayKetThucThue) >= 14400 AND ngayKetThucThue <= DATEADD(DAY, 30, ngayBatDauThue)),
    CONSTRAINT CK_hopDong_tien CHECK (tongTienGoc >= 0 AND tienGiamGia >= 0 AND tienGiamGia <= tongTienGoc AND tienDatCoc >= 0),
    CONSTRAINT CK_hopDong_trangThai CHECK (trangThai IN (0, 1, 2, 3, 4)),
    CONSTRAINT CK_hopDong_uuDai CHECK ((idUuDai IS NULL AND tenUuDaiApDung IS NULL AND phanTramApDung IS NULL AND tienGiamGia = 0) OR (idUuDai IS NOT NULL AND tenUuDaiApDung IS NOT NULL AND phanTramApDung BETWEEN 0.01 AND 100)),
    CONSTRAINT CK_hopDong_hanGiuCho CHECK (hanGiuCho IS NULL OR hanGiuCho > ngayTao)
);
GO

/* ---------- Bảng dbo.chiTietHopDong: Dòng chi tiết: xe trong hợp đồng, ghi giá tại thời điểm lập (BR-09, BR-10) ---------- */
CREATE TABLE dbo.chiTietHopDong (
    idChiTietHopDong INT IDENTITY(1,1) NOT NULL,
    idHopDong INT NOT NULL,  -- Mỗi hợp đồng đúng một dòng (UNIQUE, mục 5.1)
    idXe INT NOT NULL,
    giaThueNgayTaiThoiDiem DECIMAL(18,2) NOT NULL,  -- DailyRate x hệ số bảng giá tại StartAt (BR-10a)
    giaThueTuanTaiThoiDiem DECIMAL(18,2) NOT NULL,  -- WeeklyRate tại thời điểm lập
    soNgayThue INT NOT NULL,  -- Số ngày tính tiền = làm tròn lên (giờ / 24), tối thiểu 1 (BR-09)
    soTuanThue INT NOT NULL CONSTRAINT DF_chiTietHopDong_soTuanThue DEFAULT (0),  -- Số tuần đủ 7 ngày, tính theo giá tuần (BR-10b)
    thanhTienGoc AS (CONVERT(DECIMAL(18,2), soTuanThue * giaThueTuanTaiThoiDiem + (soNgayThue - 7 * soTuanThue) * giaThueNgayTaiThoiDiem)) PERSISTED NOT NULL,  -- Tính toán: tiền thuê trước ưu đãi và phụ phí
    CONSTRAINT PK_chiTietHopDong PRIMARY KEY CLUSTERED (idChiTietHopDong),
    CONSTRAINT UQ_chiTietHopDong_idHopDong UNIQUE (idHopDong),
    CONSTRAINT UQ_chiTietHopDong_HopDong_Xe UNIQUE (idHopDong, idXe),
    CONSTRAINT FK_chiTietHopDong_hopDong FOREIGN KEY (idHopDong) REFERENCES dbo.hopDong (idHopDong),
    CONSTRAINT FK_chiTietHopDong_xe FOREIGN KEY (idXe) REFERENCES dbo.xe (idXe),
    CONSTRAINT CK_chiTietHopDong_gia CHECK (giaThueNgayTaiThoiDiem >= 0 AND giaThueTuanTaiThoiDiem >= 0),
    CONSTRAINT CK_chiTietHopDong_soNgayThue CHECK (soNgayThue > 0),
    CONSTRAINT CK_chiTietHopDong_soTuanThue CHECK (soTuanThue >= 0 AND soTuanThue * 7 <= soNgayThue)
);
GO

/* ---------- Bảng dbo.bienBanBanGiao: Biên bản bàn giao và nhận lại xe; mỗi hợp đồng có một biên bản (mục 5.1) ---------- */
CREATE TABLE dbo.bienBanBanGiao (
    idBienBanBanGiao INT IDENTITY(1,1) NOT NULL,
    idHopDong INT NOT NULL,
    ngayBanGiao DATETIME2(0) NULL,
    soKmGiao INT NULL,
    mucNhienLieuGiao TINYINT NULL,  -- Phần trăm nhiên liệu 0-100
    tinhTrangLucGiao NVARCHAR(MAX) NULL,
    phuKienGiao NVARCHAR(255) NULL,
    ngayNhanLai DATETIME2(0) NULL,
    soKmNhan INT NULL,
    mucNhienLieuNhan TINYINT NULL,
    tinhTrangLucNhan NVARCHAR(MAX) NULL,
    phuKienNhan NVARCHAR(255) NULL,
    chiPhiPhatSinh DECIMAL(18,2) NOT NULL CONSTRAINT DF_bienBanBanGiao_chiPhiPhatSinh DEFAULT (0),  -- Tổng phụ phí SP-01 đến SP-03 ghi tại biên bản
    CONSTRAINT PK_bienBanBanGiao PRIMARY KEY CLUSTERED (idBienBanBanGiao),
    CONSTRAINT UQ_bienBanBanGiao_idHopDong UNIQUE (idHopDong),
    CONSTRAINT FK_bienBanBanGiao_hopDong FOREIGN KEY (idHopDong) REFERENCES dbo.hopDong (idHopDong),
    CONSTRAINT CK_bienBanBanGiao_soKmGiao CHECK (soKmGiao IS NULL OR soKmGiao >= 0),
    CONSTRAINT CK_bienBanBanGiao_soKmNhan CHECK (soKmNhan IS NULL OR soKmNhan >= 0),
    CONSTRAINT CK_bienBanBanGiao_soKm CHECK (soKmNhan IS NULL OR soKmGiao IS NULL OR soKmNhan >= soKmGiao),
    CONSTRAINT CK_bienBanBanGiao_nhienLieu CHECK ((mucNhienLieuGiao IS NULL OR mucNhienLieuGiao BETWEEN 0 AND 100) AND (mucNhienLieuNhan IS NULL OR mucNhienLieuNhan BETWEEN 0 AND 100)),
    CONSTRAINT CK_bienBanBanGiao_thoiGian CHECK (ngayNhanLai IS NULL OR (ngayBanGiao IS NOT NULL AND ngayNhanLai > ngayBanGiao)),
    CONSTRAINT CK_bienBanBanGiao_chiPhi CHECK (chiPhiPhatSinh >= 0)
);
GO

/* ---------- Bảng dbo.anhBienBan: Ảnh đính kèm biên bản giao hoặc nhận xe (HandoverImage, FR-4.1, FR-4.2) ---------- */
CREATE TABLE dbo.anhBienBan (
    idAnh INT IDENTITY(1,1) NOT NULL,
    idBienBanBanGiao INT NOT NULL,
    duongDanAnh VARCHAR(255) NOT NULL,
    loaiAnh INT NOT NULL,  -- 1 Giao xe; 2 Nhận lại xe
    CONSTRAINT PK_anhBienBan PRIMARY KEY CLUSTERED (idAnh),
    CONSTRAINT FK_anhBienBan_bienBan FOREIGN KEY (idBienBanBanGiao) REFERENCES dbo.bienBanBanGiao (idBienBanBanGiao),
    CONSTRAINT CK_anhBienBan_loaiAnh CHECK (loaiAnh IN (1, 2))
);
GO

/* ---------- Bảng dbo.thanhToan: Giao dịch thu hoặc chi của hợp đồng (BR-04: có người thực hiện và thời điểm) ---------- */
CREATE TABLE dbo.thanhToan (
    idThanhToan INT IDENTITY(1,1) NOT NULL,
    maGiaoDich VARCHAR(30) NOT NULL,  -- Mã giao dịch duy nhất (TransactionCode)
    idHopDong INT NOT NULL,
    soTien DECIMAL(18,2) NOT NULL,  -- Luôn dương; loại 4 là số tiền chi trả lại khách
    loaiThanhToan INT NOT NULL,  -- 1 Đặt cọc; 2 Tiền thuê; 3 Phụ phí; 4 Hoàn cọc; 5 Phí giữ lại khi hủy (BR-14, bản 2.1)
    phuongThuc NVARCHAR(50) NULL,  -- Tiền mặt, chuyển khoản hoặc cổng thử nghiệm
    ngayThanhToan DATETIME2(0) NOT NULL CONSTRAINT DF_thanhToan_ngayThanhToan DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    trangThai INT NOT NULL CONSTRAINT DF_thanhToan_trangThai DEFAULT (1),  -- 1 Hợp lệ; 0 Đã đảo bút toán (không xóa, BR-05)
    nguoiThucHien INT NOT NULL,  -- Người ghi nhận, thường là kế toán (BR-04)
    CONSTRAINT PK_thanhToan PRIMARY KEY CLUSTERED (idThanhToan),
    CONSTRAINT UQ_thanhToan_maGiaoDich UNIQUE (maGiaoDich),
    CONSTRAINT FK_thanhToan_hopDong FOREIGN KEY (idHopDong) REFERENCES dbo.hopDong (idHopDong),
    CONSTRAINT FK_thanhToan_nguoiThucHien FOREIGN KEY (nguoiThucHien) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT CK_thanhToan_soTien CHECK (soTien > 0),
    CONSTRAINT CK_thanhToan_loaiThanhToan CHECK (loaiThanhToan IN (1, 2, 3, 4, 5)),
    CONSTRAINT CK_thanhToan_trangThai CHECK (trangThai IN (0, 1)),
    CONSTRAINT CK_thanhToan_phuongThuc CHECK (phuongThuc IS NULL OR phuongThuc IN (N'Tiền mặt', N'Chuyển khoản', N'Cổng thử nghiệm'))
);
GO

/* ---------- Bảng dbo.danhGia: Đánh giá của khách về xe trong hợp đồng (chưa có FR tương ứng; nhóm cần quyết định, mục 5.2) ---------- */
CREATE TABLE dbo.danhGia (
    idDanhGia INT IDENTITY(1,1) NOT NULL,
    soSao TINYINT NOT NULL,  -- Từ 1 đến 5
    noiDung NVARCHAR(MAX) NULL,
    ngayDanhGia DATETIME2(0) NOT NULL CONSTRAINT DF_danhGia_ngayDanhGia DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    idHopDong INT NOT NULL,
    idXe INT NOT NULL,
    CONSTRAINT PK_danhGia PRIMARY KEY CLUSTERED (idDanhGia),
    CONSTRAINT UQ_danhGia_HopDong_Xe UNIQUE (idHopDong, idXe),
    CONSTRAINT FK_danhGia_chiTietHopDong FOREIGN KEY (idHopDong, idXe) REFERENCES dbo.chiTietHopDong (idHopDong, idXe),
    CONSTRAINT CK_danhGia_soSao CHECK (soSao BETWEEN 1 AND 5)
);
GO

/* ---------- Bảng dbo.lichSuHopDong: Lịch sử chuyển trạng thái hợp đồng (ContractHistory, BR-08); chỉ thêm, không sửa, không xóa ---------- */
CREATE TABLE dbo.lichSuHopDong (
    idLichSu INT IDENTITY(1,1) NOT NULL,
    idHopDong INT NOT NULL,
    trangThaiCu INT NULL,  -- NULL khi tạo mới
    trangThaiMoi INT NOT NULL,
    thoiDiem DATETIME2(0) NOT NULL CONSTRAINT DF_lichSuHopDong_thoiDiem DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    idNguoiThucHien INT NOT NULL,
    ghiChu NVARCHAR(255) NULL,
    CONSTRAINT PK_lichSuHopDong PRIMARY KEY CLUSTERED (idLichSu),
    CONSTRAINT FK_lichSuHopDong_hopDong FOREIGN KEY (idHopDong) REFERENCES dbo.hopDong (idHopDong),
    CONSTRAINT FK_lichSuHopDong_nguoiDung FOREIGN KEY (idNguoiThucHien) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT CK_lichSuHopDong_cu CHECK (trangThaiCu IS NULL OR trangThaiCu IN (0, 1, 2, 3, 4)),
    CONSTRAINT CK_lichSuHopDong_moi CHECK (trangThaiMoi IN (0, 1, 2, 3, 4))
);
GO

/* ---------- Bảng dbo.thongBao: Thông báo gửi người dùng (Notification, FR-8.1); khi chưa có SMTP thì ghi trạng thái Mô phỏng ---------- */
CREATE TABLE dbo.thongBao (
    idThongBao INT IDENTITY(1,1) NOT NULL,
    idNguoiDung INT NOT NULL,
    idHopDong INT NULL,
    loai VARCHAR(30) NOT NULL,
    noiDung NVARCHAR(MAX) NOT NULL,
    trangThaiGui INT NOT NULL,  -- 1 Mô phỏng; 2 Đã gửi; 3 Lỗi
    thoiDiem DATETIME2(0) NOT NULL CONSTRAINT DF_thongBao_thoiDiem DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT PK_thongBao PRIMARY KEY CLUSTERED (idThongBao),
    CONSTRAINT FK_thongBao_nguoiDung FOREIGN KEY (idNguoiDung) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT FK_thongBao_hopDong FOREIGN KEY (idHopDong) REFERENCES dbo.hopDong (idHopDong),
    CONSTRAINT CK_thongBao_trangThaiGui CHECK (trangThaiGui IN (1, 2, 3)),
    CONSTRAINT CK_thongBao_loai CHECK (loai IN ('XAC_NHAN_HOP_DONG', 'HUY_HOP_DONG', 'NHAC_DAT_COC', 'NHAC_TRA_XE', 'QUA_HAN', 'HET_HAN_GIU_CHO'))
);
GO

/* ---------- Bảng dbo.nhatKyKiemToan: Nhật ký kiểm toán (AuditLog, NFR-09); chỉ thêm, không sửa, không xóa ---------- */
CREATE TABLE dbo.nhatKyKiemToan (
    idNhatKy BIGINT IDENTITY(1,1) NOT NULL,
    idNguoiThucHien INT NULL,  -- NULL nếu là hành động của hệ thống
    doiTuong VARCHAR(30) NOT NULL,  -- NguoiDung, Xe, HopDong, ThanhToan, PhanQuyen, ...
    idDoiTuong VARCHAR(50) NULL,
    hanhDong VARCHAR(50) NOT NULL,
    giaTriCu NVARCHAR(MAX) NULL,
    giaTriMoi NVARCHAR(MAX) NULL,
    thoiDiem DATETIME2(0) NOT NULL CONSTRAINT DF_nhatKyKiemToan_thoiDiem DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT PK_nhatKyKiemToan PRIMARY KEY CLUSTERED (idNhatKy),
    CONSTRAINT FK_nhatKyKiemToan_nguoiDung FOREIGN KEY (idNguoiThucHien) REFERENCES dbo.nguoiDung (idNguoiDung)
);
GO

/* ---------- Bảng dbo.maDangKyNhanVien: Mã đăng ký nhân viên do quản trị viên cấp (v4). Chỉ lưu bản băm SHA-256 của mã; mã thô chỉ hiện một lần lúc cấp. Mã dùng một lần, gắn với một email và một vai trò, hết hạn sau 7 ngày. ---------- */
CREATE TABLE dbo.maDangKyNhanVien (
    idMa INT IDENTITY(1,1) NOT NULL,
    maBam VARCHAR(64) NOT NULL,  -- SHA-256 (dạng hex) của mã đã chuẩn hóa; duy nhất
    email VARCHAR(100) NOT NULL,  -- Email nhân viên phải dùng khi đăng ký; luôn lưu chữ thường
    hoTen NVARCHAR(100) NOT NULL,  -- Họ tên gắn với mã; tài khoản được tạo sẽ lấy họ tên này
    idVaiTro INT NOT NULL,  -- Vai trò của tài khoản được tạo từ mã
    hetHan DATETIME2(0) NOT NULL,  -- Hết hạn sau 7 ngày kể từ lúc cấp
    ngayDung DATETIME2(0) NULL,  -- Thời điểm nhân viên đã dùng mã; NULL = chưa dùng
    idNguoiCap INT NOT NULL,  -- Quản trị viên đã cấp mã
    ngayCap DATETIME2(0) NOT NULL CONSTRAINT DF_maDangKyNhanVien_ngayCap DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    thuHoi BIT NOT NULL CONSTRAINT DF_maDangKyNhanVien_thuHoi DEFAULT (0),  -- 1 = đã thu hồi, hoặc tự thu hồi khi cấp mã mới cho mã đã hết hạn
    CONSTRAINT PK_maDangKyNhanVien PRIMARY KEY CLUSTERED (idMa),
    CONSTRAINT UQ_maDangKyNhanVien_maBam UNIQUE (maBam),
    CONSTRAINT FK_maDangKyNhanVien_vaiTro FOREIGN KEY (idVaiTro) REFERENCES dbo.vaiTro (idVaiTro),
    CONSTRAINT FK_maDangKyNhanVien_nguoiCap FOREIGN KEY (idNguoiCap) REFERENCES dbo.nguoiDung (idNguoiDung),
    CONSTRAINT CK_maDangKyNhanVien_email CHECK (LEN(LTRIM(RTRIM(email))) > 0 AND CHARINDEX('@', email) > 1),
    CONSTRAINT CK_maDangKyNhanVien_hetHan CHECK (hetHan > ngayCap),
    CONSTRAINT CK_maDangKyNhanVien_ngayDung CHECK (ngayDung IS NULL OR ngayDung >= ngayCap)
);
GO


/* ----- Chỉ mục (index) -----
   Nguyên tắc: cột khóa ngoại hay lọc/nối đều có chỉ mục; các truy vấn nóng của view vLichXe, hàm fnXeKhaDung,
   trigger và job được "phủ" bằng INCLUDE để không phải tra ngược về bảng. */

-- xe, nguoiDung
CREATE NONCLUSTERED INDEX IX_xe_idLoaiXe ON dbo.xe (idLoaiXe);
CREATE NONCLUSTERED INDEX IX_nguoiDung_idVaiTro ON dbo.nguoiDung (idVaiTro);

-- yeuCauThue
CREATE NONCLUSTERED INDEX IX_yeuCauThue_idXe_trangThai ON dbo.yeuCauThue (idXe, trangThai) INCLUDE (ngayNhan, ngayTra, hanGiuCho);
CREATE NONCLUSTERED INDEX IX_yeuCauThue_idNguoiDung ON dbo.yeuCauThue (idNguoiDung);

-- bangGia, uuDaiXe, lichBaoTri
CREATE NONCLUSTERED INDEX IX_bangGia_idLoaiXe ON dbo.bangGia (idLoaiXe, ngayBatDau, ngayKetThuc) INCLUDE (heSo, trangThai);
CREATE NONCLUSTERED INDEX IX_uuDaiXe_idXe ON dbo.uuDaiXe (idXe);
CREATE NONCLUSTERED INDEX IX_lichBaoTri_idXe ON dbo.lichBaoTri (idXe, tgKetThuc) INCLUDE (tgBatDau);

-- hopDong, chiTietHopDong
CREATE NONCLUSTERED INDEX IX_hopDong_trangThai_ngay ON dbo.hopDong (trangThai, ngayBatDauThue, ngayKetThucThue) INCLUDE (hanGiuCho);
CREATE NONCLUSTERED INDEX IX_hopDong_idNguoiDung ON dbo.hopDong (idNguoiDung);
CREATE NONCLUSTERED INDEX IX_hopDong_idNguoiTao ON dbo.hopDong (idNguoiTao);
CREATE NONCLUSTERED INDEX IX_hopDong_idUuDai ON dbo.hopDong (idUuDai);
CREATE UNIQUE NONCLUSTERED INDEX UX_hopDong_idYeuCau ON dbo.hopDong (idYeuCau) WHERE idYeuCau IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_chiTietHopDong_idXe ON dbo.chiTietHopDong (idXe);

-- danhGia, anhBienBan
CREATE NONCLUSTERED INDEX IX_danhGia_idXe ON dbo.danhGia (idXe) INCLUDE (soSao);
CREATE NONCLUSTERED INDEX IX_anhBienBan_idBienBanBanGiao ON dbo.anhBienBan (idBienBanBanGiao);

-- thanhToan, lichSuHopDong, thongBao, nhatKyKiemToan
CREATE NONCLUSTERED INDEX IX_thanhToan_idHopDong ON dbo.thanhToan (idHopDong) INCLUDE (loaiThanhToan, trangThai, soTien);
CREATE NONCLUSTERED INDEX IX_thanhToan_nguoiThucHien ON dbo.thanhToan (nguoiThucHien);
CREATE NONCLUSTERED INDEX IX_lichSuHopDong_idHopDong ON dbo.lichSuHopDong (idHopDong);
CREATE NONCLUSTERED INDEX IX_thongBao_idNguoiDung ON dbo.thongBao (idNguoiDung, thoiDiem DESC);
CREATE NONCLUSTERED INDEX IX_nhatKyKiemToan_thoiDiem ON dbo.nhatKyKiemToan (thoiDiem);
CREATE NONCLUSTERED INDEX IX_nhatKyKiemToan_idNguoiThucHien ON dbo.nhatKyKiemToan (idNguoiThucHien);
CREATE NONCLUSTERED INDEX IX_nhatKyKiemToan_doiTuong ON dbo.nhatKyKiemToan (doiTuong, idDoiTuong);

-- maDangKyNhanVien: mỗi email chỉ có một mã chưa dùng và chưa thu hồi (v4)
CREATE UNIQUE NONCLUSTERED INDEX UX_maDangKyNhanVien_email_chuaDung ON dbo.maDangKyNhanVien (email) WHERE ngayDung IS NULL AND thuHoi = 0;
CREATE NONCLUSTERED INDEX IX_maDangKyNhanVien_idVaiTro ON dbo.maDangKyNhanVien (idVaiTro);
CREATE NONCLUSTERED INDEX IX_maDangKyNhanVien_idNguoiCap ON dbo.maDangKyNhanVien (idNguoiCap);
GO


/* ===================== PHẦN 2: VIEW VÀ HÀM ===================== */

/* Lịch xe: mọi khoảng thời gian xe đang bị giữ, theo nửa mở [batDau, ketThuc) (BR-01).
   - Hợp đồng Chờ xác nhận, Đã xác nhận: giữ đến EndAt.
   - Hợp đồng Đang thuê: giữ đến khi xe được trả (ngayNhanLai); chưa trả thì giữ mãi (BR-13).
   - Yêu cầu Chờ xử lý còn hạn giữ chỗ (BR-15). */
CREATE VIEW dbo.vLichXe
AS
SELECT ct.idXe,
       h.idHopDong,
       h.maHopDong,
       h.trangThai AS trangThaiHopDong,
       h.ngayBatDauThue AS batDau,
       CASE WHEN bb.ngayNhanLai IS NOT NULL THEN bb.ngayNhanLai
            WHEN h.trangThai = 2 THEN CAST('9999-12-31T23:59:59' AS DATETIME2(0))
            ELSE h.ngayKetThucThue
       END AS ketThuc,
       N'HopDong' AS nguon
FROM dbo.hopDong AS h
JOIN dbo.chiTietHopDong AS ct ON ct.idHopDong = h.idHopDong
LEFT JOIN dbo.bienBanBanGiao AS bb ON bb.idHopDong = h.idHopDong
WHERE h.trangThai IN (1, 2)
   OR (h.trangThai = 0 AND h.hanGiuCho > DATEADD(HOUR, 7, SYSUTCDATETIME()))
UNION ALL
SELECT yc.idXe, NULL, NULL, yc.trangThai, yc.ngayNhan, yc.ngayTra, N'YeuCau'
FROM dbo.yeuCauThue AS yc
WHERE yc.trangThai = 1
  AND yc.hanGiuCho > DATEADD(HOUR, 7, SYSUTCDATETIME())
UNION ALL
SELECT lb.idXe, NULL, NULL, NULL, lb.tgBatDau, lb.tgKetThuc, N'BaoTri'
FROM dbo.lichBaoTri AS lb
WHERE lb.tgKetThuc > DATEADD(HOUR, 7, SYSUTCDATETIME());
GO
/* Tổng hợp hợp đồng cho màn hình quản trị; quaHan và sapDenHan theo BR-07 và BR-13. */
CREATE VIEW dbo.vHopDongTongHop
AS
SELECT h.idHopDong,
       h.maHopDong,
       h.trangThai,
       h.ngayBatDauThue,
       h.ngayKetThucThue,
       h.tongTienGoc,
       h.tienGiamGia,
       h.tongTienThanhToan,
       h.tienDatCoc,
       kh.hoTen AS tenKhachHang,
       kh.email AS emailKhachHang,
       x.idXe,
       x.tenXe,
       x.bienSo,
       CASE WHEN h.trangThai = 2 AND bb.ngayNhanLai IS NULL
                 AND h.ngayKetThucThue < DATEADD(HOUR, 7, SYSUTCDATETIME())
            THEN 1 ELSE 0 END AS quaHan,
       CASE WHEN h.trangThai IN (1, 2) AND bb.ngayNhanLai IS NULL
                 AND h.ngayKetThucThue >= DATEADD(HOUR, 7, SYSUTCDATETIME())
                 AND h.ngayKetThucThue < DATEADD(HOUR, 24, DATEADD(HOUR, 7, SYSUTCDATETIME()))
            THEN 1 ELSE 0 END AS sapDenHan
FROM dbo.hopDong AS h
JOIN dbo.chiTietHopDong AS ct ON ct.idHopDong = h.idHopDong
JOIN dbo.xe AS x ON x.idXe = ct.idXe
JOIN dbo.nguoiDung AS kh ON kh.idNguoiDung = h.idNguoiDung
LEFT JOIN dbo.bienBanBanGiao AS bb ON bb.idHopDong = h.idHopDong;
GO

/* Hàm trả về các xe khả dụng trong [@batDau, @ketThuc) (FR-2.1, BR-01).
   Xe Bảo trì (3) và Ngừng hoạt động (4) không khả dụng (mục 3.8). */
CREATE FUNCTION dbo.fnXeKhaDung (@batDau DATETIME2(0), @ketThuc DATETIME2(0))
RETURNS TABLE
AS
RETURN
(
    SELECT x.idXe, x.tenXe, x.bienSo, x.giaThueNgay, x.giaThueTuan, x.tienDatCoc
    FROM dbo.xe AS x
    WHERE x.trangThai NOT IN (3, 4)
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.vLichXe AS v
            WHERE v.idXe = x.idXe
              AND @batDau < v.ketThuc
              AND v.batDau < @ketThuc)
);
GO


/* ===================== PHẦN 3: TRIGGER KIỂM TRA NGHIỆP VỤ ===================== */

/* Quy ước chung của 4 trigger chống chồng lịch:
   - Trước khi kiểm tra, khóa (UPDLOCK, HOLDLOCK) dòng xe liên quan để các giao dịch đặt cùng một xe phải xếp hàng.
     Nhờ đó hai giao dịch chạy đồng thời không thể cùng vượt qua kiểm tra rồi đặt trùng lịch,
     kể cả khi sau này bật READ_COMMITTED_SNAPSHOT.
   - Lỗi nghiệp vụ dùng RAISERROR (mức 16) rồi ROLLBACK; tầng ứng dụng nhận biết qua chuỗi "mã lỗi 409". */

/* BR-01, BR-11: một xe không chồng lịch với hợp đồng khác, yêu cầu còn hạn giữ chỗ, hoặc lịch bảo trì (mã lỗi 409 ở tầng ứng dụng).
   Hợp đồng chưa bàn giao (Chờ xác nhận, Đã xác nhận) cũng không được gán cho xe đang Bảo trì hoặc Ngừng hoạt động. */
CREATE TRIGGER dbo.trg_chiTietHopDong_KhongChongLich
ON dbo.chiTietHopDong
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @now DATETIME2(0) = DATEADD(HOUR, 7, SYSUTCDATETIME());
    DECLARE @khoa INT;

    SELECT @khoa = x.idXe
    FROM dbo.xe AS x WITH (UPDLOCK, HOLDLOCK)
    WHERE x.idXe IN (SELECT i.idXe FROM inserted AS i);

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN dbo.hopDong AS h1 ON h1.idHopDong = i.idHopDong
        JOIN dbo.xe AS xe1 ON xe1.idXe = i.idXe
        WHERE h1.trangThai IN (0, 1, 2)
          AND (
               (h1.trangThai IN (0, 1) AND xe1.trangThai IN (3, 4))
            OR EXISTS (SELECT 1 FROM dbo.vLichXe AS v
                       WHERE v.idXe = i.idXe
                         AND (v.nguon = N'BaoTri' OR (v.nguon = N'HopDong' AND v.idHopDong <> i.idHopDong))
                         AND h1.ngayBatDauThue < v.ketThuc AND v.batDau < h1.ngayKetThucThue)
            OR EXISTS (SELECT 1 FROM dbo.yeuCauThue AS yc
                       WHERE yc.idXe = i.idXe AND yc.trangThai = 1 AND yc.hanGiuCho > @now
                         AND yc.idYeuCau <> ISNULL(h1.idYeuCau, -1)
                         AND h1.ngayBatDauThue < yc.ngayTra AND yc.ngayNhan < h1.ngayKetThucThue)
          ))
    BEGIN
        RAISERROR (N'Xe bị chồng lịch thuê hoặc lịch bảo trì (BR-01, BR-11, mã lỗi 409).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* BR-01: yêu cầu thuê đang giữ chỗ không chồng lịch xe đang bị giữ, và xe không ở trạng thái Bảo trì hoặc Ngừng hoạt động (mã lỗi 409).
   Kiểm tra khi thêm mới và khi sửa xe, ngày nhận/trả, trạng thái, hạn giữ chỗ; yêu cầu đã chuyển hợp đồng, đã hủy hoặc hết hạn thì không giữ xe nên không bị kiểm tra. */
CREATE TRIGGER dbo.trg_yeuCauThue_KhongChongLich
ON dbo.yeuCauThue
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(idXe) OR UPDATE(ngayNhan) OR UPDATE(ngayTra) OR UPDATE(trangThai) OR UPDATE(hanGiuCho))
        RETURN;

    DECLARE @now DATETIME2(0) = DATEADD(HOUR, 7, SYSUTCDATETIME());
    DECLARE @khoa INT;

    SELECT @khoa = x.idXe
    FROM dbo.xe AS x WITH (UPDLOCK, HOLDLOCK)
    WHERE x.idXe IN (SELECT i.idXe FROM inserted AS i WHERE i.trangThai = 1);

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN dbo.xe AS x ON x.idXe = i.idXe
        WHERE i.trangThai = 1 AND i.hanGiuCho > @now
          AND (
               x.trangThai IN (3, 4)
            OR EXISTS (SELECT 1 FROM dbo.vLichXe AS v
                       WHERE v.idXe = i.idXe AND v.nguon IN (N'HopDong', N'BaoTri')
                         AND v.batDau < i.ngayTra AND i.ngayNhan < v.ketThuc)
            OR EXISTS (SELECT 1 FROM dbo.yeuCauThue AS yc
                       WHERE yc.idXe = i.idXe AND yc.trangThai = 1 AND yc.hanGiuCho > @now
                         AND yc.idYeuCau <> i.idYeuCau
                         AND yc.ngayNhan < i.ngayTra AND i.ngayNhan < yc.ngayTra)
          ))
    BEGIN
        RAISERROR (N'Xe không khả dụng trong khoảng thời gian yêu cầu (BR-01, BR-11, mã lỗi 409).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* BR-01, BR-11: đổi ngày thuê của hợp đồng không được làm chồng lịch (mã lỗi 409).
   Yêu cầu thuê gốc của chính hợp đồng này không bị tính là xung đột (cùng quy tắc với trigger trên chiTietHopDong). */
CREATE TRIGGER dbo.trg_hopDong_KhongChongLichKhiDoiNgay
ON dbo.hopDong
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(ngayBatDauThue) OR UPDATE(ngayKetThucThue))
        RETURN;

    DECLARE @now DATETIME2(0) = DATEADD(HOUR, 7, SYSUTCDATETIME());
    DECLARE @khoa INT;

    SELECT @khoa = x.idXe
    FROM dbo.xe AS x WITH (UPDLOCK, HOLDLOCK)
    WHERE x.idXe IN (SELECT ct0.idXe
                     FROM dbo.chiTietHopDong AS ct0
                     JOIN inserted AS i0 ON i0.idHopDong = ct0.idHopDong);

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN dbo.chiTietHopDong AS ct ON ct.idHopDong = i.idHopDong
        WHERE i.trangThai IN (0, 1, 2)
          AND (
               EXISTS (SELECT 1 FROM dbo.vLichXe AS v
                       WHERE v.idXe = ct.idXe
                         AND (v.nguon = N'BaoTri' OR (v.nguon = N'HopDong' AND v.idHopDong <> i.idHopDong))
                         AND i.ngayBatDauThue < v.ketThuc AND v.batDau < i.ngayKetThucThue)
            OR EXISTS (SELECT 1 FROM dbo.yeuCauThue AS yc
                       WHERE yc.idXe = ct.idXe AND yc.trangThai = 1 AND yc.hanGiuCho > @now
                         AND yc.idYeuCau <> ISNULL(i.idYeuCau, -1)
                         AND i.ngayBatDauThue < yc.ngayTra AND yc.ngayNhan < i.ngayKetThucThue)
          ))
    BEGIN
        RAISERROR (N'Đổi ngày thuê làm chồng lịch xe (BR-01, BR-11, mã lỗi 409).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* BR-11: lịch bảo trì không được trùng hợp đồng đã xác nhận hoặc đang thuê (mã lỗi 409).
   Kiểm tra cả khi thêm mới lẫn khi sửa xe hoặc thời gian của lịch bảo trì. */
CREATE TRIGGER dbo.trg_lichBaoTri_KhongChongHopDong
ON dbo.lichBaoTri
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(idXe) OR UPDATE(tgBatDau) OR UPDATE(tgKetThuc))
        RETURN;

    DECLARE @khoa INT;

    SELECT @khoa = x.idXe
    FROM dbo.xe AS x WITH (UPDLOCK, HOLDLOCK)
    WHERE x.idXe IN (SELECT i.idXe FROM inserted AS i);

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        WHERE EXISTS (SELECT 1 FROM dbo.vLichXe AS v
                      WHERE v.idXe = i.idXe AND v.nguon = N'HopDong' AND v.trangThaiHopDong IN (1, 2)
                        AND v.batDau < i.tgKetThuc AND i.tgBatDau < v.ketThuc))
    BEGIN
        RAISERROR (N'Lịch bảo trì trùng hợp đồng đã xác nhận hoặc đang thuê (BR-11, mã lỗi 409).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* BR-08: chỉ cho phép các chuyển trạng thái trong bảng mục 3.8 (hợp đồng).
   Hợp lệ: 0->1, 0->4, 1->2, 1->4, 2->3. */
CREATE TRIGGER dbo.trg_hopDong_ChuyenTrangThai
ON dbo.hopDong
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(trangThai) AND EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted AS d ON d.idHopDong = i.idHopDong
        WHERE i.trangThai <> d.trangThai
          AND NOT ((d.trangThai = 0 AND i.trangThai IN (1, 4))
                OR (d.trangThai = 1 AND i.trangThai IN (2, 4))
                OR (d.trangThai = 2 AND i.trangThai = 3)))
    BEGIN
        RAISERROR (N'Chuyển trạng thái hợp đồng không hợp lệ theo bảng mục 3.8 (BR-08).', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* BR-08: trạng thái yêu cầu thuê. Hợp lệ: 1->2, 1->3. */
CREATE TRIGGER dbo.trg_yeuCauThue_ChuyenTrangThai
ON dbo.yeuCauThue
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(trangThai) AND EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted AS d ON d.idYeuCau = i.idYeuCau
        WHERE i.trangThai <> d.trangThai
          AND NOT (d.trangThai = 1 AND i.trangThai IN (2, 3)))
    BEGIN
        RAISERROR (N'Chuyển trạng thái yêu cầu thuê không hợp lệ (BR-08).', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* BR-08 và BR-11: trạng thái xe. Hợp lệ: 1->2, 1->3, 1->4, 2->1, 2->3, 3->1.
   Chuyển sang Bảo trì hoặc Ngừng hoạt động bị từ chối khi còn hợp đồng Đã xác nhận hoặc Đang thuê chưa kết thúc. */
CREATE TRIGGER dbo.trg_xe_ChuyenTrangThai
ON dbo.xe
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(trangThai) AND EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted AS d ON d.idXe = i.idXe
        WHERE i.trangThai <> d.trangThai
          AND NOT ((d.trangThai = 1 AND i.trangThai IN (2, 3, 4))
                OR (d.trangThai = 2 AND i.trangThai IN (1, 3))
                OR (d.trangThai = 3 AND i.trangThai = 1)))
    BEGIN
        RAISERROR (N'Chuyển trạng thái xe không hợp lệ theo bảng mục 3.8 (BR-08).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF UPDATE(trangThai) AND EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted AS d ON d.idXe = i.idXe
        WHERE i.trangThai IN (3, 4) AND i.trangThai <> d.trangThai
          AND EXISTS (SELECT 1 FROM dbo.vLichXe AS v
                      WHERE v.idXe = i.idXe AND v.nguon = N'HopDong'
                        AND v.trangThaiHopDong IN (1, 2)
                        AND v.ketThuc > DATEADD(HOUR, 7, SYSUTCDATETIME())))
    BEGIN
        RAISERROR (N'Không chuyển xe sang Bảo trì hoặc Ngừng hoạt động khi còn hợp đồng Đã xác nhận hoặc Đang thuê (BR-11).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* NFR-09 và BR-08: nhật ký kiểm toán và lịch sử hợp đồng chỉ được thêm, không sửa, không xóa. */
CREATE TRIGGER dbo.trg_nhatKyKiemToan_BatBien
ON dbo.nhatKyKiemToan
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;  -- lệnh không tác động dòng nào thì bỏ qua
    RAISERROR (N'Nhật ký kiểm toán chỉ được thêm, không được sửa hoặc xóa (NFR-09).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

CREATE TRIGGER dbo.trg_lichSuHopDong_BatBien
ON dbo.lichSuHopDong
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;  -- lệnh không tác động dòng nào thì bỏ qua
    RAISERROR (N'Lịch sử hợp đồng chỉ được thêm, không được sửa hoặc xóa (BR-08).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO


/* BR-05: giao dịch thanh toán không được xóa; muốn hủy phải đảo bút toán (trangThai = 0). */
CREATE TRIGGER dbo.trg_thanhToan_ChanXoa
ON dbo.thanhToan
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    RAISERROR (N'Giao dịch thanh toán không được xóa, chỉ được đảo bút toán (BR-05).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO


/* BR-15 (S4): tự hủy hợp đồng Chờ xác nhận quá hạn giữ chỗ mà chưa có đặt cọc. Chạy định kỳ bằng SQL Server Agent (phần 6 cuối script).
   - Một câu UPDATE duy nhất vừa chọn vừa hủy (điều kiện trạng thái và chưa đặt cọc được kiểm lại ngay lúc cập nhật),
     kết quả lấy bằng OUTPUT nên không còn khoảng hở giữa lúc chọn và lúc hủy.
   - XACT_ABORT + TRY/CATCH: lỗi bất kỳ (kể cả trigger từ chối) đều rollback sạch và báo lại cho Agent.
   - @idNguoiHeThong: tài khoản ghi vào lịch sử và nhật ký (mặc định QTV mã 1). */
CREATE OR ALTER PROCEDURE dbo.spHuyHopDongQuaHanGiuCho
    @idNguoiHeThong INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(0) = DATEADD(HOUR, 7, SYSUTCDATETIME());
    DECLARE @ds TABLE (idHopDong INT NOT NULL PRIMARY KEY, maHopDong VARCHAR(50) NOT NULL, idNguoiDung INT NOT NULL);

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE h
        SET h.trangThai = 4,
            h.lyDoHuy = N'Hết hạn giữ chỗ chưa đặt cọc (BR-15)'
        OUTPUT inserted.idHopDong, inserted.maHopDong, inserted.idNguoiDung INTO @ds (idHopDong, maHopDong, idNguoiDung)
        FROM dbo.hopDong AS h
        WHERE h.trangThai = 0
          AND h.hanGiuCho < @now
          AND NOT EXISTS (SELECT 1 FROM dbo.thanhToan AS t
                          WHERE t.idHopDong = h.idHopDong AND t.loaiThanhToan = 1 AND t.trangThai = 1);

        INSERT INTO dbo.lichSuHopDong (idHopDong, trangThaiCu, trangThaiMoi, thoiDiem, idNguoiThucHien, ghiChu)
        SELECT d.idHopDong, 0, 4, @now, @idNguoiHeThong, N'Tự hủy do hết hạn giữ chỗ (BR-15)'
        FROM @ds AS d;

        INSERT INTO dbo.thongBao (idNguoiDung, idHopDong, loai, noiDung, trangThaiGui, thoiDiem)
        SELECT d.idNguoiDung, d.idHopDong, 'HUY_HOP_DONG', N'Hợp đồng ' + d.maHopDong + N' đã bị hủy do quá hạn giữ chỗ.', 1, @now
        FROM @ds AS d;

        INSERT INTO dbo.nhatKyKiemToan (idNguoiThucHien, doiTuong, idDoiTuong, hanhDong, giaTriCu, giaTriMoi, thoiDiem)
        SELECT @idNguoiHeThong, 'HopDong', CAST(d.idHopDong AS VARCHAR(50)), 'TU_HUY_QUA_HAN', N'trangThai=0', N'trangThai=4', @now
        FROM @ds AS d;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ===================== PHẦN 4: DỮ LIỆU DANH MỤC ===================== */
/* Chỉ có dữ liệu danh mục để ứng dụng chạy được: vai trò, loại xe, điểm giao nhận, chính sách phụ phí mặc định.
   Không có xe mẫu, hợp đồng mẫu hay giao dịch mẫu. Hai tài khoản thử nghiệm nằm ở PHẦN 5. */

SET IDENTITY_INSERT dbo.vaiTro ON;
INSERT INTO dbo.vaiTro (idVaiTro, tenVaiTro, maVaiTro)
VALUES
    (1, N'Quản trị viên', N'ADMIN'),
    (2, N'Nhân viên kinh doanh', N'KINHDOANH'),
    (3, N'Nhân viên giao nhận', N'GIAONHAN'),
    (4, N'Kế toán', N'KETOAN'),
    (5, N'Khách hàng', N'KHACHHANG');
SET IDENTITY_INSERT dbo.vaiTro OFF;
GO

SET IDENTITY_INSERT dbo.loaiXe ON;
INSERT INTO dbo.loaiXe (idLoaiXe, tenLoai, moTa, hangGPLXToiThieu)
VALUES
    (1, N'Sedan hạng sang', N'Xe sedan phục vụ công việc và di chuyển cao cấp.', N'B'),
    (2, N'SUV hạng sang', N'Xe gầm cao phục vụ gia đình và các chuyến đi.', N'B'),
    (3, N'Xe thể thao', N'Xe thể thao dành cho trải nghiệm lái.', N'B'),
    (4, N'Siêu xe', N'Xe hiệu suất cao dành cho sự kiện và trải nghiệm.', N'B'),
    (5, N'Xe mui trần', N'Xe có thể mở mui dành cho du lịch và sự kiện.', N'B');
SET IDENTITY_INSERT dbo.loaiXe OFF;
GO

SET IDENTITY_INSERT dbo.diemGiaoNhan ON;
INSERT INTO dbo.diemGiaoNhan (idDiem, tenDiem, diaChi, trangThai)
VALUES
    (1, N'Quầy giao nhận Cầu Giấy', N'Quận Cầu Giấy, Hà Nội', 1),
    (2, N'Quầy giao nhận Thanh Xuân', N'Quận Thanh Xuân, Hà Nội', 1);
SET IDENTITY_INSERT dbo.diemGiaoNhan OFF;
GO

SET IDENTITY_INSERT dbo.chinhSachPhuPhi ON;
INSERT INTO dbo.chinhSachPhuPhi (idChinhSach, idLoaiXe, anHanPhut, hanMucKmNgay, donGiaKm)
VALUES
    (1, 1, 120, 200, 5000),
    (2, 2, 120, 200, 5000),
    (3, 3, 120, 200, 5000),
    (4, 4, 120, 200, 5000),
    (5, 5, 120, 200, 5000);
SET IDENTITY_INSERT dbo.chinhSachPhuPhi OFF;
GO

/* ===================== PHẦN 5: TÀI KHOẢN THỬ NGHIỆM (v4) ===================== */
/* Chỉ có hai tài khoản để thử nghiệm và nộp bài. Mật khẩu:
   - Quản trị viên: admin@rentcar.vn / Admin@2026
   - Khách hàng:    khach@rentcar.vn / Khach@2026
   Chuỗi băm theo ASP.NET Core Identity V3 (PBKDF2-HMACSHA256, 100 000 vòng, salt 16 byte).
   Dữ liệu giả lập, không dùng cho hệ thống thật: hãy xóa hoặc đổi mật khẩu trước khi triển khai. */
INSERT INTO dbo.nguoiDung (hoTen, email, matKhau, ngaySinh, soDienThoai, cccd, diaChi, trangThai, soLanSai, khoaDen, maDatLaiMatKhau, hanMaDatLai, idVaiTro)
VALUES
    (N'Quản trị viên thử nghiệm', 'admin@rentcar.vn', 'AQAAAAEAAYagAAAAEPFP3Bz8VZIKl8AQJlUrkL+pl4FeSTU2i4FQ1CkWqVGHjwrw54L7MtaKcEc+gVywiQ==', NULL, NULL, NULL, NULL, 1, 0, NULL, NULL, NULL, 1),
    (N'Khách hàng thử nghiệm', 'khach@rentcar.vn', 'AQAAAAEAAYagAAAAEDa+NVKPf7T4cs828Y6ySZnlh4SPAbVDs4VoCxqJL4awSpuGhIdqDNXXD1/oBCP32g==', '1995-06-15', '0900000001', NULL, N'Hà Nội', 1, 0, NULL, NULL, NULL, 5);
INSERT INTO dbo.giayPhepLaiXe (idNguoiDung, soGPLX, hang, ngayHetHan)
VALUES ((SELECT idNguoiDung FROM dbo.nguoiDung WHERE email = 'khach@rentcar.vn'), 'B2-TEST-0001', 'B', '2030-12-31');
GO

/* ===================== PHẦN 6: CÔNG VIỆC ĐỊNH KỲ (cần SQL Server Agent) =====================
   Nếu máy chủ không có Agent/msdb (Express, LocalDB, Azure SQL...), phần này chỉ in cảnh báo và không làm hỏng CSDL đã tạo.
   Khi đó hãy gọi EXEC dbo.spHuyHopDongQuaHanGiuCho; bằng công cụ lập lịch khác (ví dụ BackgroundService của ứng dụng). */
IF DB_ID(N'msdb') IS NOT NULL AND OBJECT_ID(N'msdb.dbo.sp_add_job') IS NOT NULL
BEGIN
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'RentCar_HuyHopDongQuaHan')
            EXEC msdb.dbo.sp_delete_job @job_name = N'RentCar_HuyHopDongQuaHan', @delete_unused_schedule = 1;

        EXEC msdb.dbo.sp_add_job @job_name = N'RentCar_HuyHopDongQuaHan', @enabled = 1,
             @description = N'RentCarDb: tự hủy hợp đồng Chờ xác nhận quá hạn giữ chỗ mà chưa đặt cọc (BR-15)';
        EXEC msdb.dbo.sp_add_jobstep @job_name = N'RentCar_HuyHopDongQuaHan', @step_name = N'Hủy hợp đồng quá hạn giữ chỗ',
             @subsystem = N'TSQL', @database_name = N'RentCarDb', @command = N'EXEC dbo.spHuyHopDongQuaHanGiuCho;';
        EXEC msdb.dbo.sp_add_jobschedule @job_name = N'RentCar_HuyHopDongQuaHan', @name = N'Moi 5 phut',
             @freq_type = 4, @freq_interval = 1, @freq_subday_type = 4, @freq_subday_interval = 5;
        EXEC msdb.dbo.sp_add_jobserver @job_name = N'RentCar_HuyHopDongQuaHan', @server_name = N'(local)';

        PRINT N'Đã tạo job RentCar_HuyHopDongQuaHan (chạy mỗi 5 phút, cần bật SQL Server Agent).';
    END TRY
    BEGIN CATCH
        PRINT N'CẢNH BÁO: không tạo được job SQL Server Agent: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE
    PRINT N'CẢNH BÁO: máy chủ không có SQL Server Agent/msdb, bỏ qua việc tạo job RentCar_HuyHopDongQuaHan.';
GO

PRINT N'=== Hoàn tất: RentCarDb đã được tạo (23 bảng, 2 view, 1 hàm, 1 thủ tục, các trigger nghiệp vụ). ===';
GO
