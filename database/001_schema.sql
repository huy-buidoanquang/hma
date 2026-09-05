-- Hma target schema (SQL Server 2016+) — brief-first domain.
-- English PascalCase names. Frozen Cash/VAT tables remain for later accounting phase.

IF DB_ID(N'Hma') IS NULL
    CREATE DATABASE Hma;
GO
USE Hma;
GO

IF OBJECT_ID(N'dbo.FreightStatementLine', N'U') IS NOT NULL DROP TABLE dbo.FreightStatementLine;
IF OBJECT_ID(N'dbo.FreightStatement', N'U') IS NOT NULL DROP TABLE dbo.FreightStatement;
IF OBJECT_ID(N'dbo.DispatchDocument', N'U') IS NOT NULL DROP TABLE dbo.DispatchDocument;
IF OBJECT_ID(N'dbo.ChangeLog', N'U') IS NOT NULL DROP TABLE dbo.ChangeLog;
IF OBJECT_ID(N'dbo.VatInvoiceLine', N'U') IS NOT NULL DROP TABLE dbo.VatInvoiceLine;
IF OBJECT_ID(N'dbo.VatInvoice', N'U') IS NOT NULL DROP TABLE dbo.VatInvoice;
IF OBJECT_ID(N'dbo.CashPayment', N'U') IS NOT NULL DROP TABLE dbo.CashPayment;
IF OBJECT_ID(N'dbo.CashReceipt', N'U') IS NOT NULL DROP TABLE dbo.CashReceipt;
IF OBJECT_ID(N'dbo.DispatchOrderStop', N'U') IS NOT NULL DROP TABLE dbo.DispatchOrderStop;
IF OBJECT_ID(N'dbo.DispatchOrderLine', N'U') IS NOT NULL DROP TABLE dbo.DispatchOrderLine;
IF OBJECT_ID(N'dbo.DispatchOrder', N'U') IS NOT NULL DROP TABLE dbo.DispatchOrder;
IF OBJECT_ID(N'dbo.PriceListItem', N'U') IS NOT NULL DROP TABLE dbo.PriceListItem;
IF OBJECT_ID(N'dbo.PriceListRevision', N'U') IS NOT NULL DROP TABLE dbo.PriceListRevision;
IF OBJECT_ID(N'dbo.PriceList', N'U') IS NOT NULL DROP TABLE dbo.PriceList;
IF OBJECT_ID(N'dbo.UserPermission', N'U') IS NOT NULL DROP TABLE dbo.UserPermission;
IF OBJECT_ID(N'dbo.AppUser', N'U') IS NOT NULL DROP TABLE dbo.AppUser;
IF OBJECT_ID(N'dbo.CustomerAlias', N'U') IS NOT NULL DROP TABLE dbo.CustomerAlias;
IF OBJECT_ID(N'dbo.LocationAlias', N'U') IS NOT NULL DROP TABLE dbo.LocationAlias;
IF OBJECT_ID(N'dbo.RouteAlias', N'U') IS NOT NULL DROP TABLE dbo.RouteAlias;
IF OBJECT_ID(N'dbo.RouteStop', N'U') IS NOT NULL DROP TABLE dbo.RouteStop;
IF OBJECT_ID(N'dbo.Route', N'U') IS NOT NULL DROP TABLE dbo.Route;
IF OBJECT_ID(N'dbo.Location', N'U') IS NOT NULL DROP TABLE dbo.Location;
IF OBJECT_ID(N'dbo.Customer', N'U') IS NOT NULL DROP TABLE dbo.Customer;
IF OBJECT_ID(N'dbo.VehicleAlias', N'U') IS NOT NULL DROP TABLE dbo.VehicleAlias;
IF OBJECT_ID(N'dbo.Vehicle', N'U') IS NOT NULL DROP TABLE dbo.Vehicle;
IF OBJECT_ID(N'dbo.Driver', N'U') IS NOT NULL DROP TABLE dbo.Driver;
IF OBJECT_ID(N'dbo.Partner', N'U') IS NOT NULL DROP TABLE dbo.Partner;
IF OBJECT_ID(N'dbo.Employee', N'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID(N'dbo.JobTitle', N'U') IS NOT NULL DROP TABLE dbo.JobTitle;
IF OBJECT_ID(N'dbo.Department', N'U') IS NOT NULL DROP TABLE dbo.Department;
IF OBJECT_ID(N'dbo.City', N'U') IS NOT NULL DROP TABLE dbo.City;
IF OBJECT_ID(N'dbo.VehicleType', N'U') IS NOT NULL DROP TABLE dbo.VehicleType;
IF OBJECT_ID(N'dbo.PaymentMethod', N'U') IS NOT NULL DROP TABLE dbo.PaymentMethod;
IF OBJECT_ID(N'dbo.AppScreen', N'U') IS NOT NULL DROP TABLE dbo.AppScreen;
IF OBJECT_ID(N'dbo.DocumentSequence', N'U') IS NOT NULL DROP TABLE dbo.DocumentSequence;
IF OBJECT_ID(N'dbo.SystemParameter', N'U') IS NOT NULL DROP TABLE dbo.SystemParameter;
IF OBJECT_ID(N'dbo.Company', N'U') IS NOT NULL DROP TABLE dbo.Company;
GO

CREATE TABLE dbo.City (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_City PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_City_Code UNIQUE (Code)
);

CREATE TABLE dbo.Location (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    CityId INT NULL CONSTRAINT FK_Location_City REFERENCES dbo.City (Id),
    LegacyId INT NULL,
    CONSTRAINT UQ_Location_Code UNIQUE (Code)
);
CREATE INDEX IX_Location_City ON dbo.Location (CityId);

CREATE TABLE dbo.Route (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Route PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Fingerprint NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_Route_Code UNIQUE (Code),
    CONSTRAINT UQ_Route_Fingerprint UNIQUE (Fingerprint)
);

CREATE TABLE dbo.RouteStop (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RouteStop PRIMARY KEY,
    RouteId INT NOT NULL CONSTRAINT FK_RouteStop_Route REFERENCES dbo.Route (Id) ON DELETE CASCADE,
    Sequence INT NOT NULL,
    LocationId INT NOT NULL CONSTRAINT FK_RouteStop_Location REFERENCES dbo.Location (Id),
    LegacyId INT NULL,
    CONSTRAINT UQ_RouteStop_Route_Sequence UNIQUE (RouteId, Sequence)
);
CREATE INDEX IX_RouteStop_Location ON dbo.RouteStop (LocationId);

CREATE TABLE dbo.LocationAlias (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LocationAlias PRIMARY KEY,
    Alias NVARCHAR(100) NOT NULL,
    LocationId INT NOT NULL CONSTRAINT FK_LocationAlias_Location REFERENCES dbo.Location (Id),
    Kind INT NOT NULL CONSTRAINT DF_LocationAlias_Kind DEFAULT (0),
    CONSTRAINT UQ_LocationAlias_Alias UNIQUE (Alias)
);
CREATE INDEX IX_LocationAlias_Location ON dbo.LocationAlias (LocationId);

CREATE TABLE dbo.RouteAlias (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RouteAlias PRIMARY KEY,
    Alias NVARCHAR(100) NOT NULL,
    RouteId INT NOT NULL CONSTRAINT FK_RouteAlias_Route REFERENCES dbo.Route (Id),
    CONSTRAINT UQ_RouteAlias_Alias UNIQUE (Alias)
);
CREATE INDEX IX_RouteAlias_Route ON dbo.RouteAlias (RouteId);

CREATE TABLE dbo.Department (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_Department_Code UNIQUE (Code)
);

CREATE TABLE dbo.JobTitle (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobTitle PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_JobTitle_Code UNIQUE (Code)
);

CREATE TABLE dbo.VehicleType (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleType PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Tonnage DECIMAL(9,2) NOT NULL,
    CONSTRAINT UQ_VehicleType_Code UNIQUE (Code)
);

CREATE TABLE dbo.PaymentMethod (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentMethod PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_PaymentMethod_Code UNIQUE (Code)
);

CREATE TABLE dbo.Employee (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    Phone NVARCHAR(50) NULL,
    Mobile NVARCHAR(50) NULL,
    BirthDate DATE NULL,
    IdentityNumber NVARCHAR(50) NULL,
    VehiclePlate NVARCHAR(50) NULL,
    DepartmentId INT NULL CONSTRAINT FK_Employee_Department REFERENCES dbo.Department (Id),
    JobTitleId INT NULL CONSTRAINT FK_Employee_JobTitle REFERENCES dbo.JobTitle (Id),
    UpdatedAt DATETIME2 NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_Employee_Code UNIQUE (Code)
);

CREATE TABLE dbo.Partner (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Partner PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    TaxCode NVARCHAR(50) NULL,
    Address NVARCHAR(255) NULL,
    ContactName NVARCHAR(255) NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(255) NULL,
    OperatingFeePercent DECIMAL(9,2) NOT NULL CONSTRAINT DF_Partner_OpFee DEFAULT (0),
    LegacyId INT NULL,
    CONSTRAINT UQ_Partner_Code UNIQUE (Code)
);

CREATE TABLE dbo.Driver (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Driver PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(50) NULL,
    BirthDate DATE NULL,
    IdentityNumber NVARCHAR(50) NULL,
    PartnerId INT NOT NULL CONSTRAINT FK_Driver_Partner REFERENCES dbo.Partner (Id),
    LegacyId INT NULL,
    CONSTRAINT UQ_Driver_Code UNIQUE (Code)
);

CREATE TABLE dbo.Vehicle (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vehicle PRIMARY KEY,
    PlateNumber NVARCHAR(50) NOT NULL,
    PartnerId INT NOT NULL CONSTRAINT FK_Vehicle_Partner REFERENCES dbo.Partner (Id),
    VehicleTypeId INT NULL CONSTRAINT FK_Vehicle_VehicleType REFERENCES dbo.VehicleType (Id),
    Tonnage DECIMAL(9,2) NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_Vehicle_Plate UNIQUE (PlateNumber)
);

CREATE TABLE dbo.VehicleAlias (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleAlias PRIMARY KEY,
    Alias NVARCHAR(100) NOT NULL,
    VehicleId INT NOT NULL CONSTRAINT FK_VehicleAlias_Vehicle REFERENCES dbo.Vehicle (Id) ON DELETE CASCADE,
    CONSTRAINT UQ_VehicleAlias_Alias UNIQUE (Alias)
);
CREATE INDEX IX_VehicleAlias_Vehicle ON dbo.VehicleAlias (VehicleId);

CREATE TABLE dbo.Customer (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    Phone NVARCHAR(50) NULL,
    TaxCode NVARCHAR(50) NULL,
    ContactName NVARCHAR(255) NULL,
    Email NVARCHAR(255) NULL,
    CityId INT NULL CONSTRAINT FK_Customer_City REFERENCES dbo.City (Id),
    AccountantEmployeeId INT NULL CONSTRAINT FK_Customer_Accountant REFERENCES dbo.Employee (Id),
    IsWalkIn BIT NOT NULL CONSTRAINT DF_Customer_IsWalkIn DEFAULT (0),
    UpdatedAt DATETIME2 NULL,
    LegacyId INT NULL
);

CREATE INDEX IX_Customer_Code ON dbo.Customer (Code);
CREATE INDEX IX_Customer_Name ON dbo.Customer (Name);

CREATE TABLE dbo.CustomerAlias (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerAlias PRIMARY KEY,
    Alias NVARCHAR(100) NOT NULL,
    CustomerId INT NOT NULL CONSTRAINT FK_CustomerAlias_Customer REFERENCES dbo.Customer (Id),
    CONSTRAINT UQ_CustomerAlias_Alias UNIQUE (Alias)
);
CREATE INDEX IX_CustomerAlias_Customer ON dbo.CustomerAlias (CustomerId);

CREATE TABLE dbo.PriceList (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PriceList PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    CustomerId INT NULL CONSTRAINT FK_PriceList_Customer REFERENCES dbo.Customer (Id),
    EffectiveFrom DATE NULL,
    EffectiveTo DATE NULL,
    CreatedAt DATETIME2 NULL,
    HasPriceFluctuation BIT NOT NULL CONSTRAINT DF_PriceList_Fluctuation DEFAULT (0),
    IsLocked BIT NOT NULL CONSTRAINT DF_PriceList_IsLocked DEFAULT (0),
    LockedAt DATETIME2 NULL,
    LockReason NVARCHAR(255) NULL,
    LegacyId INT NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_PriceList_Code UNIQUE (Code)
);

CREATE TABLE dbo.PriceListRevision (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PriceListRevision PRIMARY KEY,
    PriceListId INT NOT NULL CONSTRAINT FK_PriceListRevision_PriceList REFERENCES dbo.PriceList (Id),
    EmployeeId INT NULL CONSTRAINT FK_PriceListRevision_Employee REFERENCES dbo.Employee (Id),
    CreatedAt DATETIME2 NULL,
    LegacyId INT NULL
);

CREATE TABLE dbo.PriceListItem (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PriceListItem PRIMARY KEY,
    PriceListRevisionId INT NOT NULL CONSTRAINT FK_PriceListItem_Revision REFERENCES dbo.PriceListRevision (Id),
    RouteId INT NOT NULL CONSTRAINT FK_PriceListItem_Route REFERENCES dbo.Route (Id),
    VehicleTypeId INT NOT NULL CONSTRAINT FK_PriceListItem_VehicleType REFERENCES dbo.VehicleType (Id),
    UnitPrice DECIMAL(20,2) NOT NULL CONSTRAINT DF_PriceListItem_Unit DEFAULT (0),
    Surcharge DECIMAL(20,2) NOT NULL CONSTRAINT DF_PriceListItem_Surcharge DEFAULT (0),
    LegacyId INT NULL
);

CREATE TABLE dbo.AppScreen (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppScreen PRIMARY KEY,
    [Key] NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    CONSTRAINT UQ_AppScreen_Key UNIQUE ([Key])
);

CREATE TABLE dbo.AppUser (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUser PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255) NULL,
    EmployeeId INT NULL CONSTRAINT FK_AppUser_Employee REFERENCES dbo.Employee (Id),
    IsManager BIT NOT NULL CONSTRAINT DF_AppUser_IsManager DEFAULT (0),
    IsSpecial BIT NOT NULL CONSTRAINT DF_AppUser_IsSpecial DEFAULT (0),
    CreatedAt DATETIME2 NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_AppUser_UserName UNIQUE (UserName)
);

