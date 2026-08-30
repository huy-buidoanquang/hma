-- Additive: optimistic concurrency tokens. Safe on a live Hma that already has data.
-- New databases get the same columns from 001_schema.sql.

USE Hma;
GO

IF COL_LENGTH(N'dbo.DispatchOrder', N'RowVersion') IS NULL
    ALTER TABLE dbo.DispatchOrder ADD RowVersion ROWVERSION NOT NULL;
IF COL_LENGTH(N'dbo.PriceList', N'RowVersion') IS NULL
    ALTER TABLE dbo.PriceList ADD RowVersion ROWVERSION NOT NULL;
IF COL_LENGTH(N'dbo.FreightStatement', N'RowVersion') IS NULL
    ALTER TABLE dbo.FreightStatement ADD RowVersion ROWVERSION NOT NULL;
IF COL_LENGTH(N'dbo.DocumentSequence', N'RowVersion') IS NULL
    ALTER TABLE dbo.DocumentSequence ADD RowVersion ROWVERSION NOT NULL;
GO
