-- Additive: soft-delete flag on dispatch orders. Safe on a live Hma that already has data.

USE Hma;
GO

IF COL_LENGTH(N'dbo.DispatchOrder', N'IsDeleted') IS NULL
    ALTER TABLE dbo.DispatchOrder ADD IsDeleted BIT NOT NULL CONSTRAINT DF_DispatchOrder_IsDeleted DEFAULT (0);
GO
