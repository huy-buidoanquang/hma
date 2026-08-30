USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.DispatchOrder ON;
INSERT INTO dbo.DispatchOrder (
    Id, Code, CreatedAt, Status, ReconciliationStatus, CustomerId,
    SenderCustomerId, SenderName, SenderPhone, SenderAddress, SenderTaxCode,
    ReceiverCustomerId, ReceiverName, ReceiverPhone, ReceiverAddress, ReceiverTaxCode,
    PickupAt, PickupAddress, PickupCityId, DeliveryAddress, DeliveryCityId,
    VehicleId, DriverId, VehicleTypeId, EmployeeId,
    UnitPrice, Surcharge, ExtraCost, TotalAmount, AmountInWords, Notes, LegacyId)
SELECT
    n.nil_id,
    ISNULL(n.nil_ud, CAST(n.nil_id AS NVARCHAR(50))),
    ISNULL(n.ngaylap, GETDATE()),
    2, -- Completed so historical trips can enter reconcile/statement after documents
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
    sg.thanhpho_id,
    nn.diachi,
    ISNULL(nn.thanhpho_id, n.hanhtrinh_id),
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
LEFT JOIN dbo.Driver d ON d.LegacyId = n.bienso_id;
SET IDENTITY_INSERT dbo.DispatchOrder OFF;

INSERT INTO dbo.DispatchOrderLine (DispatchOrderId, LineNumber, GoodsName, PackageCount, Route, Kilometers, Notes, LegacyId)
SELECT nil_id,
       ROW_NUMBER() OVER (PARTITION BY nil_id ORDER BY nil_ct_id),
       tenhang, sokien, hanhtrinh, sokm, ghichu, nil_ct_id
FROM LEGACY.DHXE.dbo.nil_ct;

COMMIT;
GO
