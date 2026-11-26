-- LEGACY: only for the old RentCarDb schema (bit vehicle status, khuyenMai tables).
-- For RentCarDb_V2 use CreateRentCarDbV2.sql and SeedRentCarDbV2.sql.
-- Source: https://richcars.vn/vi/richcars-dich-vu-thue-xe-sang-uy-tin-lau-doi/
-- Reference prices, synthetic plates. Retain vehicles referenced by historical contracts.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
DECLARE @Old TABLE (Id int PRIMARY KEY);
INSERT @Old SELECT idXe FROM dbo.xe WHERE bienSo NOT LIKE 'RC-REF-%';
UPDATE dbo.xe SET trangThai=0 WHERE idXe IN (SELECT Id FROM @Old);
DELETE FROM dbo.khuyenMaiXe WHERE idXe IN (SELECT Id FROM @Old) AND idXe NOT IN (SELECT idXe FROM dbo.chiTietHopDong);
DELETE FROM dbo.hinhAnhXe WHERE idXe IN (SELECT Id FROM @Old) AND idXe NOT IN (SELECT idXe FROM dbo.chiTietHopDong);
DELETE FROM dbo.xe WHERE idXe IN (SELECT Id FROM @Old) AND idXe NOT IN (SELECT idXe FROM dbo.chiTietHopDong);
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-01')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Rolls-Royce Ghost','RC-REF-01',N'Rolls-Royce',129000000,1,'/images/catalog/richcars-rolls-royce-ghost-black-cover-768x768.webp',1,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-01' AND h.duongDanAnh='/images/catalog/richcars-rolls-royce-ghost-black-cover-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/richcars-rolls-royce-ghost-black-cover-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-01';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-02')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Ferrari 488 GTB','RC-REF-02',N'Ferrari',99000000,1,'/images/catalog/Ferrari-488-GTB_1-768x768.webp',4,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-02' AND h.duongDanAnh='/images/catalog/Ferrari-488-GTB_1-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/Ferrari-488-GTB_1-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-02';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-03')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Lamborghini Huracan','RC-REF-03',N'Lamborghini',89900000,1,'/images/catalog/Huracan-2019-768x768.webp',4,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-03' AND h.duongDanAnh='/images/catalog/Huracan-2019-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/Huracan-2019-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-03';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-04')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Porsche 911 Carrera','RC-REF-04',N'Porsche',39900000,1,'/images/catalog/911-768x768.jpg',3,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-04' AND h.duongDanAnh='/images/catalog/911-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/911-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-04';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-05')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Bentley Mulsanne','RC-REF-05',N'Bentley',39900000,1,'/images/catalog/richcars-bentley-mulsanne-black-cover-768x768.webp',1,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-05' AND h.duongDanAnh='/images/catalog/richcars-bentley-mulsanne-black-cover-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/richcars-bentley-mulsanne-black-cover-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-05';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-06')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Porsche Panamera','RC-REF-06',N'Porsche',29900000,1,'/images/catalog/PANAMERA-VN-768x768.jpg',1,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-06' AND h.duongDanAnh='/images/catalog/PANAMERA-VN-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/PANAMERA-VN-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-06';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-07')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Land Cruiser','RC-REF-07',N'Toyota',29900000,1,'/images/catalog/Land-Cruiser-768x768.jpg',2,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-07' AND h.duongDanAnh='/images/catalog/Land-Cruiser-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/Land-Cruiser-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-07';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-08')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Mercedes Benz S450','RC-REF-08',N'Mercedes-Benz',22900000,1,'/images/catalog/S450-768x768.jpg',1,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-08' AND h.duongDanAnh='/images/catalog/S450-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/S450-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-08';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-09')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Porsche 718 Boxster','RC-REF-09',N'Porsche',19900000,1,'/images/catalog/Porsche-718-Boxster-768x768.webp',5,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-09' AND h.duongDanAnh='/images/catalog/Porsche-718-Boxster-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/Porsche-718-Boxster-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-09';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-10')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Audi Q8','RC-REF-10',N'Audi',19900000,1,'/images/catalog/AUDI-Q8-768x768.jpg',2,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-10' AND h.duongDanAnh='/images/catalog/AUDI-Q8-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/AUDI-Q8-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-10';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-11')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Porsche Macan','RC-REF-11',N'Porsche',13900000,1,'/images/catalog/richcars-porsche-macan-silver-cover-768x768.webp',2,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-11' AND h.duongDanAnh='/images/catalog/richcars-porsche-macan-silver-cover-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/richcars-porsche-macan-silver-cover-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-11';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-12')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'BMW Z4','RC-REF-12',N'BMW',12000000,1,'/images/catalog/Z4-768x768.jpg',5,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-12' AND h.duongDanAnh='/images/catalog/Z4-768x768.jpg')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/Z4-768x768.jpg' FROM dbo.xe WHERE bienSo='RC-REF-12';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-13')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Lexus RX350 Premium','RC-REF-13',N'Lexus',12000000,1,'/images/catalog/richcars-lexus-rx350-premium-thumbnail-768x768.webp',2,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-13' AND h.duongDanAnh='/images/catalog/richcars-lexus-rx350-premium-thumbnail-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/richcars-lexus-rx350-premium-thumbnail-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-13';
IF NOT EXISTS (SELECT 1 FROM dbo.xe WHERE bienSo='RC-REF-14')
INSERT dbo.xe (tenXe,bienSo,hangXe,giaThueNgay,trangThai,hinhAnhDaiDien,idLoaiXe,phienBan) VALUES (N'Mercedes Benz C300','RC-REF-14',N'Mercedes-Benz',5900000,1,'/images/catalog/C300-1-768x768.webp',1,N'');
IF NOT EXISTS (SELECT 1 FROM dbo.hinhAnhXe h JOIN dbo.xe x ON x.idXe=h.idXe WHERE x.bienSo='RC-REF-14' AND h.duongDanAnh='/images/catalog/C300-1-768x768.webp')
INSERT dbo.hinhAnhXe (idXe,duongDanAnh) SELECT idXe,'/images/catalog/C300-1-768x768.webp' FROM dbo.xe WHERE bienSo='RC-REF-14';
COMMIT TRANSACTION;