CREATE TABLE dbo.UserPermission (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPermission PRIMARY KEY,
    AppUserId INT NOT NULL CONSTRAINT FK_UserPermission_User REFERENCES dbo.AppUser (Id) ON DELETE CASCADE,
    AppScreenId INT NOT NULL CONSTRAINT FK_UserPermission_Screen REFERENCES dbo.AppScreen (Id),
    CanCreate BIT NOT NULL CONSTRAINT DF_UserPermission_Create DEFAULT (0),
    CanDelete BIT NOT NULL CONSTRAINT DF_UserPermission_Delete DEFAULT (0),
    CanUpdate BIT NOT NULL CONSTRAINT DF_UserPermission_Update DEFAULT (0),
    CanView BIT NOT NULL CONSTRAINT DF_UserPermission_View DEFAULT (0),
    CanPrint BIT NOT NULL CONSTRAINT DF_UserPermission_Print DEFAULT (0),
    CONSTRAINT UQ_UserPermission_User_Screen UNIQUE (AppUserId, AppScreenId)
);

CREATE TABLE dbo.DispatchOrder (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DispatchOrder PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedByUserId INT NULL CONSTRAINT FK_DispatchOrder_CreatedBy REFERENCES dbo.AppUser (Id),
    Status INT NOT NULL CONSTRAINT DF_DispatchOrder_Status DEFAULT (1),
    ReconciliationStatus INT NOT NULL CONSTRAINT DF_DispatchOrder_Recon DEFAULT (0),
    ReconciledAt DATETIME2 NULL,
    ReconciledByUserId INT NULL CONSTRAINT FK_DispatchOrder_ReconciledBy REFERENCES dbo.AppUser (Id),
    CustomerId INT NULL CONSTRAINT FK_DispatchOrder_Customer REFERENCES dbo.Customer (Id),
    SenderCustomerId INT NULL CONSTRAINT FK_DispatchOrder_Sender REFERENCES dbo.Customer (Id),
    SenderName NVARCHAR(255) NULL,
    SenderPhone NVARCHAR(50) NULL,
    SenderAddress NVARCHAR(255) NULL,
    SenderTaxCode NVARCHAR(50) NULL,
    ReceiverCustomerId INT NULL CONSTRAINT FK_DispatchOrder_Receiver REFERENCES dbo.Customer (Id),
    ReceiverName NVARCHAR(255) NULL,
    ReceiverPhone NVARCHAR(50) NULL,
    ReceiverAddress NVARCHAR(255) NULL,
    ReceiverTaxCode NVARCHAR(50) NULL,
    PickupAt DATETIME2 NOT NULL,
    PickupAddress NVARCHAR(255) NULL,
    DeliveryAddress NVARCHAR(255) NULL,
    RouteId INT NULL CONSTRAINT FK_DispatchOrder_Route REFERENCES dbo.Route (Id),
    VehicleId INT NULL CONSTRAINT FK_DispatchOrder_Vehicle REFERENCES dbo.Vehicle (Id),
    DriverId INT NULL CONSTRAINT FK_DispatchOrder_Driver REFERENCES dbo.Driver (Id),
    VehicleTypeId INT NULL CONSTRAINT FK_DispatchOrder_VehicleType REFERENCES dbo.VehicleType (Id),
    EmployeeId INT NULL CONSTRAINT FK_DispatchOrder_Employee REFERENCES dbo.Employee (Id),
    PaymentMethodId INT NULL CONSTRAINT FK_DispatchOrder_Payment REFERENCES dbo.PaymentMethod (Id),
    BillingYear INT NOT NULL CONSTRAINT DF_DispatchOrder_BillYear DEFAULT (0),
    BillingMonth INT NOT NULL CONSTRAINT DF_DispatchOrder_BillMonth DEFAULT (0),
    ConfirmedByUserId INT NULL CONSTRAINT FK_DispatchOrder_ConfirmedBy REFERENCES dbo.AppUser (Id),
    ConfirmedAt DATETIME2 NULL,
    ArNumber NVARCHAR(50) NULL,
    UnitPrice DECIMAL(20,2) NOT NULL CONSTRAINT DF_DispatchOrder_Unit DEFAULT (0),
    Surcharge DECIMAL(20,2) NOT NULL CONSTRAINT DF_DispatchOrder_Surcharge DEFAULT (0),
    ExtraCost DECIMAL(20,2) NOT NULL CONSTRAINT DF_DispatchOrder_Extra DEFAULT (0),
    TotalAmount DECIMAL(20,2) NOT NULL CONSTRAINT DF_DispatchOrder_Total DEFAULT (0),
    AmountInWords NVARCHAR(500) NULL,
    Notes NVARCHAR(500) NULL,
    LegacyId INT NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_DispatchOrder_IsDeleted DEFAULT (0),
    RowVersion ROWVERSION NOT NULL
);

