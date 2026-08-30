USE Hma;
GO
SET NOCOUNT ON;

UPDATE dbo.DocumentSequence SET LastValue = ISNULL((
    SELECT CAST(giatri AS INT) FROM LEGACY.DHXE.dbo.sinhma WHERE sinhma_ud = N'nil_ud'), LastValue)
WHERE [Key] = N'dispatch-order';

UPDATE dbo.SystemParameter SET [Value] = (
    SELECT CAST(giatri AS NVARCHAR(255)) FROM LEGACY.DHXE.dbo.sinhma WHERE sinhma_ud = N'VAT')
WHERE [Key] = N'VatRate'
  AND EXISTS (SELECT 1 FROM LEGACY.DHXE.dbo.sinhma WHERE sinhma_ud = N'VAT');
GO
