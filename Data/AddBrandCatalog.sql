SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Brand', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Brand (Id int IDENTITY PRIMARY KEY, Name nvarchar(80) NOT NULL UNIQUE);
    INSERT dbo.Brand (Name) SELECT DISTINCT Brand FROM dbo.Vehicle;
END;
IF OBJECT_ID(N'dbo.FK_Vehicle_BrandCatalog', N'F') IS NULL
BEGIN
    INSERT dbo.Brand (Name)
    SELECT DISTINCT v.Brand FROM dbo.Vehicle v
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Brand b WHERE b.Name = v.Brand);
    ALTER TABLE dbo.Vehicle ADD CONSTRAINT FK_Vehicle_BrandCatalog
        FOREIGN KEY (Brand) REFERENCES dbo.Brand(Name) ON UPDATE CASCADE;
END;
COMMIT TRANSACTION;
