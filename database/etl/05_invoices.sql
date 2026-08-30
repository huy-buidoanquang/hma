-- Phase 1 (brief): do NOT run this script. VAT invoices are out of scope.
-- Kept for a later accounting phase.
USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.VatInvoice ON;
INSERT INTO dbo.VatInvoice (
    Id, Code, InvoiceDate, CustomerId, EmployeeId, IsByCustomer,
    GoodsName, PaymentMethodText, Unit, Quantity, Amount, VatRate, VatAmount, TotalAmount, AmountInWords, LegacyId)
SELECT
    hdgtgt_id,
    ISNULL(hdgtgt_ud, CAST(hdgtgt_id AS NVARCHAR(50))),
    ISNULL(ngaylap, GETDATE()),
    kh_id,
    nhanvien_id,
    ISNULL(phanloai, 0),
    tenhang,
    hinhthuctt,
    dvt,
    soluong,
    ISNULL(thanhtien, 0),
    ISNULL(vat, 10),
    ISNULL(tienvat, 0),
    ISNULL(tongcong, 0),
    doctien,
    hdgtgt_id
FROM LEGACY.DHXE.dbo.hdgtgt;
SET IDENTITY_INSERT dbo.VatInvoice OFF;

INSERT INTO dbo.VatInvoiceLine (VatInvoiceId, DispatchOrderId, LegacyId)
SELECT hdgtgt_id, nil_id, hdgtgt_ct_id
FROM LEGACY.DHXE.dbo.hdgtgt_ct;

COMMIT;
GO