CREATE INDEX IX_DispatchOrder_Code ON dbo.DispatchOrder (Code);
CREATE INDEX IX_DispatchOrder_PickupAt ON dbo.DispatchOrder (PickupAt);
CREATE INDEX IX_DispatchOrder_Customer ON dbo.DispatchOrder (CustomerId);
CREATE INDEX IX_DispatchOrder_Route ON dbo.DispatchOrder (RouteId);

CREATE TABLE dbo.DispatchOrderStop (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DispatchOrderStop PRIMARY KEY,
    DispatchOrderId INT NOT NULL CONSTRAINT FK_DispatchOrderStop_Order REFERENCES dbo.DispatchOrder (Id) ON DELETE CASCADE,
    Sequence INT NOT NULL,
    LocationId INT NOT NULL CONSTRAINT FK_DispatchOrderStop_Location REFERENCES dbo.Location (Id),
    NameSnapshot NVARCHAR(255) NOT NULL,
    LegacyId INT NULL,
    CONSTRAINT UQ_DispatchOrderStop_Order_Sequence UNIQUE (DispatchOrderId, Sequence)
);
CREATE INDEX IX_DispatchOrderStop_Location ON dbo.DispatchOrderStop (LocationId);

CREATE TABLE dbo.DispatchOrderLine (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DispatchOrderLine PRIMARY KEY,
    DispatchOrderId INT NOT NULL CONSTRAINT FK_DispatchOrderLine_Order REFERENCES dbo.DispatchOrder (Id) ON DELETE CASCADE,
    LineNumber INT NOT NULL,
    GoodsName NVARCHAR(255) NULL,
    PackageCount INT NULL,
    Route NVARCHAR(255) NULL,
    Kilometers DECIMAL(18,2) NULL,
    Notes NVARCHAR(255) NULL,
    LegacyId INT NULL
);

