-- RectCar: ban mau theo SRS 1.2, muc 5 va Hinh B.3.
-- Chay toan bo script trong SSMS tren SQL Server.
USE master;
GO
IF DB_ID(N'RentCarDB1') IS NULL
    CREATE DATABASE RentCarDB1;
GO
USE RentCarDB1;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
IF EXISTS (SELECT 1 FROM sys.tables)
    THROW 50001, N'Cơ sở dữ liệu mẫu phải trống.', 1;
BEGIN TRANSACTION;

CREATE TABLE dbo.Vehicle (
    Id int IDENTITY(1,1) PRIMARY KEY,
    PlateNumber nvarchar(20) NOT NULL UNIQUE,
    Brand nvarchar(80) NOT NULL,
    Model nvarchar(80) NOT NULL,
    VehicleType nvarchar(50) NOT NULL,
    Seats tinyint NOT NULL CHECK (Seats > 0),
    Transmission nvarchar(30) NOT NULL,
    FuelType nvarchar(30) NOT NULL,
    ManufactureYear smallint NOT NULL,
    DailyRate decimal(18,2) NOT NULL CHECK (DailyRate >= 0),
    WeeklyRate decimal(18,2) NOT NULL CHECK (WeeklyRate >= 0),
    DepositAmount decimal(18,2) NOT NULL CHECK (DepositAmount >= 0),
    Status nvarchar(30) NOT NULL
);

