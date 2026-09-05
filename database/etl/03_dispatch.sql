USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

INSERT INTO dbo.Location (Code, Name, Description, CityId, LegacyId)
SELECT c.Code, c.Name, c.Description, c.Id, c.LegacyId
FROM dbo.City c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Location l WHERE l.Code = c.Code);

;WITH Trip AS (
    SELECT
        n.nil_id,
        ISNULL(pl.Id, dl.Id) AS PickupLocationId,
        ISNULL(dl.Id, pl.Id) AS DeliveryLocationId,
        ISNULL(pl.Code, dl.Code) AS PickupCode,
        ISNULL(dl.Code, pl.Code) AS DeliveryCode,
        ISNULL(pl.Name, dl.Name) AS PickupName,
        ISNULL(dl.Name, pl.Name) AS DeliveryName
    FROM LEGACY.DHXE.dbo.nil n
    LEFT JOIN LEGACY.DHXE.dbo.khachhang sg ON sg.kh_id = n.nguoigui_id
    LEFT JOIN LEGACY.DHXE.dbo.khachhang nn ON nn.kh_id = n.nguoinhan_id
    LEFT JOIN dbo.Location pl ON pl.CityId = sg.thanhpho_id
    LEFT JOIN dbo.Location dl ON dl.CityId = ISNULL(nn.thanhpho_id, n.hanhtrinh_id)
    WHERE ISNULL(pl.Id, dl.Id) IS NOT NULL
      AND ISNULL(dl.Id, pl.Id) IS NOT NULL
)
INSERT INTO dbo.Route (Code, Name, Fingerprint)
SELECT DISTINCT
       LEFT(CONCAT(PickupCode, N'-', DeliveryCode, N'-', PickupLocationId, N'-', DeliveryLocationId), 50),
       CONCAT(PickupName, N' → ', DeliveryName),
       CONCAT(PickupLocationId, N'-', DeliveryLocationId)
FROM Trip t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Route r
    WHERE r.Fingerprint = CONCAT(t.PickupLocationId, N'-', t.DeliveryLocationId));

INSERT INTO dbo.RouteStop (RouteId, Sequence, LocationId)
SELECT r.Id, 0, TRY_CAST(LEFT(r.Fingerprint, CHARINDEX('-', r.Fingerprint) - 1) AS INT)
FROM dbo.Route r
WHERE NOT EXISTS (SELECT 1 FROM dbo.RouteStop s WHERE s.RouteId = r.Id AND s.Sequence = 0)
  AND CHARINDEX('-', r.Fingerprint) > 1;
INSERT INTO dbo.RouteStop (RouteId, Sequence, LocationId)
SELECT r.Id, 1, TRY_CAST(SUBSTRING(r.Fingerprint, CHARINDEX('-', r.Fingerprint) + 1, 32) AS INT)
FROM dbo.Route r
WHERE NOT EXISTS (SELECT 1 FROM dbo.RouteStop s WHERE s.RouteId = r.Id AND s.Sequence = 1)
  AND CHARINDEX('-', r.Fingerprint) > 1;

SET IDENTITY_INSERT dbo.DispatchOrder ON;
INSERT INTO dbo.DispatchOrder (
    Id, Code, CreatedAt, Status, ReconciliationStatus, CustomerId,
    SenderCustomerId, SenderName, SenderPhone, SenderAddress, SenderTaxCode,
    ReceiverCustomerId, ReceiverName, ReceiverPhone, ReceiverAddress, ReceiverTaxCode,
    PickupAt, PickupAddress, DeliveryAddress, RouteId,
    VehicleId, DriverId, VehicleTypeId, EmployeeId,
    UnitPrice, Surcharge, ExtraCost, TotalAmount, AmountInWords, Notes, LegacyId)
SELECT
    n.nil_id,
    ISNULL(n.nil_ud, CAST(n.nil_id AS NVARCHAR(50))),
    ISNULL(n.ngaylap, GETDATE()),
    2,
    0,
    n.nguoigui_id,
    n.nguoigui_id,
    sg.kh_nm,
    sg.sodt,
    sg.diachi,
    sg.masothue,
    n.nguoinhan_id,
    nn.kh_nm,
    nn.sodt,
    nn.diachi,
    nn.masothue,
    DATEADD(MINUTE, ISNULL(n.phut, 0), DATEADD(HOUR, ISNULL(n.gio, 0), CAST(CAST(ISNULL(n.ngaylap, GETDATE()) AS DATE) AS DATETIME))),
    sg.diachi,
    nn.diachi,
    r.Id,
    v.Id,
    d.Id,
    n.loaixe_id,
    n.nhanvien_id,
    ISNULL(n.cuocdv, 0),
    0,
    ISNULL(n.thukhac, 0),
    ISNULL(n.tongthu, ISNULL(n.cuocdv, 0) + ISNULL(n.thukhac, 0)),
    n.docso,
    n.ghichu,
    n.nil_id
FROM LEGACY.DHXE.dbo.nil n
LEFT JOIN LEGACY.DHXE.dbo.khachhang sg ON sg.kh_id = n.nguoigui_id
LEFT JOIN LEGACY.DHXE.dbo.khachhang nn ON nn.kh_id = n.nguoinhan_id
LEFT JOIN LEGACY.DHXE.dbo.nhanvien nv ON nv.nhanvien_id = n.bienso_id
LEFT JOIN dbo.Vehicle v ON v.PlateNumber = LTRIM(RTRIM(nv.biensoxe))
LEFT JOIN dbo.Driver d ON d.LegacyId = n.bienso_id
LEFT JOIN dbo.Location pl ON pl.CityId = sg.thanhpho_id
LEFT JOIN dbo.Location dl ON dl.CityId = ISNULL(nn.thanhpho_id, n.hanhtrinh_id)
LEFT JOIN dbo.Route r ON r.Fingerprint = CONCAT(ISNULL(pl.Id, dl.Id), N'-', ISNULL(dl.Id, pl.Id));
SET IDENTITY_INSERT dbo.DispatchOrder OFF;

INSERT INTO dbo.DispatchOrderStop (DispatchOrderId, Sequence, LocationId, NameSnapshot)
SELECT o.Id, s.Sequence, s.LocationId, ISNULL(l.Name, N'')
FROM dbo.DispatchOrder o
INNER JOIN dbo.RouteStop s ON s.RouteId = o.RouteId
INNER JOIN dbo.Location l ON l.Id = s.LocationId
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DispatchOrderStop x WHERE x.DispatchOrderId = o.Id AND x.Sequence = s.Sequence);

INSERT INTO dbo.DispatchOrderLine (DispatchOrderId, LineNumber, GoodsName, PackageCount, Route, Kilometers, Notes, LegacyId)
SELECT nil_id,
       ROW_NUMBER() OVER (PARTITION BY nil_id ORDER BY nil_ct_id),
       tenhang, sokien, hanhtrinh, sokm, ghichu, nil_ct_id
FROM LEGACY.DHXE.dbo.nil_ct;

COMMIT;
GO
