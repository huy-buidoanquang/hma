-- Phase 1 (brief): do NOT run this script. Cash receipts are out of scope.
-- Kept for a later accounting phase.
USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.CashReceipt ON;
INSERT INTO dbo.CashReceipt (
    Id, Code, DocumentDate, Kind, DispatchOrderId, CustomerId,
    Amount, AmountInWords, PayerName, Address, Reason, EmployeeId, LegacyId)
SELECT
    sopt_id,
    ISNULL(sopt_ud, CAST(sopt_id AS NVARCHAR(50))),
    ISNULL(ngaytao, ngaycapnhat),
    ISNULL(loaipt, 1),
    CASE WHEN loaipt = 0 THEN doituong_id ELSE doituong_id END,
    kh_id,
    ISNULL(sotien, tongtien),
    doctien,
    kh_nm,
    diachi,
    lydonoptien,
    nhanvien_id,
    sopt_id
FROM LEGACY.DHXE.dbo.phieuthu;
SET IDENTITY_INSERT dbo.CashReceipt OFF;

SET IDENTITY_INSERT dbo.CashPayment ON;
INSERT INTO dbo.CashPayment (
    Id, Code, DocumentDate, Kind, CustomerId, DriverEmployeeId,
    Amount, AmountInWords, PayeeName, Address, Reason, EmployeeId, LegacyId)
SELECT
    sopc_id,
    ISNULL(sopc_ud, CAST(sopc_id AS NVARCHAR(50))),
    ngaytao,
    ISNULL(loaipc, 1),
    kh_id,
    laixe_id,
    ISNULL(sotien, 0),
    doctien,
    hoten,
    diachi,
    lydonoptien,
    nhanvien_id,
    sopc_id
FROM LEGACY.DHXE.dbo.phieuchi;
SET IDENTITY_INSERT dbo.CashPayment OFF;

COMMIT;
GO