CREATE TABLE dbo.VehicleImage (
    Id int IDENTITY(1,1) PRIMARY KEY,
    VehicleId int NOT NULL REFERENCES dbo.Vehicle(Id),
    ImageUrl nvarchar(2048) NOT NULL,
    IsPrimary bit NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Customer (
    Id int IDENTITY(1,1) PRIMARY KEY,
    FullName nvarchar(150) NOT NULL,
    DateOfBirth date NOT NULL,
    Phone nvarchar(25) NOT NULL,
    Email nvarchar(254) NOT NULL,
    IdentityNumber nvarchar(30) NOT NULL,
    Address nvarchar(300) NULL
);

CREATE TABLE dbo.DriverLicense (
    Id int IDENTITY(1,1) PRIMARY KEY,
    CustomerId int NOT NULL UNIQUE REFERENCES dbo.Customer(Id),
    LicenseNumber nvarchar(30) NOT NULL,
    LicenseClass nvarchar(10) NOT NULL,
    ExpiryDate date NOT NULL
);

CREATE TABLE dbo.AppUser (
    Id int IDENTITY(1,1) PRIMARY KEY,
    CustomerId int NULL REFERENCES dbo.Customer(Id),
    FullName nvarchar(150) NOT NULL,
    Email nvarchar(254) NOT NULL UNIQUE,
    PasswordHash nvarchar(512) NULL,
    Role nvarchar(40) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    FailedLoginCount int NOT NULL DEFAULT 0,
    LockedUntil datetime2(0) NULL,
    ResetTokenHash nvarchar(512) NULL,
    ResetTokenExpiresAt datetime2(0) NULL
);

CREATE TABLE dbo.PickupLocation (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(120) NOT NULL,
    Address nvarchar(300) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1
);

CREATE TABLE dbo.RentalRequest (
    Id int IDENTITY(1,1) PRIMARY KEY,
    CustomerId int NOT NULL REFERENCES dbo.Customer(Id),
    VehicleId int NOT NULL REFERENCES dbo.Vehicle(Id),
    StartAt datetime2(0) NOT NULL,
    EndAt datetime2(0) NOT NULL,
    PickupLocationId int NOT NULL REFERENCES dbo.PickupLocation(Id),
    ReturnLocationId int NOT NULL REFERENCES dbo.PickupLocation(Id),
    CreatedBy int NOT NULL REFERENCES dbo.AppUser(Id),
    CreatedAt datetime2(0) NOT NULL,
    ExpiresAt datetime2(0) NOT NULL,
    Status nvarchar(30) NOT NULL,
    CHECK (EndAt > StartAt),
    UNIQUE (Id, CustomerId, VehicleId)
);

CREATE TABLE dbo.RentalContract (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ContractCode nvarchar(40) NOT NULL UNIQUE,
    RentalRequestId int NOT NULL UNIQUE,
    CustomerId int NOT NULL REFERENCES dbo.Customer(Id),
    VehicleId int NOT NULL REFERENCES dbo.Vehicle(Id),
    StartAt datetime2(0) NOT NULL,
    EndAt datetime2(0) NOT NULL,
    ActualReturnAt datetime2(0) NULL,
    RentalPrice decimal(18,2) NOT NULL CHECK (RentalPrice >= 0),
    DiscountAmount decimal(18,2) NOT NULL,
    DepositAmount decimal(18,2) NOT NULL CHECK (DepositAmount >= 0),
    KmLimit int NOT NULL CHECK (KmLimit >= 0),
    Status nvarchar(30) NOT NULL,
    CancelReason nvarchar(500) NULL,
    ExpiresAt datetime2(0) NULL,
    CONSTRAINT CK_RentalContract_DiscountAmount CHECK (DiscountAmount BETWEEN 0 AND RentalPrice),
    CHECK (EndAt > StartAt),
    FOREIGN KEY (RentalRequestId, CustomerId, VehicleId) REFERENCES dbo.RentalRequest(Id, CustomerId, VehicleId)
);

CREATE TABLE dbo.HandoverRecord (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ContractId int NOT NULL REFERENCES dbo.RentalContract(Id),
    RecordType nvarchar(20) NOT NULL CHECK (RecordType IN (N'Bàn giao', N'Nhận lại')),
    RecordedAt datetime2(0) NOT NULL,
    RecordedBy int NOT NULL REFERENCES dbo.AppUser(Id),
    Odometer int NOT NULL CHECK (Odometer >= 0),
    FuelLevel tinyint NOT NULL CHECK (FuelLevel <= 100),
    Accessories nvarchar(500) NULL,
    ConditionNote nvarchar(1000) NULL,
    UNIQUE (ContractId, RecordType)
);

CREATE TABLE dbo.HandoverImage (
    Id int IDENTITY(1,1) PRIMARY KEY,
    HandoverId int NOT NULL REFERENCES dbo.HandoverRecord(Id),
    ImageUrl nvarchar(2048) NOT NULL
);

CREATE TABLE dbo.PaymentTransaction (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ContractId int NOT NULL REFERENCES dbo.RentalContract(Id),
    Amount decimal(18,2) NOT NULL CHECK (Amount > 0),
    PaymentType nvarchar(20) NOT NULL CHECK (PaymentType IN (N'Đặt cọc', N'Tiền thuê', N'Phụ phí', N'Hoàn cọc')),
    Method nvarchar(30) NOT NULL,
    TransactionCode nvarchar(100) NULL,
    CreatedAt datetime2(0) NOT NULL,
    CreatedBy int NOT NULL REFERENCES dbo.AppUser(Id)
);

CREATE TABLE dbo.MaintenanceRecord (
    Id int IDENTITY(1,1) PRIMARY KEY,
    VehicleId int NOT NULL REFERENCES dbo.Vehicle(Id),
    MaintenanceType nvarchar(50) NOT NULL,
    Description nvarchar(1000) NULL,
    StartAt datetime2(0) NOT NULL,
    EndAt datetime2(0) NOT NULL,
    Cost decimal(18,2) NOT NULL CHECK (Cost >= 0),
    Status nvarchar(30) NOT NULL,
    CHECK (EndAt > StartAt)
);

CREATE TABLE dbo.ContractHistory (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ContractId int NOT NULL REFERENCES dbo.RentalContract(Id),
    OldStatus nvarchar(30) NULL,
    NewStatus nvarchar(30) NOT NULL,
    ChangedAt datetime2(0) NOT NULL,
    ChangedBy int NOT NULL REFERENCES dbo.AppUser(Id),
    Note nvarchar(500) NULL
);

CREATE TABLE dbo.PriceRule (
    Id int IDENTITY(1,1) PRIMARY KEY,
    VehicleType nvarchar(50) NOT NULL,
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    Multiplier decimal(9,4) NOT NULL CHECK (Multiplier > 0),
    CHECK (EndDate >= StartDate)
);

CREATE TABLE dbo.Promotion (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(40) NOT NULL UNIQUE,
    DiscountPercent decimal(5,2) NOT NULL CHECK (DiscountPercent BETWEEN 0 AND 100),
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CHECK (EndDate >= StartDate)
);

CREATE TABLE dbo.SurchargePolicy (
    Id int IDENTITY(1,1) PRIMARY KEY,
    VehicleType nvarchar(50) NOT NULL UNIQUE,
    GraceMinutes int NOT NULL CHECK (GraceMinutes >= 0),
    KmLimitPerDay int NOT NULL CHECK (KmLimitPerDay >= 0),
    ExtraKmRate decimal(18,2) NOT NULL CHECK (ExtraKmRate >= 0)
);

CREATE TABLE dbo.Notification (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL REFERENCES dbo.AppUser(Id),
    ContractId int NULL REFERENCES dbo.RentalContract(Id),
    Type nvarchar(40) NOT NULL,
    Content nvarchar(max) NOT NULL,
    SentAt datetime2(0) NULL,
    Mode nvarchar(20) NOT NULL CHECK (Mode IN (N'Gửi thật', N'Mô phỏng'))
);

CREATE TABLE dbo.AuditLog (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ObjectType nvarchar(60) NOT NULL,
    ObjectId int NOT NULL,
    Action nvarchar(40) NOT NULL,
    OldValue nvarchar(max) NULL,
    NewValue nvarchar(max) NULL,
    UserId int NOT NULL REFERENCES dbo.AppUser(Id),
    CreatedAt datetime2(0) NOT NULL
);

CREATE UNIQUE INDEX UX_AppUser_Customer ON dbo.AppUser(CustomerId) WHERE CustomerId IS NOT NULL;
CREATE UNIQUE INDEX UX_VehicleImage_Primary ON dbo.VehicleImage(VehicleId) WHERE IsPrimary = 1;
CREATE INDEX IX_RentalRequest_Schedule ON dbo.RentalRequest(VehicleId, StartAt, EndAt) INCLUDE (Status, ExpiresAt);
CREATE INDEX IX_RentalContract_Schedule ON dbo.RentalContract(VehicleId, StartAt, EndAt) INCLUDE (Status, ExpiresAt, ActualReturnAt);
CREATE INDEX IX_MaintenanceRecord_Schedule ON dbo.MaintenanceRecord(VehicleId, StartAt, EndAt) INCLUDE (Status);

INSERT dbo.PickupLocation (Name, Address)
VALUES (N'Điểm giao nhận mẫu', N'Hà Nội');

INSERT dbo.Vehicle (PlateNumber, Brand, Model, VehicleType, Seats, Transmission, FuelType,
                    ManufactureYear, DailyRate, WeeklyRate, DepositAmount, Status)
VALUES (N'DEMO-01', N'Toyota', N'Vios', N'Sedan', 5, N'Tự động', N'Xăng', 2023, 700000, 4200000, 2000000, N'Sẵn sàng'),
       (N'DEMO-02', N'Mitsubishi', N'Xpander', N'MPV', 7, N'Tự động', N'Xăng', 2024, 900000, 5400000, 3000000, N'Sẵn sàng'),
       (N'DEMO-03', N'Hyundai', N'Accent', N'Sedan', 5, N'Tự động', N'Xăng', 2022, 650000, 3900000, 2000000, N'Bảo trì');

INSERT dbo.Customer (FullName, DateOfBirth, Phone, Email, IdentityNumber)
VALUES (N'Khách hàng mẫu', '1995-01-01', N'0000000000', N'customer@example.test', N'DEMO-ID-0001');
DECLARE @CustomerId int = CONVERT(int, SCOPE_IDENTITY());

INSERT dbo.DriverLicense (CustomerId, LicenseNumber, LicenseClass, ExpiryDate)
VALUES (@CustomerId, N'DEMO-GPLX-0001', N'B', '2099-12-31');

-- Ho so mau bi vo hieu hoa, khong co mat khau dang nhap.
INSERT dbo.AppUser (CustomerId, FullName, Email, Role, IsActive)
VALUES (@CustomerId, N'Khách hàng mẫu', N'customer@example.test', N'Khách hàng', 0),
       (NULL, N'Kinh doanh mẫu', N'sales@example.test', N'Nhân viên kinh doanh', 0),
       (NULL, N'Giao nhận mẫu', N'handover@example.test', N'Nhân viên giao nhận', 0),
       (NULL, N'Kế toán mẫu', N'accounting@example.test', N'Kế toán', 0),
       (NULL, N'Quản trị mẫu', N'admin@example.test', N'Quản trị viên', 0);

DECLARE @Now datetime2(0) = SYSUTCDATETIME();
DECLARE @LocationId int = (SELECT Id FROM dbo.PickupLocation WHERE Name = N'Điểm giao nhận mẫu');
DECLARE @VehicleId int = (SELECT Id FROM dbo.Vehicle WHERE PlateNumber = N'DEMO-01');
DECLARE @UserId int = (SELECT Id FROM dbo.AppUser WHERE Email = N'customer@example.test');

INSERT dbo.RentalRequest (CustomerId, VehicleId, StartAt, EndAt, PickupLocationId,
                          ReturnLocationId, CreatedBy, CreatedAt, ExpiresAt, Status)
VALUES (@CustomerId, @VehicleId, DATEADD(day, 1, @Now), DATEADD(day, 2, @Now),
        @LocationId, @LocationId, @UserId, @Now, DATEADD(hour, 24, @Now), N'Chờ xử lý');

COMMIT TRANSACTION;

SELECT name AS TableName FROM sys.tables ORDER BY name;
SELECT PlateNumber, Model, Status FROM dbo.Vehicle ORDER BY PlateNumber;
SELECT VehicleId, StartAt, EndAt, ExpiresAt, Status FROM dbo.RentalRequest;

