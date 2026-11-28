-- Run once on the EXISTING UTC-based V2 database after a verified backup and with the app stopped.
-- Does not recreate tables, change statuses/money/IDs, or change DATE-only birth/license dates.
-- Run with sqlcmd -b -I. No USE statement: explicitly choose the intended database with -d.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID(N'dbo.chiTietYeuCauThue') IS NULL OR COL_LENGTH(N'dbo.hopDong',N'phiTaiXe') IS NULL
    THROW 50120,N'Database không đúng cấu trúc V2 nhiều xe. Dừng chuyển giờ.',1;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RentCar.TimeStorage' AND CONVERT(nvarchar(100),value)=N'Asia/Ho_Chi_Minh')
BEGIN PRINT N'Đã lưu giờ Việt Nam; không cộng thêm lần nữa.'; RETURN; END;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RentCar.TimeStorage')
    THROW 50121,N'Database có quy ước thời gian chưa xác định. Cần đối chiếu trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'RentCar.TimeStorageMigration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 50122,N'Không lấy được khóa chuyển giờ.',1;
 -- A second concurrent invocation must recheck after acquiring the lock.
 IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RentCar.TimeStorage')
 BEGIN COMMIT; PRINT N'Đã chuyển trước đó.'; RETURN; END;
 DECLARE @triggers TABLE(Name nvarchar(520));
 INSERT @triggers SELECT QUOTENAME(OBJECT_SCHEMA_NAME(object_id))+N'.'+QUOTENAME(name) FROM sys.triggers WHERE parent_class=1 AND is_disabled=0 AND is_ms_shipped=0;
 DECLARE @defaults TABLE(TableName nvarchar(520),Name sysname,ColumnName sysname,Definition nvarchar(max));
 INSERT @defaults SELECT QUOTENAME(OBJECT_SCHEMA_NAME(d.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(d.parent_object_id)),d.name,c.name,d.definition
 FROM sys.default_constraints d JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id WHERE d.definition LIKE N'%SYSUTCDATETIME%';
 DECLARE @modules TABLE(Definition nvarchar(max),Ansi bit,Quoted bit);
 INSERT @modules SELECT definition,uses_ansi_nulls,uses_quoted_identifier FROM sys.sql_modules WHERE definition LIKE N'%SYSUTCDATETIME%';
 DECLARE @name nvarchar(520),@sql nvarchar(max),@column sysname,@constraint sysname,@definition nvarchar(max),@ansi bit,@quoted bit;
 DECLARE triggers_off CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @triggers;
 OPEN triggers_off; FETCH NEXT FROM triggers_off INTO @name;
 WHILE @@FETCH_STATUS=0 BEGIN SET @sql=N'DISABLE TRIGGER '+@name+N' ON '+QUOTENAME(OBJECT_SCHEMA_NAME(OBJECT_ID(@name)))+N'.'+QUOTENAME(OBJECT_NAME((SELECT parent_id FROM sys.triggers WHERE object_id=OBJECT_ID(@name)))); EXEC sys.sp_executesql @sql; FETCH NEXT FROM triggers_off INTO @name; END;
 CLOSE triggers_off; DEALLOCATE triggers_off;
 DECLARE tables_to_shift CURSOR LOCAL FAST_FORWARD FOR
 SELECT QUOTENAME(SCHEMA_NAME(t.schema_id))+N'.'+QUOTENAME(t.name),STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)+N'=DATEADD(HOUR,7,'+QUOTENAME(c.name)+N')'),N',')
 FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types y ON y.user_type_id=c.user_type_id
 WHERE t.is_ms_shipped=0 AND c.is_computed=0 AND y.name IN('datetime','datetime2','smalldatetime') GROUP BY t.schema_id,t.name;
 OPEN tables_to_shift; FETCH NEXT FROM tables_to_shift INTO @name,@sql;
 WHILE @@FETCH_STATUS=0 BEGIN EXEC(N'UPDATE '+@name+N' SET '+@sql+N';'); FETCH NEXT FROM tables_to_shift INTO @name,@sql; END;
 CLOSE tables_to_shift; DEALLOCATE tables_to_shift;
 DECLARE default_updates CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Name,ColumnName,Definition FROM @defaults;
 OPEN default_updates; FETCH NEXT FROM default_updates INTO @name,@constraint,@column,@definition;
 WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'ALTER TABLE '+@name+N' DROP CONSTRAINT '+QUOTENAME(@constraint)+N'; ALTER TABLE '+@name+N' ADD CONSTRAINT '+QUOTENAME(@constraint)+N' DEFAULT '+REPLACE(@definition,N'SYSUTCDATETIME()',N'DATEADD(HOUR,7,SYSUTCDATETIME())')+N' FOR '+QUOTENAME(@column)+N';';
 EXEC sys.sp_executesql @sql; FETCH NEXT FROM default_updates INTO @name,@constraint,@column,@definition; END;
 CLOSE default_updates; DEALLOCATE default_updates;
 DECLARE module_updates CURSOR LOCAL FAST_FORWARD FOR SELECT Definition,Ansi,Quoted FROM @modules;
 OPEN module_updates; FETCH NEXT FROM module_updates INTO @definition,@ansi,@quoted;
 WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=REPLACE(@definition,N'SYSUTCDATETIME()',N'DATEADD(HOUR,7,SYSUTCDATETIME())');
 IF UPPER(@sql) NOT LIKE N'%CREATE OR ALTER%' AND CHARINDEX(N'CREATE',UPPER(@sql))>0
 SET @sql=STUFF(@sql,CHARINDEX(N'CREATE',UPPER(@sql)),6,N'ALTER');
 -- Reuse original module settings; dynamic ALTER must start its own batch.
 IF @ansi=1 SET ANSI_NULLS ON ELSE SET ANSI_NULLS OFF;
 IF @quoted=1 SET QUOTED_IDENTIFIER ON ELSE SET QUOTED_IDENTIFIER OFF;
 EXEC sys.sp_executesql @sql; FETCH NEXT FROM module_updates INTO @definition,@ansi,@quoted; END;
 CLOSE module_updates; DEALLOCATE module_updates;
 SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
 DECLARE triggers_on CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @triggers;
 OPEN triggers_on; FETCH NEXT FROM triggers_on INTO @name;
 WHILE @@FETCH_STATUS=0 BEGIN SET @sql=N'ENABLE TRIGGER '+@name+N' ON '+QUOTENAME(OBJECT_SCHEMA_NAME(OBJECT_ID(@name)))+N'.'+QUOTENAME(OBJECT_NAME((SELECT parent_id FROM sys.triggers WHERE object_id=OBJECT_ID(@name)))); EXEC sys.sp_executesql @sql; FETCH NEXT FROM triggers_on INTO @name; END;
 CLOSE triggers_on; DEALLOCATE triggers_on;
 EXEC sys.sp_addextendedproperty @name=N'RentCar.TimeStorage',@value=N'Asia/Ho_Chi_Minh';
 COMMIT;
 PRINT N'Đã chuyển dữ liệu datetime, mặc định và logic SQL sang giờ Việt Nam. DATE, ID, trạng thái, tiền được giữ nguyên.';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;