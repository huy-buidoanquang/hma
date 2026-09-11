USE Hma;
GO
SET NOCOUNT ON;

SELECT 'Customer' AS TableName, COUNT(*) AS RowCount FROM dbo.Customer
UNION ALL SELECT 'Employee', COUNT(*) FROM dbo.Employee
UNION ALL SELECT 'Partner', COUNT(*) FROM dbo.Partner
UNION ALL SELECT 'Driver', COUNT(*) FROM dbo.Driver
UNION ALL SELECT 'Vehicle', COUNT(*) FROM dbo.Vehicle
UNION ALL SELECT 'City', COUNT(*) FROM dbo.City
UNION ALL SELECT 'PaymentMethod', COUNT(*) FROM dbo.PaymentMethod
UNION ALL SELECT 'PriceList', COUNT(*) FROM dbo.PriceList
UNION ALL SELECT 'PriceListRevision', COUNT(*) FROM dbo.PriceListRevision
UNION ALL SELECT 'PriceListItem', COUNT(*) FROM dbo.PriceListItem
UNION ALL SELECT 'DispatchOrder', COUNT(*) FROM dbo.DispatchOrder
UNION ALL SELECT 'DispatchOrderLine', COUNT(*) FROM dbo.DispatchOrderLine
UNION ALL SELECT 'AppUser', COUNT(*) FROM dbo.AppUser;

SELECT
    (SELECT COUNT(*) FROM LEGACY.DHXE.dbo.khachhang) AS LegacyCustomers,
    (SELECT COUNT(*) FROM dbo.Customer WHERE LegacyId IS NOT NULL) AS MigratedCustomers,
    (SELECT COUNT(*) FROM LEGACY.DHXE.dbo.nil) AS LegacyDispatchOrders,
    (SELECT COUNT(*) FROM dbo.DispatchOrder WHERE LegacyId IS NOT NULL) AS MigratedDispatchOrders,
    (SELECT COUNT(*) FROM LEGACY.DHXE.dbo.nil_ct) AS LegacyDispatchLines,
    (SELECT COUNT(*) FROM dbo.DispatchOrderLine WHERE LegacyId IS NOT NULL) AS MigratedDispatchLines;

SELECT
    CAST((SELECT SUM(CAST(ISNULL(tongthu, ISNULL(cuocdv, 0) + ISNULL(thukhac, 0)) AS DECIMAL(38,2)))
          FROM LEGACY.DHXE.dbo.nil) AS DECIMAL(38,2)) AS LegacyDispatchTotal,
    CAST((SELECT SUM(TotalAmount) FROM dbo.DispatchOrder WHERE LegacyId IS NOT NULL) AS DECIMAL(38,2)) AS MigratedDispatchTotal;

SELECT [Key], LastValue FROM dbo.DocumentSequence ORDER BY [Key];

SELECT 'DispatchMissingCustomer' AS CheckName, COUNT(*) AS ErrorCount
FROM dbo.DispatchOrder d WHERE d.CustomerId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Customer c WHERE c.Id = d.CustomerId)
UNION ALL SELECT 'DispatchMissingRoute', COUNT(*)
FROM dbo.DispatchOrder d WHERE d.RouteId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Route r WHERE r.Id = d.RouteId)
UNION ALL SELECT 'DispatchMissingVehicle', COUNT(*)
FROM dbo.DispatchOrder d WHERE d.VehicleId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Vehicle v WHERE v.Id = d.VehicleId)
UNION ALL SELECT 'DispatchMissingDriver', COUNT(*)
FROM dbo.DispatchOrder d WHERE d.DriverId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Driver x WHERE x.Id = d.DriverId)
UNION ALL SELECT 'DispatchMissingPaymentMethod', COUNT(*)
FROM dbo.DispatchOrder d
INNER JOIN LEGACY.DHXE.dbo.nil n ON n.nil_id = d.LegacyId
WHERE n.hinhthuc_tt_id IS NOT NULL AND d.PaymentMethodId IS NULL
UNION ALL SELECT 'DispatchInvalidBillingPeriod', COUNT(*)
FROM dbo.DispatchOrder WHERE BillingYear < 2000 OR BillingMonth NOT BETWEEN 1 AND 12
UNION ALL SELECT 'DispatchDuplicateCode', COUNT(*)
FROM (SELECT Code FROM dbo.DispatchOrder GROUP BY Code HAVING COUNT(*) > 1) d
UNION ALL SELECT 'PriceDuplicateRouteVehicle', COUNT(*)
FROM (SELECT PriceListRevisionId, RouteId, VehicleTypeId FROM dbo.PriceListItem WHERE RouteId IS NOT NULL GROUP BY PriceListRevisionId, RouteId, VehicleTypeId HAVING COUNT(*) > 1) p
UNION ALL SELECT 'PriceDuplicateDestinationVehicle', COUNT(*)
FROM (SELECT PriceListRevisionId, DeliveryLocationId, VehicleTypeId FROM dbo.PriceListItem WHERE RouteId IS NULL GROUP BY PriceListRevisionId, DeliveryLocationId, VehicleTypeId HAVING COUNT(*) > 1) p
UNION ALL SELECT 'RouteMissingTwoStops', COUNT(*)
FROM dbo.Route r WHERE (SELECT COUNT(*) FROM dbo.RouteStop s WHERE s.RouteId = r.Id) < 2
UNION ALL SELECT 'ImportedUsersPendingAdminReset', COUNT(*)
FROM dbo.AppUser WHERE PasswordHash = N'DISABLED';

