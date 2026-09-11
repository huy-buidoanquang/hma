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

-- Legacy banggia_ct stores destination-based rates. Preserve them as fallback
-- PriceListItem rows; the runtime engine prefers an exact route when one exists.
INSERT INTO dbo.PriceListItem (
    PriceListRevisionId, RouteId, DeliveryLocationId, VehicleTypeId,
    UnitPrice, Surcharge, LegacyId)
SELECT
    d.bg_id,
    NULL,
    l.Id,
    rate.VehicleTypeId,
    ISNULL(rate.UnitPrice, 0),
    0,
    d.bg_ct_id
FROM LEGACY.DHXE.dbo.banggia_ct d
INNER JOIN dbo.Location l ON l.CityId = d.thanhpho_id
CROSS APPLY (VALUES
    (d.id125, d.cuocxe125),
    (d.id35,  d.cuocxe35),
    (d.id5,   d.cuocxe5),
    (d.id145, d.cuocxe145),
    (d.id8,   d.cuocxe8)
) rate(VehicleTypeId, UnitPrice)
WHERE rate.VehicleTypeId IS NOT NULL
  AND rate.UnitPrice IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.VehicleType v WHERE v.Id = rate.VehicleTypeId)
  AND EXISTS (SELECT 1 FROM dbo.PriceListRevision r WHERE r.Id = d.bg_id)
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.PriceListItem i
      WHERE i.PriceListRevisionId = d.bg_id
        AND i.RouteId IS NULL
        AND i.DeliveryLocationId = l.Id
        AND i.VehicleTypeId = rate.VehicleTypeId);

UPDATE p
SET HasPriceFluctuation = 1
FROM dbo.PriceList p
WHERE EXISTS (
    SELECT 1
    FROM LEGACY.DHXE.dbo.banggia_ct d
    WHERE d.tenbg_id = p.LegacyId AND ISNULL(d.dacbiet, 0) = 1);

COMMIT;
GO
