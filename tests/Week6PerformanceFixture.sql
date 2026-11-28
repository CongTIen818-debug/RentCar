USE RentCar_Week6_Verify_20261128;
SET XACT_ABORT ON; BEGIN TRANSACTION;
IF DB_NAME() NOT LIKE 'RentCar_Week6_Verify_%' THROW 51000,'Only isolated verification databases are allowed',1;
INSERT nguoiDung(hoTen,email,matKhau,ngaySinh,trangThai,idVaiTro) SELECT N'Khách kiểm tra hiệu năng','week6-perf@rentcar.test',matKhau,'1985-01-01',1,5 FROM nguoiDung WHERE idNguoiDung=5;
DECLARE @customer int=SCOPE_IDENTITY(),@cars int=1000-(SELECT COUNT(*) FROM xe),@contracts int=20000-(SELECT COUNT(*) FROM hopDong);
WITH nums AS(SELECT TOP(@cars) ROW_NUMBER() OVER(ORDER BY a.object_id,b.object_id) n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT xe(tenXe,bienSo,hangXe,namSanXuat,giaThueNgay,giaThueTuan,tienDatCoc,trangThai,idLoaiXe,soCho,hopSo,nhienLieu)
SELECT CONCAT(N'Xe kiểm tra ',n),CONCAT('LOAD-',n),N'Mercedes-Benz',2023,1000000,6000000,2000000,1,1,5,N'Tự động',N'Xăng' FROM nums;
WITH nums AS(SELECT TOP(@contracts) ROW_NUMBER() OVER(ORDER BY a.object_id,b.object_id) n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT hopDong(maHopDong,ngayTao,ngayBatDauThue,ngayKetThucThue,tongTienGoc,tienDatCoc,trangThai,idNguoiDung,idNguoiTao,idDiemNhan,idDiemTra,thueSuatVat)
SELECT CONCAT('LOAD-HD-',n),'2020-01-01',DATEADD(day,n,'1960-01-01'),DATEADD(day,n+1,'1960-01-01'),1000000,2000000,3,@customer,1,1,1,0 FROM nums;
INSERT chiTietHopDong(idHopDong,idXe,giaThueNgayTaiThoiDiem,giaThueTuanTaiThoiDiem,soNgayThue,soTuanThue,coTaiXe,phiTaiXeNgay)
SELECT idHopDong,11,1000000,6000000,1,0,0,0 FROM hopDong WHERE maHopDong LIKE 'LOAD-HD-%';
COMMIT;
SELECT (SELECT COUNT(*) FROM xe) Vehicles,(SELECT COUNT(*) FROM hopDong) Contracts;