DECLARE @LegacyOrderCount BIGINT = (SELECT COUNT(*) FROM LEGACY.DHXE.dbo.nil);
DECLARE @TargetOrderCount BIGINT = (SELECT COUNT(*) FROM dbo.DispatchOrder WHERE LegacyId IS NOT NULL);
DECLARE @LegacyLineCount BIGINT = (SELECT COUNT(*) FROM LEGACY.DHXE.dbo.nil_ct);
DECLARE @TargetLineCount BIGINT = (SELECT COUNT(*) FROM dbo.DispatchOrderLine WHERE LegacyId IS NOT NULL);
DECLARE @LegacyTotal DECIMAL(38,2) = (SELECT SUM(CAST(ISNULL(tongthu, ISNULL(cuocdv, 0) + ISNULL(thukhac, 0)) AS DECIMAL(38,2))) FROM LEGACY.DHXE.dbo.nil);
DECLARE @TargetTotal DECIMAL(38,2) = (SELECT SUM(TotalAmount) FROM dbo.DispatchOrder WHERE LegacyId IS NOT NULL);

IF @LegacyOrderCount <> @TargetOrderCount
    THROW 52001, N'ETL validation failed: số lệnh legacy và Hma không khớp.', 1;
IF @LegacyLineCount <> @TargetLineCount
    THROW 52002, N'ETL validation failed: số dòng hàng legacy và Hma không khớp.', 1;
IF ISNULL(@LegacyTotal, 0) <> ISNULL(@TargetTotal, 0)
    THROW 52003, N'ETL validation failed: tổng tiền lệnh legacy và Hma không khớp.', 1;
IF EXISTS (SELECT 1 FROM dbo.DispatchOrder WHERE BillingYear < 2000 OR BillingMonth NOT BETWEEN 1 AND 12)
    THROW 52004, N'ETL validation failed: lệnh có kỳ kế toán không hợp lệ.', 1;
IF EXISTS (SELECT Code FROM dbo.DispatchOrder GROUP BY Code HAVING COUNT(*) > 1)
    THROW 52005, N'ETL validation failed: số lệnh bị trùng.', 1;
IF EXISTS (SELECT 1 FROM dbo.DispatchOrder WHERE RouteId IS NULL OR VehicleId IS NULL OR DriverId IS NULL)
    THROW 52006, N'ETL validation failed: lệnh thiếu tuyến, xe hoặc tài xế.', 1;
IF EXISTS (
    SELECT 1
    FROM dbo.DispatchOrder d
    INNER JOIN LEGACY.DHXE.dbo.nil n ON n.nil_id = d.LegacyId
    WHERE n.hinhthuc_tt_id IS NOT NULL AND d.PaymentMethodId IS NULL)
    THROW 52007, N'ETL validation failed: lệnh có hình thức thanh toán legacy nhưng chưa ánh xạ.', 1;
GO
