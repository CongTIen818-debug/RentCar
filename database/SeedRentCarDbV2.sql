-- Fixed datetime literals converted once from the previous UTC seed to Vietnam wall-clock time.
-- Demo data for the NEW V2 schema. Run only on an empty database.
-- The in-place data copy is a different script: MigrateRentCarDbToV2.sql.
USE RentCarDb_V2;
GO
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
IF EXISTS(SELECT 1 FROM dbo.nguoiDung) OR EXISTS(SELECT 1 FROM dbo.xe) OR EXISTS(SELECT 1 FROM dbo.vaiTro)
BEGIN
    RAISERROR(N'Database đã có dữ liệu. Dừng nạp dữ liệu demo.',16,1);
    SET NOEXEC ON;
END;
GO
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @disable nvarchar(max)=N'';
SELECT @disable+=N'DISABLE TRIGGER ALL ON dbo.'+QUOTENAME(name)+N';' FROM sys.tables;
EXEC sp_executesql @disable;
SET IDENTITY_INSERT dbo.[vaiTro] ON;
INSERT dbo.[vaiTro]([idVaiTro],[tenVaiTro],[maVaiTro]) VALUES(1,N'Quản trị viên',N'ADMIN');
INSERT dbo.[vaiTro]([idVaiTro],[tenVaiTro],[maVaiTro]) VALUES(2,N'Nhân viên kinh doanh',N'KINHDOANH');
INSERT dbo.[vaiTro]([idVaiTro],[tenVaiTro],[maVaiTro]) VALUES(3,N'Nhân viên giao nhận',N'GIAONHAN');
INSERT dbo.[vaiTro]([idVaiTro],[tenVaiTro],[maVaiTro]) VALUES(4,N'Kế toán',N'KETOAN');
INSERT dbo.[vaiTro]([idVaiTro],[tenVaiTro],[maVaiTro]) VALUES(5,N'Khách hàng',N'KHACHHANG');
SET IDENTITY_INSERT dbo.[vaiTro] OFF;
SET IDENTITY_INSERT dbo.[nguoiDung] ON;
INSERT dbo.[nguoiDung]([idNguoiDung],[hoTen],[email],[matKhau],[ngaySinh],[soDienThoai],[cccd],[diaChi],[trangThai],[soLanSai],[khoaDen],[maDatLaiMatKhau],[hanMaDatLai],[idVaiTro]) VALUES(1,N'Trần Gia Huy',N'giahuy.tran@gmail.com',N'AQAAAAIAAYagAAAAEJpcxksP0q1iJBsVeGLgMQaSsoC/Iqe1cCw57twWpPg7djBLdHBAR9bSdYx98L81yw==',N'1981-08-28',N'0001000001',N'GT0000000001',N'Ba Đình, Hà Nội',1,0,NULL,NULL,NULL,1);
INSERT dbo.[nguoiDung]([idNguoiDung],[hoTen],[email],[matKhau],[ngaySinh],[soDienThoai],[cccd],[diaChi],[trangThai],[soLanSai],[khoaDen],[maDatLaiMatKhau],[hanMaDatLai],[idVaiTro]) VALUES(2,N'Nguyễn Khánh Linh',N'khanhlinh.nguyen@gmail.com',N'AQAAAAIAAYagAAAAEMg+ZB1g6kWQgjVAhPymk4aJ8xA2dWINQWDbzfaC/QvxTNrr39LTSCcftfVC+TnUdg==',N'1983-01-06',N'0001000002',N'GT0000000002',N'Cầu Giấy, Hà Nội',0,0,NULL,NULL,NULL,2);
INSERT dbo.[nguoiDung]([idNguoiDung],[hoTen],[email],[matKhau],[ngaySinh],[soDienThoai],[cccd],[diaChi],[trangThai],[soLanSai],[khoaDen],[maDatLaiMatKhau],[hanMaDatLai],[idVaiTro]) VALUES(3,N'Lê Hoàng Phúc',N'hoangphuc.le@gmail.com',N'AQAAAAIAAYagAAAAEDr2Nr5kJwCcygzLnU660ePlZXXPmSY7k6J/a0T6Nn/MKLdlnZ0y12zTUKTNzO6dDg==',N'1995-06-01',N'0001000003',N'GT0000000003',N'Thanh Xuân, Hà Nội',0,0,NULL,NULL,NULL,3);
INSERT dbo.[nguoiDung]([idNguoiDung],[hoTen],[email],[matKhau],[ngaySinh],[soDienThoai],[cccd],[diaChi],[trangThai],[soLanSai],[khoaDen],[maDatLaiMatKhau],[hanMaDatLai],[idVaiTro]) VALUES(4,N'Phạm Ngọc Mai',N'ngocmai.pham@gmail.com',N'AQAAAAIAAYagAAAAEKklueZvViqR3sJNxB4mL7YO6G8GtNBU09WbBpaMZywzAyiJZNIPEbwEuzK1LGcn1Q==',N'1986-06-05',N'0001000004',N'GT0000000004',N'Tây Hồ, Hà Nội',0,0,NULL,NULL,NULL,4);
INSERT dbo.[nguoiDung]([idNguoiDung],[hoTen],[email],[matKhau],[ngaySinh],[soDienThoai],[cccd],[diaChi],[trangThai],[soLanSai],[khoaDen],[maDatLaiMatKhau],[hanMaDatLai],[idVaiTro]) VALUES(5,N'Vũ Minh Quân',N'minhquan.vu@gmail.com',N'AQAAAAIAAYagAAAAEHwis/bOMJxCqY1jUxP5G07iylPgdGSKESCt2w4RivO6SSz0cj0LtLXqqULVq4buGw==',N'1980-04-10',N'0001000005',N'GT0000000005',N'Long Biên, Hà Nội',1,0,NULL,NULL,NULL,5);
SET IDENTITY_INSERT dbo.[nguoiDung] OFF;
SET IDENTITY_INSERT dbo.[loaiXe] ON;
INSERT dbo.[loaiXe]([idLoaiXe],[tenLoai],[moTa],[hangGPLXToiThieu]) VALUES(1,N'Sedan hạng sang',N'Xe sedan phục vụ công việc và di chuyển cao cấp.',N'B');
INSERT dbo.[loaiXe]([idLoaiXe],[tenLoai],[moTa],[hangGPLXToiThieu]) VALUES(2,N'SUV hạng sang',N'Xe gầm cao phục vụ gia đình và các chuyến đi.',N'B');
INSERT dbo.[loaiXe]([idLoaiXe],[tenLoai],[moTa],[hangGPLXToiThieu]) VALUES(3,N'Xe thể thao',N'Xe thể thao dành cho trải nghiệm lái.',N'B');
INSERT dbo.[loaiXe]([idLoaiXe],[tenLoai],[moTa],[hangGPLXToiThieu]) VALUES(4,N'Siêu xe',N'Xe hiệu suất cao dành cho sự kiện và trải nghiệm.',N'B');
INSERT dbo.[loaiXe]([idLoaiXe],[tenLoai],[moTa],[hangGPLXToiThieu]) VALUES(5,N'Xe mui trần',N'Xe có thể mở mui dành cho du lịch và sự kiện.',N'B');
SET IDENTITY_INSERT dbo.[loaiXe] OFF;
SET IDENTITY_INSERT dbo.[xe] ON;
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(1,N'Mercedes-Benz S-Class',N'RC-0001',N'Mercedes-Benz',N'S 450',2023,N'Đen',5,N'Tự động',N'Xăng',7000000.00,42000000.00,3000000.00,4,N'Mercedes-Benz S-Class 2023, màu Đen, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/vehicles/x001-s-class-black.png',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(3,N'BMW 7 Series',N'RC-0003',N'BMW',N'740i',2024,N'Đen',5,N'Tự động',N'Xăng',8000000.00,48000000.00,3000000.00,4,N'BMW 7 Series 2024, màu Đen, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/vehicles/x003-bmw-740i-black.png',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(4,N'Range Rover',N'RC-0004',N'Land Rover',N'Autobiography',2023,N'Trắng',5,N'Tự động',N'Xăng',10000000.00,60000000.00,5000000.00,4,N'Range Rover 2023, màu Trắng, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/vehicles/x004-range-rover-white.webp',2);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(6,N'Porsche 911',N'RC-0006',N'Porsche',N'Carrera',2023,N'Đỏ',4,N'Tự động',N'Xăng',12000000.00,72000000.00,5000000.00,4,N'Porsche 911 2023, màu Đỏ, 4 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/vehicles/x006-911-red.png',3);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(10,N'BMW Z4',N'RC-0010',N'BMW',N'sDrive30i',2023,N'Xanh',2,N'Tự động',N'Xăng',9000000.00,54000000.00,3000000.00,4,N'BMW Z4 2023, màu Xanh, 2 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/vehicles/x010-z4-blue.png',5);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(11,N'Rolls-Royce Ghost',N'RC-REF-01',N'Rolls-Royce',N'Tiêu chuẩn',2025,N'Xám',4,N'Tự động',N'Xăng',129000000.00,774000000.00,5000000.00,1,N'Rolls-Royce Ghost 2025, màu Xám, 4 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/richcars-rolls-royce-ghost-black-cover-768x768.webp',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(12,N'Ferrari 488 GTB',N'RC-REF-02',N'Ferrari',N'Tiêu chuẩn',2022,N'Đỏ',2,N'Tự động',N'Xăng',99000000.00,594000000.00,5000000.00,1,N'Ferrari 488 GTB 2022, màu Đỏ, 2 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/Ferrari-488-GTB_1-768x768.webp',4);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(13,N'Lamborghini Huracan',N'RC-REF-03',N'Lamborghini',N'Tiêu chuẩn',2020,N'Xanh dương',2,N'Tự động',N'Xăng',89900000.00,539400000.00,5000000.00,1,N'Lamborghini Huracan 2020, màu Xanh dương, 2 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/Huracan-2019-768x768.webp',4);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(14,N'Porsche 911 Carrera',N'RC-REF-04',N'Porsche',N'Tiêu chuẩn',2022,N'Vàng',4,N'Tự động',N'Xăng',39900000.00,239400000.00,5000000.00,1,N'Porsche 911 Carrera 2022, màu Vàng, 4 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/911-768x768.jpg',3);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(15,N'Bentley Mulsanne',N'RC-REF-05',N'Bentley',N'Tiêu chuẩn',2020,N'Xanh lá',5,N'Tự động',N'Xăng',39900000.00,239400000.00,5000000.00,1,N'Bentley Mulsanne 2020, màu Xanh lá, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/richcars-bentley-mulsanne-black-cover-768x768.webp',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(16,N'Porsche Panamera',N'RC-REF-06',N'Porsche',N'Tiêu chuẩn',2022,N'Đen',4,N'Tự động',N'Xăng',29900000.00,179400000.00,5000000.00,1,N'Porsche Panamera 2022, màu Đen, 4 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/PANAMERA-VN-768x768.jpg',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(17,N'Land Cruiser',N'RC-REF-07',N'Toyota',N'Tiêu chuẩn',2026,N'Trắng',7,N'Tự động',N'Xăng',29900000.00,179400000.00,5000000.00,1,N'Land Cruiser 2026, màu Trắng, 7 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/Land-Cruiser-768x768.jpg',2);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(18,N'Mercedes Benz S450',N'RC-REF-08',N'Mercedes-Benz',N'Tiêu chuẩn',2022,N'Bạc',5,N'Tự động',N'Xăng',22900000.00,137400000.00,5000000.00,1,N'Mercedes Benz S450 2022, màu Bạc, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/S450-768x768.jpg',1);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(19,N'Porsche 718 Boxster',N'RC-REF-09',N'Porsche',N'Tiêu chuẩn',2023,N'Xám',2,N'Tự động',N'Xăng',19900000.00,119400000.00,5000000.00,1,N'Porsche 718 Boxster 2023, màu Xám, 2 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/Porsche-718-Boxster-768x768.webp',5);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(20,N'Audi Q8',N'RC-REF-10',N'Audi',N'Tiêu chuẩn',2026,N'Đỏ',5,N'Tự động',N'Xăng',19900000.00,119400000.00,5000000.00,1,N'Audi Q8 2026, màu Đỏ, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/AUDI-Q8-768x768.jpg',2);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(21,N'Porsche Macan',N'RC-REF-11',N'Porsche',N'Tiêu chuẩn',2020,N'Xanh dương',5,N'Tự động',N'Xăng',13900000.00,83400000.00,5000000.00,1,N'Porsche Macan 2020, màu Xanh dương, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/richcars-porsche-macan-silver-cover-768x768.webp',2);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(22,N'BMW Z4',N'RC-REF-12',N'BMW',N'Tiêu chuẩn',2020,N'Vàng',2,N'Tự động',N'Xăng',12000000.00,72000000.00,5000000.00,1,N'BMW Z4 2020, màu Vàng, 2 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/Z4-768x768.jpg',5);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(23,N'Lexus RX350 Premium',N'RC-REF-13',N'Lexus',N'Tiêu chuẩn',2019,N'Xanh lá',5,N'Tự động',N'Xăng',12000000.00,72000000.00,5000000.00,1,N'Lexus RX350 Premium 2019, màu Xanh lá, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/richcars-lexus-rx350-premium-thumbnail-768x768.webp',2);
INSERT dbo.[xe]([idXe],[tenXe],[bienSo],[hangXe],[phienBan],[namSanXuat],[mauSac],[soCho],[hopSo],[nhienLieu],[giaThueNgay],[giaThueTuan],[tienDatCoc],[trangThai],[moTa],[hinhAnhDaiDien],[idLoaiXe]) VALUES(24,N'Mercedes Benz C300',N'RC-REF-14',N'Mercedes-Benz',N'Tiêu chuẩn',2026,N'Đen',5,N'Tự động',N'Xăng',5900000.00,35400000.00,3000000.00,1,N'Mercedes Benz C300 2026, màu Đen, 5 chỗ ngồi. Hộp số tự động, sử dụng xăng. Phù hợp cho hành trình cá nhân, công tác hoặc các dịp đặc biệt.',N'/images/catalog/C300-1-768x768.webp',1);
SET IDENTITY_INSERT dbo.[xe] OFF;
SET IDENTITY_INSERT dbo.[hinhAnhXe] ON;
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(1,N'/images/vehicles/x001-s-class-black.png',1);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(3,N'/images/vehicles/x003-bmw-740i-black.png',3);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(4,N'/images/vehicles/x004-range-rover-white.webp',4);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(6,N'/images/vehicles/x006-911-red.png',6);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(10,N'/images/vehicles/x010-z4-blue.png',10);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(11,N'/images/catalog/richcars-rolls-royce-ghost-black-cover-768x768.webp',11);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(12,N'/images/catalog/Ferrari-488-GTB_1-768x768.webp',12);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(13,N'/images/catalog/Huracan-2019-768x768.webp',13);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(14,N'/images/catalog/911-768x768.jpg',14);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(15,N'/images/catalog/richcars-bentley-mulsanne-black-cover-768x768.webp',15);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(16,N'/images/catalog/PANAMERA-VN-768x768.jpg',16);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(17,N'/images/catalog/Land-Cruiser-768x768.jpg',17);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(18,N'/images/catalog/S450-768x768.jpg',18);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(19,N'/images/catalog/Porsche-718-Boxster-768x768.webp',19);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(20,N'/images/catalog/AUDI-Q8-768x768.jpg',20);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(21,N'/images/catalog/richcars-porsche-macan-silver-cover-768x768.webp',21);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(22,N'/images/catalog/Z4-768x768.jpg',22);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(23,N'/images/catalog/richcars-lexus-rx350-premium-thumbnail-768x768.webp',23);
INSERT dbo.[hinhAnhXe]([idHinhAnhXe],[duongDanAnh],[idXe]) VALUES(24,N'/images/catalog/C300-1-768x768.webp',24);
SET IDENTITY_INSERT dbo.[hinhAnhXe] OFF;
SET IDENTITY_INSERT dbo.[diemGiaoNhan] ON;
INSERT dbo.[diemGiaoNhan]([idDiem],[tenDiem],[diaChi],[trangThai]) VALUES(1,N'Chưa xác định - hợp đồng cũ',N'Chưa có thông tin địa điểm trong dữ liệu cũ',1);
SET IDENTITY_INSERT dbo.[diemGiaoNhan] OFF;
SET IDENTITY_INSERT dbo.[uuDai] ON;
INSERT dbo.[uuDai]([idUuDai],[maUuDai],[tenUuDai],[moTa],[phanTramGiam],[ngayBatDau],[ngayKetThuc],[trangThai]) VALUES(1,N'UD-CU-1',N'Ưu đãi S-Class',N'Giảm 10 phần trăm giá thuê ngày.',10.00,N'2026-11-19T07:00:00',N'2026-12-20T07:00:00',1);
INSERT dbo.[uuDai]([idUuDai],[maUuDai],[tenUuDai],[moTa],[phanTramGiam],[ngayBatDau],[ngayKetThuc],[trangThai]) VALUES(2,N'UD-CU-2',N'Trải nghiệm BMW',N'Giảm 800.000 đồng trên mỗi ngày thuê.',10.00,N'2026-11-19T07:00:00',N'2026-12-20T07:00:00',1);
INSERT dbo.[uuDai]([idUuDai],[maUuDai],[tenUuDai],[moTa],[phanTramGiam],[ngayBatDau],[ngayKetThuc],[trangThai]) VALUES(3,N'UD-CU-3',N'Hành trình SUV',N'Giảm 15 phần trăm giá thuê ngày.',15.00,N'2026-11-19T07:00:00',N'2026-12-20T07:00:00',1);
INSERT dbo.[uuDai]([idUuDai],[maUuDai],[tenUuDai],[moTa],[phanTramGiam],[ngayBatDau],[ngayKetThuc],[trangThai]) VALUES(4,N'UD-CU-4',N'Cảm hứng thể thao',N'Giảm 1.200.000 đồng trên mỗi ngày thuê.',10.00,N'2026-11-19T07:00:00',N'2026-12-20T07:00:00',1);
INSERT dbo.[uuDai]([idUuDai],[maUuDai],[tenUuDai],[moTa],[phanTramGiam],[ngayBatDau],[ngayKetThuc],[trangThai]) VALUES(5,N'UD-CU-5',N'Chuyến đi mui trần',N'Giảm 10 phần trăm giá thuê ngày.',10.00,N'2026-11-19T07:00:00',N'2026-12-20T07:00:00',1);
SET IDENTITY_INSERT dbo.[uuDai] OFF;
INSERT dbo.[uuDaiXe]([idUuDai],[idXe]) VALUES(1,1);
INSERT dbo.[uuDaiXe]([idUuDai],[idXe]) VALUES(2,3);
INSERT dbo.[uuDaiXe]([idUuDai],[idXe]) VALUES(3,4);
INSERT dbo.[uuDaiXe]([idUuDai],[idXe]) VALUES(4,6);
INSERT dbo.[uuDaiXe]([idUuDai],[idXe]) VALUES(5,10);
SET IDENTITY_INSERT dbo.[hopDong] ON;
INSERT dbo.[hopDong]([idHopDong],[maHopDong],[ngayTao],[ngayBatDauThue],[ngayKetThucThue],[tongTienGoc],[idUuDai],[tenUuDaiApDung],[phanTramApDung],[tienGiamGia],[tienDatCoc],[trangThai],[lyDoHuy],[hanGiuCho],[idNguoiDung],[idNguoiTao],[idYeuCau],[idDiemNhan],[idDiemTra]) VALUES(1,N'HD001',N'2026-11-20T20:23:10',N'2026-11-22T07:00:00',N'2026-11-24T07:00:00',14000000.00,1,N'Ưu đãi S-Class',10.00,1400000.00,5000000.00,1,NULL,NULL,5,NULL,NULL,1,1);
INSERT dbo.[hopDong]([idHopDong],[maHopDong],[ngayTao],[ngayBatDauThue],[ngayKetThucThue],[tongTienGoc],[idUuDai],[tenUuDaiApDung],[phanTramApDung],[tienGiamGia],[tienDatCoc],[trangThai],[lyDoHuy],[hanGiuCho],[idNguoiDung],[idNguoiTao],[idYeuCau],[idDiemNhan],[idDiemTra]) VALUES(2,N'HD002',N'2026-11-20T20:23:10',N'2026-11-25T07:00:00',N'2026-11-28T07:00:00',24000000.00,2,N'Trải nghiệm BMW',10.00,2400000.00,7000000.00,1,NULL,NULL,5,NULL,NULL,1,1);
INSERT dbo.[hopDong]([idHopDong],[maHopDong],[ngayTao],[ngayBatDauThue],[ngayKetThucThue],[tongTienGoc],[idUuDai],[tenUuDaiApDung],[phanTramApDung],[tienGiamGia],[tienDatCoc],[trangThai],[lyDoHuy],[hanGiuCho],[idNguoiDung],[idNguoiTao],[idYeuCau],[idDiemNhan],[idDiemTra]) VALUES(3,N'HD003',N'2026-11-20T20:23:10',N'2026-11-30T07:00:00',N'2026-12-02T07:00:00',20000000.00,3,N'Hành trình SUV',15.00,3000000.00,6000000.00,1,NULL,NULL,5,NULL,NULL,1,1);
INSERT dbo.[hopDong]([idHopDong],[maHopDong],[ngayTao],[ngayBatDauThue],[ngayKetThucThue],[tongTienGoc],[idUuDai],[tenUuDaiApDung],[phanTramApDung],[tienGiamGia],[tienDatCoc],[trangThai],[lyDoHuy],[hanGiuCho],[idNguoiDung],[idNguoiTao],[idYeuCau],[idDiemNhan],[idDiemTra]) VALUES(4,N'HD004',N'2026-11-20T20:23:10',N'2026-12-05T07:00:00',N'2026-12-06T07:00:00',12000000.00,4,N'Cảm hứng thể thao',10.00,1200000.00,5000000.00,1,NULL,NULL,5,NULL,NULL,1,1);
INSERT dbo.[hopDong]([idHopDong],[maHopDong],[ngayTao],[ngayBatDauThue],[ngayKetThucThue],[tongTienGoc],[idUuDai],[tenUuDaiApDung],[phanTramApDung],[tienGiamGia],[tienDatCoc],[trangThai],[lyDoHuy],[hanGiuCho],[idNguoiDung],[idNguoiTao],[idYeuCau],[idDiemNhan],[idDiemTra]) VALUES(5,N'HD005',N'2026-11-20T20:23:10',N'2026-12-10T07:00:00',N'2026-12-12T07:00:00',18000000.00,5,N'Chuyến đi mui trần',10.00,1800000.00,5000000.00,1,NULL,NULL,5,NULL,NULL,1,1);
SET IDENTITY_INSERT dbo.[hopDong] OFF;
SET IDENTITY_INSERT dbo.[chiTietHopDong] ON;
INSERT dbo.[chiTietHopDong]([idChiTietHopDong],[idHopDong],[idXe],[giaThueNgayTaiThoiDiem],[giaThueTuanTaiThoiDiem],[soNgayThue],[soTuanThue],[coTaiXe],[phiTaiXeNgay]) VALUES(1,1,1,7000000.00,42000000.00,2,0,0,0.00);
INSERT dbo.[chiTietHopDong]([idChiTietHopDong],[idHopDong],[idXe],[giaThueNgayTaiThoiDiem],[giaThueTuanTaiThoiDiem],[soNgayThue],[soTuanThue],[coTaiXe],[phiTaiXeNgay]) VALUES(2,2,3,8000000.00,48000000.00,3,0,0,0.00);
INSERT dbo.[chiTietHopDong]([idChiTietHopDong],[idHopDong],[idXe],[giaThueNgayTaiThoiDiem],[giaThueTuanTaiThoiDiem],[soNgayThue],[soTuanThue],[coTaiXe],[phiTaiXeNgay]) VALUES(3,3,4,10000000.00,60000000.00,2,0,0,0.00);
INSERT dbo.[chiTietHopDong]([idChiTietHopDong],[idHopDong],[idXe],[giaThueNgayTaiThoiDiem],[giaThueTuanTaiThoiDiem],[soNgayThue],[soTuanThue],[coTaiXe],[phiTaiXeNgay]) VALUES(4,4,6,12000000.00,72000000.00,1,0,0,0.00);
INSERT dbo.[chiTietHopDong]([idChiTietHopDong],[idHopDong],[idXe],[giaThueNgayTaiThoiDiem],[giaThueTuanTaiThoiDiem],[soNgayThue],[soTuanThue],[coTaiXe],[phiTaiXeNgay]) VALUES(5,5,10,9000000.00,54000000.00,2,0,0,0.00);
SET IDENTITY_INSERT dbo.[chiTietHopDong] OFF;
SET IDENTITY_INSERT dbo.[bienBanBanGiao] ON;
INSERT dbo.[bienBanBanGiao]([idBienBanBanGiao],[idHopDong],[ngayBanGiao],[soKmGiao],[mucNhienLieuGiao],[tinhTrangLucGiao],[phuKienGiao],[ngayNhanLai],[soKmNhan],[mucNhienLieuNhan],[tinhTrangLucNhan],[phuKienNhan],[chiPhiPhatSinh]) VALUES(1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0.00);
INSERT dbo.[bienBanBanGiao]([idBienBanBanGiao],[idHopDong],[ngayBanGiao],[soKmGiao],[mucNhienLieuGiao],[tinhTrangLucGiao],[phuKienGiao],[ngayNhanLai],[soKmNhan],[mucNhienLieuNhan],[tinhTrangLucNhan],[phuKienNhan],[chiPhiPhatSinh]) VALUES(2,2,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0.00);
INSERT dbo.[bienBanBanGiao]([idBienBanBanGiao],[idHopDong],[ngayBanGiao],[soKmGiao],[mucNhienLieuGiao],[tinhTrangLucGiao],[phuKienGiao],[ngayNhanLai],[soKmNhan],[mucNhienLieuNhan],[tinhTrangLucNhan],[phuKienNhan],[chiPhiPhatSinh]) VALUES(3,3,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0.00);
INSERT dbo.[bienBanBanGiao]([idBienBanBanGiao],[idHopDong],[ngayBanGiao],[soKmGiao],[mucNhienLieuGiao],[tinhTrangLucGiao],[phuKienGiao],[ngayNhanLai],[soKmNhan],[mucNhienLieuNhan],[tinhTrangLucNhan],[phuKienNhan],[chiPhiPhatSinh]) VALUES(4,4,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0.00);
INSERT dbo.[bienBanBanGiao]([idBienBanBanGiao],[idHopDong],[ngayBanGiao],[soKmGiao],[mucNhienLieuGiao],[tinhTrangLucGiao],[phuKienGiao],[ngayNhanLai],[soKmNhan],[mucNhienLieuNhan],[tinhTrangLucNhan],[phuKienNhan],[chiPhiPhatSinh]) VALUES(5,5,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0.00);
SET IDENTITY_INSERT dbo.[bienBanBanGiao] OFF;
DECLARE @enable nvarchar(max)=N'';
SELECT @enable+=N'ENABLE TRIGGER ALL ON dbo.'+QUOTENAME(name)+N';' FROM sys.tables;
EXEC sp_executesql @enable;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
THROW;
END CATCH;
GO

GO
SET NOEXEC OFF;
GO