CREATE TABLE dbo.DispatchDocument (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DispatchDocument PRIMARY KEY,
    DispatchOrderId INT NOT NULL CONSTRAINT FK_DispatchDocument_Order REFERENCES dbo.DispatchOrder (Id) ON DELETE CASCADE,
    Kind INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StoredPath NVARCHAR(500) NOT NULL,
    UploadedAt DATETIME2 NOT NULL,
    UploadedByUserId INT NULL CONSTRAINT FK_DispatchDocument_User REFERENCES dbo.AppUser (Id),
    LegacyId INT NULL
);

CREATE TABLE dbo.FreightStatement (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FreightStatement PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    CustomerId INT NOT NULL CONSTRAINT FK_FreightStatement_Customer REFERENCES dbo.Customer (Id),
    [Year] INT NOT NULL,
    [Month] INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    TripCount INT NOT NULL CONSTRAINT DF_FreightStatement_Trips DEFAULT (0),
    FreightTotal DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_Freight DEFAULT (0),
    SurchargeTotal DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_Surcharge DEFAULT (0),
    ExtraCostTotal DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_Extra DEFAULT (0),
    GrandTotal DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_Grand DEFAULT (0),
    VatRate DECIMAL(9,2) NOT NULL CONSTRAINT DF_FreightStatement_VatRate DEFAULT (10),
    VatAmount DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_VatAmt DEFAULT (0),
    TotalWithVat DECIMAL(20,2) NOT NULL CONSTRAINT DF_FreightStatement_WithVat DEFAULT (0),
    Notes NVARCHAR(500) NULL,
    LegacyId INT NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_FreightStatement_Customer_Period UNIQUE (CustomerId, [Year], [Month])
);

