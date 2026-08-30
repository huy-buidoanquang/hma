USE Hma;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.VehicleType)
BEGIN
    SET IDENTITY_INSERT dbo.VehicleType ON;
    INSERT INTO dbo.VehicleType (Id, Code, Name, Tonnage) VALUES
        (1, N'1.25T', N'Xe 1.25 tấn', 1.25),
        (2, N'3.5T',  N'Xe 3.5 tấn',  3.50),
        (3, N'5T',    N'Xe 5 tấn',    5.00),
        (4, N'1.45T', N'Xe 1.45 tấn', 1.45),
        (5, N'8T',    N'Xe 8 tấn',    8.00),
        (6, N'2.5T',  N'Xe 2.5 tấn',  2.50),
        (7, N'10T',   N'Xe 10 tấn',  10.00),
        (8, N'15T',   N'Xe 15 tấn',  15.00);
    SET IDENTITY_INSERT dbo.VehicleType OFF;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Partner)
BEGIN
    INSERT INTO dbo.Partner (Code, Name) VALUES (N'UNASSIGNED', N'Chưa gán đối tác');
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppScreen)
BEGIN
    INSERT INTO dbo.AppScreen ([Key], Name) VALUES
        (N'customers', N'Khách hàng'),
        (N'partners', N'Đối tác'),
        (N'drivers', N'Tài xế'),
        (N'vehicles', N'Xe'),
        (N'employees', N'Nhân viên'),
        (N'cities', N'Thành phố / hành trình'),
        (N'departments', N'Phòng ban'),
        (N'job-titles', N'Chức vụ'),
        (N'price-lists', N'Bảng giá'),
        (N'dispatch-orders', N'Lệnh điều xe'),
        (N'reconcile', N'Đối soát'),
        (N'statements', N'Bảng kê tháng'),
        (N'lookup', N'Tra cứu chuyến'),
        (N'dashboard', N'Dashboard'),
        (N'reports', N'Báo cáo'),
        (N'users', N'Người dùng'),
        (N'settings', N'Tham số hệ thống');
END

IF NOT EXISTS (SELECT 1 FROM dbo.DocumentSequence)
BEGIN
    INSERT INTO dbo.DocumentSequence ([Key], LastValue) VALUES
        (N'dispatch-order', 0),
        (N'freight-statement', 0),
        (N'walk-in-customer', 0),
        (N'cash-receipt', 0),
        (N'cash-payment', 0),
        (N'vat-invoice', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.SystemParameter)
BEGIN
    INSERT INTO dbo.SystemParameter ([Key], [Value]) VALUES
        (N'VatRate', N'10'),
        (N'DocumentStorePath', N''),
        (N'SchemaVersion', N'legacy-ux-1');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Company)
BEGIN
    INSERT INTO dbo.Company (Name, Address)
    VALUES (N'Công ty TNHH dịch vụ vận tải và thương mại Hà Minh Anh', NULL);
END
GO
