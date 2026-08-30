USE Hma;
GO
SET NOCOUNT ON;

SELECT 'Customer' AS TableName, COUNT(*) AS RowCount FROM dbo.Customer
UNION ALL SELECT 'Employee', COUNT(*) FROM dbo.Employee
UNION ALL SELECT 'Partner', COUNT(*) FROM dbo.Partner
UNION ALL SELECT 'Driver', COUNT(*) FROM dbo.Driver
UNION ALL SELECT 'Vehicle', COUNT(*) FROM dbo.Vehicle
UNION ALL SELECT 'City', COUNT(*) FROM dbo.City
UNION ALL SELECT 'PriceListItem', COUNT(*) FROM dbo.PriceListItem
UNION ALL SELECT 'DispatchOrder', COUNT(*) FROM dbo.DispatchOrder
UNION ALL SELECT 'DispatchOrderLine', COUNT(*) FROM dbo.DispatchOrderLine
UNION ALL SELECT 'AppUser', COUNT(*) FROM dbo.AppUser;

SELECT SUM(TotalAmount) AS DispatchTotal FROM dbo.DispatchOrder;
SELECT SUM(UnitPrice + ExtraCost) AS FreightPlusExtra FROM dbo.DispatchOrder;

SELECT [Key], LastValue FROM dbo.DocumentSequence;

SELECT COUNT(*) AS DispatchMissingCustomer
FROM dbo.DispatchOrder d
WHERE d.CustomerId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Customer c WHERE c.Id = d.CustomerId);
GO
