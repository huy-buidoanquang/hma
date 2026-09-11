-- Requires linked server LEGACY pointing at DHXE, or edit four-part names.
-- Preserves legacy Ids via IDENTITY_INSERT.
-- Phase 1: catalogs + partners/vehicles/drivers from nhanvien plates. No cash/VAT.

USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

SET IDENTITY_INSERT dbo.City ON;
INSERT INTO dbo.City (Id, Code, Name, Description, LegacyId)
SELECT thanhpho_id,
       ISNULL(thanhpho_ud, CAST(thanhpho_id AS NVARCHAR(50))),
       ISNULL(thanhpho_nm, N''),
       mota,
       thanhpho_id
FROM LEGACY.DHXE.dbo.thanhpho;
SET IDENTITY_INSERT dbo.City OFF;

-- Legacy prices are destination-based. Materialize one location per legacy city so
-- 02_pricing.sql can preserve every rate without inventing an origin route.
INSERT INTO dbo.Location (Code, Name, Description, CityId, LegacyId)
SELECT c.Code, c.Name, c.Description, c.Id, c.LegacyId
FROM dbo.City c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Location l WHERE l.Code = c.Code);

SET IDENTITY_INSERT dbo.Department ON;
INSERT INTO dbo.Department (Id, Code, Name, LegacyId)
SELECT phongban_id,
       ISNULL(phongban_ud, CAST(phongban_id AS NVARCHAR(50))),
       ISNULL(phongban_nm, N''),
       phongban_id
FROM LEGACY.DHXE.dbo.phongban;
SET IDENTITY_INSERT dbo.Department OFF;

SET IDENTITY_INSERT dbo.JobTitle ON;
INSERT INTO dbo.JobTitle (Id, Code, Name, LegacyId)
SELECT chucvu_id,
       ISNULL(chucvu_ud, CAST(chucvu_id AS NVARCHAR(50))),
       ISNULL(chucvu_nm, N''),
       chucvu_id
FROM LEGACY.DHXE.dbo.chucvu;
SET IDENTITY_INSERT dbo.JobTitle OFF;

SET IDENTITY_INSERT dbo.VehicleType ON;
INSERT INTO dbo.VehicleType (Id, Code, Name, Tonnage)
SELECT loaixe_id,
       ISNULL(loaixe_ud, CAST(loaixe_id AS NVARCHAR(50))),
       ISNULL(loaixe_nm, N''),
       0
FROM LEGACY.DHXE.dbo.loaixe
WHERE NOT EXISTS (SELECT 1 FROM dbo.VehicleType t WHERE t.Id = loaixe.loaixe_id);
SET IDENTITY_INSERT dbo.VehicleType OFF;

SET IDENTITY_INSERT dbo.Employee ON;
INSERT INTO dbo.Employee (Id, Code, Name, Address, Phone, Mobile, BirthDate, IdentityNumber, VehiclePlate, DepartmentId, JobTitleId, UpdatedAt, LegacyId)
SELECT nhanvien_id,
       ISNULL(nhanvien_ud, CAST(nhanvien_id AS NVARCHAR(50))),
       ISNULL(nhanvien_nm, N''),
       diachi, sodt, didong, ngaysinh, socmnd, biensoxe,
       phongban_id, chucvu_id, ngaycapnhat, nhanvien_id
FROM LEGACY.DHXE.dbo.nhanvien;
SET IDENTITY_INSERT dbo.Employee OFF;

SET IDENTITY_INSERT dbo.Customer ON;
INSERT INTO dbo.Customer (Id, Code, Name, Address, Phone, TaxCode, CityId, IsWalkIn, UpdatedAt, LegacyId)
SELECT kh_id,
       ISNULL(kh_ud, CAST(kh_id AS NVARCHAR(50))),
       ISNULL(kh_nm, N''),
       diachi, sodt, masothue, thanhpho_id,
       CASE WHEN ISNULL(loai_kh, 1) = 1 THEN 0 ELSE 1 END,
       ngaycapnhat, kh_id
FROM LEGACY.DHXE.dbo.khachhang;
SET IDENTITY_INSERT dbo.Customer OFF;

IF NOT EXISTS (SELECT 1 FROM dbo.Partner WHERE Code = N'UNASSIGNED')
    INSERT INTO dbo.Partner (Code, Name) VALUES (N'UNASSIGNED', N'Chưa gán đối tác');

DECLARE @PartnerId INT = (SELECT TOP 1 Id FROM dbo.Partner WHERE Code = N'UNASSIGNED');

INSERT INTO dbo.Driver (Code, Name, Phone, BirthDate, IdentityNumber, PartnerId, LegacyId)
SELECT ISNULL(nhanvien_ud, CAST(nhanvien_id AS NVARCHAR(50))),
       ISNULL(nhanvien_nm, N''),
       ISNULL(didong, sodt),
       ngaysinh,
       socmnd,
       @PartnerId,
       nhanvien_id
FROM LEGACY.DHXE.dbo.nhanvien
WHERE NULLIF(LTRIM(RTRIM(biensoxe)), N'') IS NOT NULL;

INSERT INTO dbo.Vehicle (PlateNumber, PartnerId, VehicleTypeId, Tonnage, LegacyId)
SELECT plate, @PartnerId, NULL, NULL, MIN(nv_id)
FROM (
    SELECT LTRIM(RTRIM(biensoxe)) AS plate, nhanvien_id AS nv_id
    FROM LEGACY.DHXE.dbo.nhanvien
    WHERE NULLIF(LTRIM(RTRIM(biensoxe)), N'') IS NOT NULL
) x
GROUP BY plate;

INSERT INTO dbo.Company (Name, Address, Phone, TaxCode, Bank, Website, Email)
SELECT TOP 1 tencty, diachi, sodtfax, masothue, nganhang, website, email
FROM LEGACY.DHXE.dbo.congty;

COMMIT;
GO
