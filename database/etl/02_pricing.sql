USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.PriceList ON;
INSERT INTO dbo.PriceList (Id, Code, Name, Description, CustomerId, EffectiveFrom, EffectiveTo, CreatedAt, HasPriceFluctuation, IsLocked, LockedAt, LockReason, LegacyId)
SELECT tenbg_id,
       ISNULL(tenbg_ud, CAST(tenbg_id AS NVARCHAR(50))),
       ISNULL(tenbg_nm, N''),
       mota,
       NULL,
       ngaybdhl,
       ngaykthl,
       ngaylap,
       0,
       CASE WHEN ngaykhoa IS NULL THEN 0 ELSE 1 END,
       ngaykhoa,
       lydokhoa,
       tenbg_id
FROM LEGACY.DHXE.dbo.tenbg;
SET IDENTITY_INSERT dbo.PriceList OFF;

SET IDENTITY_INSERT dbo.PriceListRevision ON;
INSERT INTO dbo.PriceListRevision (Id, PriceListId, EmployeeId, CreatedAt, LegacyId)
SELECT bg_id, tenbg_id, nhanvien_id, ngaylap, bg_id
FROM LEGACY.DHXE.dbo.banggia;
SET IDENTITY_INSERT dbo.PriceListRevision OFF;

-- Legacy banggia_ct only has delivery city (any origin). New PriceListItem requires a catalog Route
-- (customer × route × vehicle type × date). Re-enter dòng giá in the app after ETL.

COMMIT;
GO