CREATE TABLE dbo.FreightStatementLine (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FreightStatementLine PRIMARY KEY,
    FreightStatementId INT NOT NULL CONSTRAINT FK_FreightStatementLine_Header REFERENCES dbo.FreightStatement (Id) ON DELETE CASCADE,
    DispatchOrderId INT NOT NULL CONSTRAINT FK_FreightStatementLine_Dispatch REFERENCES dbo.DispatchOrder (Id),
    TripDate DATETIME2 NOT NULL,
    DispatchCode NVARCHAR(50) NOT NULL,
    Route NVARCHAR(255) NULL,
    PlateNumber NVARCHAR(50) NULL,
    Tonnage NVARCHAR(50) NULL,
    DriverName NVARCHAR(255) NULL,
    UnitPrice DECIMAL(20,2) NOT NULL,
    Surcharge DECIMAL(20,2) NOT NULL,
    ExtraCost DECIMAL(20,2) NOT NULL,
    LineTotal DECIMAL(20,2) NOT NULL,
    Notes NVARCHAR(255) NULL,
    LegacyId INT NULL
);

CREATE TABLE dbo.ChangeLog (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChangeLog PRIMARY KEY,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    Summary NVARCHAR(500) NULL,
    OldJson NVARCHAR(MAX) NULL,
    NewJson NVARCHAR(MAX) NULL,
    UserId INT NULL CONSTRAINT FK_ChangeLog_User REFERENCES dbo.AppUser (Id),
    ChangedAt DATETIME2 NOT NULL
);

