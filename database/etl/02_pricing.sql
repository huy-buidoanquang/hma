USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.PriceList ON;
INSERT INTO dbo.PriceList (Id, Code, Name, Description, CustomerId, EffectiveFrom, EffectiveTo, CreatedAt, IsLocked, LockedAt, LockReason, LegacyId)
SELECT tenbg_id,
       ISNULL(tenbg_ud, CAST(tenbg_id AS NVARCHAR(50))),
       ISNULL(tenbg_nm, N''),
       mota,
       NULL,
       ngaybdhl,
       ngaykthl,
       ngaylap,
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

-- Unpivot five DHXE tonnage columns into route items (pickup city null = any origin).
INSERT INTO dbo.PriceListItem (PriceListRevisionId, PickupCityId, DeliveryCityId, VehicleTypeId, UnitPrice, Surcharge, LegacyId)
SELECT bg_id, NULL, thanhpho_id, 1, ISNULL(cuocxe125, 0), 0, bg_ct_id FROM LEGACY.DHXE.dbo.banggia_ct WHERE cuocxe125 IS NOT NULL
UNION ALL
SELECT bg_id, NULL, thanhpho_id, 2, ISNULL(cuocxe35, 0), 0, bg_ct_id FROM LEGACY.DHXE.dbo.banggia_ct WHERE cuocxe35 IS NOT NULL
UNION ALL
SELECT bg_id, NULL, thanhpho_id, 3, ISNULL(cuocxe5, 0), 0, bg_ct_id FROM LEGACY.DHXE.dbo.banggia_ct WHERE cuocxe5 IS NOT NULL
UNION ALL
SELECT bg_id, NULL, thanhpho_id, 4, ISNULL(cuocxe145, 0), 0, bg_ct_id FROM LEGACY.DHXE.dbo.banggia_ct WHERE cuocxe145 IS NOT NULL
UNION ALL
SELECT bg_id, NULL, thanhpho_id, 5, ISNULL(cuocxe8, 0), 0, bg_ct_id FROM LEGACY.DHXE.dbo.banggia_ct WHERE cuocxe8 IS NOT NULL;

COMMIT;
GO
