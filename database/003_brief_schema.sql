-- Additive upgrade from the first DHXE-clone schema to the brief-first schema.
-- Prefer recreating via 001_schema.sql on empty LocalDB. Use this when Hma already has data
-- and Partner table is missing. Review before running on anything other than a dry-run copy.

USE Hma;
GO

IF OBJECT_ID(N'dbo.Partner', N'U') IS NOT NULL
BEGIN
    PRINT N'Schema already has Partner — skip 003.';
    RETURN;
END
GO

PRINT N'Old schema detected. Recreate from 001_schema.sql (dev LocalDB) or restore a backup first.';
-- This script does not DROP live operational data automatically.
-- For LocalDB development the WPF app drops and recreates when Partner is missing.
GO