CREATE INDEX IX_ChangeLog_Entity ON dbo.ChangeLog (EntityName, EntityId);

CREATE TABLE dbo.CashReceipt (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashReceipt PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    DocumentDate DATETIME2 NOT NULL,
    Kind INT NOT NULL,
    DispatchOrderId INT NULL CONSTRAINT FK_CashReceipt_Dispatch REFERENCES dbo.DispatchOrder (Id),
    CustomerId INT NULL CONSTRAINT FK_CashReceipt_Customer REFERENCES dbo.Customer (Id),
    Amount DECIMAL(18,0) NOT NULL,
    AmountInWords NVARCHAR(500) NULL,
    PayerName NVARCHAR(255) NULL,
    Address NVARCHAR(255) NULL,
    Reason NVARCHAR(255) NULL,
    EmployeeId INT NULL CONSTRAINT FK_CashReceipt_Employee REFERENCES dbo.Employee (Id),
    LegacyId INT NULL
);

CREATE TABLE dbo.CashPayment (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashPayment PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    DocumentDate DATETIME2 NOT NULL,
    Kind INT NOT NULL,
    CustomerId INT NULL CONSTRAINT FK_CashPayment_Customer REFERENCES dbo.Customer (Id),
    DriverEmployeeId INT NULL CONSTRAINT FK_CashPayment_Driver REFERENCES dbo.Employee (Id),
    Amount DECIMAL(18,0) NOT NULL,
    AmountInWords NVARCHAR(500) NULL,
    PayeeName NVARCHAR(255) NULL,
    Address NVARCHAR(255) NULL,
    Reason NVARCHAR(255) NULL,
    EmployeeId INT NULL CONSTRAINT FK_CashPayment_Employee REFERENCES dbo.Employee (Id),
    LegacyId INT NULL
);

CREATE TABLE dbo.VatInvoice (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VatInvoice PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    InvoiceDate DATETIME2 NOT NULL,
    CustomerId INT NULL CONSTRAINT FK_VatInvoice_Customer REFERENCES dbo.Customer (Id),
    EmployeeId INT NULL CONSTRAINT FK_VatInvoice_Employee REFERENCES dbo.Employee (Id),
    IsByCustomer BIT NOT NULL CONSTRAINT DF_VatInvoice_IsByCustomer DEFAULT (0),
    GoodsName NVARCHAR(255) NULL,
    PaymentMethodText NVARCHAR(50) NULL,
    Unit NVARCHAR(50) NULL,
    Quantity DECIMAL(18,2) NULL,
    Amount DECIMAL(18,0) NOT NULL CONSTRAINT DF_VatInvoice_Amount DEFAULT (0),
    VatRate DECIMAL(9,2) NOT NULL CONSTRAINT DF_VatInvoice_VatRate DEFAULT (10),
    VatAmount DECIMAL(18,0) NOT NULL CONSTRAINT DF_VatInvoice_VatAmount DEFAULT (0),
    TotalAmount DECIMAL(18,0) NOT NULL CONSTRAINT DF_VatInvoice_Total DEFAULT (0),
    AmountInWords NVARCHAR(500) NULL,
    LegacyId INT NULL
);

CREATE TABLE dbo.VatInvoiceLine (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VatInvoiceLine PRIMARY KEY,
    VatInvoiceId INT NOT NULL CONSTRAINT FK_VatInvoiceLine_Invoice REFERENCES dbo.VatInvoice (Id) ON DELETE CASCADE,
    DispatchOrderId INT NOT NULL CONSTRAINT FK_VatInvoiceLine_Dispatch REFERENCES dbo.DispatchOrder (Id),
    LegacyId INT NULL
);

CREATE TABLE dbo.DocumentSequence (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentSequence PRIMARY KEY,
    [Key] NVARCHAR(50) NOT NULL,
    LastValue INT NOT NULL CONSTRAINT DF_DocumentSequence_LastValue DEFAULT (0),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_DocumentSequence_Key UNIQUE ([Key])
);

CREATE TABLE dbo.SystemParameter (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemParameter PRIMARY KEY,
    [Key] NVARCHAR(50) NOT NULL,
    [Value] NVARCHAR(255) NULL,
    CONSTRAINT UQ_SystemParameter_Key UNIQUE ([Key])
);

CREATE TABLE dbo.Company (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Company PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    Phone NVARCHAR(50) NULL,
    TaxCode NVARCHAR(50) NULL,
    Bank NVARCHAR(255) NULL,
    Website NVARCHAR(255) NULL,
    Email NVARCHAR(255) NULL
);
GO
