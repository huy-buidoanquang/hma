using Hma.Application.Features.Routes;
using Hma.Application.Features.Reconciliation;
using Hma.Application.Common.Persistence;
using Hma.Application.Features.Authentication;
using Hma.Application.Features.TransportExceptions;
using Hma.Application.Features.Reporting;
using Hma.Application.Common.Formatting;
using Hma.Application.Features.Pricing;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Statements;
using Hma.Application.Common.Security;
using Hma.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hma.Infrastructure.SqlServer.Tests;

public class SqlServerWorkflowTests
{
    [Fact]
    public async Task Core_operational_workflow_uses_application_contracts_end_to_end()
    {
        var options = CreateOptions();
        var storageRoot = Path.Combine(Path.GetTempPath(), "HmaIntegration", Guid.NewGuid().ToString("N"));
        try
        {
            var hasher = new Pbkdf2PasswordHasher();
            AppUser maker;
            AppUser reviewer;
            Customer customer;
            Partner partner;
            Route route;
            VehicleType vehicleType;
            Vehicle vehicle;
            Driver driver;
            PaymentMethod paymentMethod;
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();
                maker = new AppUser
                {
                    UserName = "workflow-maker",
                    DisplayName = "Người lập",
                    PasswordHash = hasher.Hash("test-password"),
                    IsManager = true,
                };
                reviewer = new AppUser
                {
                    UserName = "workflow-reviewer",
                    DisplayName = "Người duyệt",
                    PasswordHash = hasher.Hash("test-password"),
                    IsManager = true,
                };
                customer = new Customer { Code = "WF-CUSTOMER", Name = "Khách quy trình" };
                partner = new Partner { Code = "WF-PARTNER", Name = "Đối tác quy trình", OperatingFeePercent = 10 };
                vehicleType = new VehicleType { Code = "WF-TRUCK", Name = "Xe 5 tấn", Tonnage = 5 };
                var location = new Location { Code = "WF-LOCATION", Name = "Kho lấy hàng" };
                var delivery = new Location { Code = "WF-DELIVERY", Name = "Kho giao hàng" };
                route = new Route { Code = "WF-ROUTE", Name = "Tuyến quy trình" };
                route.Stops.Add(new RouteStop { Sequence = 0, Location = location });
                route.Stops.Add(new RouteStop { Sequence = 1, Location = delivery });
                vehicle = new Vehicle { PlateNumber = "29C-99999", Partner = partner, VehicleType = vehicleType, Tonnage = 5 };
                driver = new Driver { Code = "WF-DRIVER", Name = "Tài xế quy trình", Partner = partner };
                paymentMethod = new PaymentMethod { Code = PaymentMethodCodes.Credit, Name = "Trả sau" };
                setup.AddRange(maker, reviewer, customer, route, vehicle, driver, paymentMethod);
                setup.Parameters.Add(new SystemParameter { Key = "DocumentStorePath", Value = storageRoot });
                setup.Sequences.AddRange(
                    new DocumentSequence { Key = "dispatch-order", LastValue = 0 },
                    new DocumentSequence { Key = "freight-statement", LastValue = 0 });
                await setup.SaveChangesAsync();
            }

            await using var db = new HmaDbContext(options);
            var authenticated = await new AuthService(db, hasher, TimeProvider.System)
                .AuthenticateAsync("workflow-maker", "test-password");
            Assert.NotNull(authenticated);
            Assert.Equal(maker.Id, authenticated.Id);

            var current = new MutableCurrentUser(maker);
            var cityService = new CityService(db, current);
            await cityService.SaveAsync(new SaveCatalogItemCommand(0, "WF-CITY", "Thành phố quy trình", null));
            var city = Assert.Single(await cityService.ListAsync());
            await cityService.SaveAsync(new SaveCatalogItemCommand(city.Id, city.Code, "Thành phố đã sửa", null));
            Assert.Equal("Thành phố đã sửa", Assert.Single(await cityService.ListAsync()).Name);
            await cityService.DeleteAsync(city.Id);
            Assert.Empty(await cityService.ListAsync());

            var priceLists = new PriceListService(db, current, TimeProvider.System);
            var priceList = await priceLists.SaveAsync(new SavePriceListCommand(
                0, "WF-PRICE", "Bảng giá quy trình", null, customer.Id,
                new DateTime(2026, 9, 1), null, false, null, []));
            var revisionId = await priceLists.EnsureRevisionAsync(priceList.Id);
            await priceLists.AddItemAsync(new AddPriceListItemCommand(
                revisionId, route.Id, null, vehicleType.Id, 1_000_000, 100_000));
            var quote = await priceLists.GetFreightAsync(
                customer.Id, route.Id, vehicleType.Id, new DateTime(2026, 9, 12));
            Assert.NotNull(quote);
            Assert.Equal(1_000_000, quote.UnitPrice);

            var numbers = new DocumentNumberService(db);
            var log = new ChangeLogService(db, current, TimeProvider.System);
            var partnerRates = new PartnerRateService(db, current, TimeProvider.System);
            await partnerRates.SaveAsync(new SavePartnerRateCommand(
                0, partner.Id, route.Id, vehicleType.Id, new DateTime(2026, 9, 1), null,
                700_000, 50_000, []));
            var editor = new DispatchOrderEditorService(
                db, numbers, priceLists, partnerRates, current, log, TimeProvider.System);
            var draft = await editor.CreateNewAsync();
            var header = draft.Header with
            {
                PickupAt = new DateTime(2026, 9, 12, 8, 0, 0),
                Customer = new Hma.Application.Features.Customers.CustomerOption(
                    customer.Id, customer.Code, customer.Name, null, null, null, false),
                RouteId = route.Id,
                VehicleId = vehicle.Id,
                DriverId = driver.Id,
                VehicleType = new VehicleTypeOption(
                    vehicleType.Id, vehicleType.Code, vehicleType.Name, vehicleType.Tonnage),
            };
            var dispatch = draft with
            {
                Header = header,
                CustomerId = customer.Id,
                VehicleTypeId = vehicleType.Id,
                PaymentMethodId = paymentMethod.Id,
                Lines = [new DispatchOrderLineDetails(0, 1, "Hàng kiểm thử", 2, route.Name, 10, null)],
            };
            var freight = await editor.ApplyFreightAsync(new SaveDispatchOrderCommand(dispatch));
            Assert.NotNull(freight.Quote);
            await editor.SaveAsync(new SaveDispatchOrderCommand(freight.Order));

            var queries = new DispatchOrderQueryService(db);
            var saved = Assert.Single(await queries.SearchAsync(
                null, null, null, customer.Id, null, null, null, null, null));
            Assert.Equal(1_100_000, saved.TotalAmount);

            var documents = new DispatchDocumentService(db, current, new LocalDiskFileStorage(db), TimeProvider.System);
            var document = await documents.AttachAsync(
                saved.Id, DispatchDocumentKind.DeliveryNote, "delivery.txt", new MemoryStream([1, 2, 3]));
            await using (var content = await documents.OpenReadAsync(document.StoredPath))
            {
                using var copy = new MemoryStream();
                await content.CopyToAsync(copy);
                Assert.Equal(new byte[] { 1, 2, 3 }, copy.ToArray());
            }

            var disposableDocument = await documents.AttachAsync(
                saved.Id, DispatchDocumentKind.Other, "temporary.txt", new MemoryStream([9, 8, 7]));
            await documents.DeleteAsync(disposableDocument.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => documents.OpenReadAsync(disposableDocument.StoredPath));

            await editor.SetStatusAsync(saved.Id, DispatchStatus.Completed);
            var reconciliation = new DispatchReconciliationService(db, queries, current, log, TimeProvider.System);
            await reconciliation.SubmitAsync(saved.Id);
            current.User = reviewer;
            await reconciliation.ApproveAsync(saved.Id);

            var statements = new FreightStatementService(db, numbers, current, log, TimeProvider.System);
            var statement = await statements.GenerateDetailsAsync(customer.Id, 2026, 9);
            Assert.Equal(1, statement.TripCount);
            Assert.Equal(1_100_000, statement.GrandTotal);

            var reports = new ReportQueryService(db);
            Assert.Contains(await reports.PeriodDispatchAsync(
                new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)), x => x.Id == saved.Id);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
            if (Directory.Exists(storageRoot)) Directory.Delete(storageRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Failed_location_delete_does_not_poison_later_price_list_save_in_same_context()
    {
        var options = CreateOptions();
        try
        {
            int managerId;
            int locationId;
            int priceListId;
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();

                var seededManager = new AppUser
                {
                    UserName = "referential-conflict-manager",
                    PasswordHash = "test",
                    IsManager = true
                };
                var seededLocation = new Location { Code = "USED-LOCATION", Name = "Điểm đang dùng" };
                var order = new DispatchOrder
                {
                    Code = "REFERENTIAL-CONFLICT-ORDER",
                    PickupAt = new DateTime(2026, 9, 12),
                    BillingYear = 2026,
                    BillingMonth = 9
                };
                order.Stops.Add(new DispatchOrderStop
                {
                    Sequence = 0,
                    Location = seededLocation,
                    NameSnapshot = seededLocation.Name
                });
                var seededPriceList = new PriceList
                {
                    Code = "REFERENTIAL-CONFLICT-PRICE",
                    Name = "Bảng giá ban đầu"
                };
                setup.AddRange(seededManager, order, seededPriceList);
                await setup.SaveChangesAsync();
                managerId = seededManager.Id;
                locationId = seededLocation.Id;
                priceListId = seededPriceList.Id;
            }

            await using var db = new HmaDbContext(options);
            var manager = await db.Users.SingleAsync(x => x.Id == managerId);
            var priceList = await db.PriceLists.SingleAsync(x => x.Id == priceListId);

            var current = new MutableCurrentUser(manager);
            var locationService = new LocationService(db, current);
            var priceListService = new PriceListService(db, current, TimeProvider.System);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => locationService.DeleteAsync(locationId));

            Assert.Equal(ReferentialConflict.Message, error.Message);
            var location = db.ChangeTracker.Entries<Location>().Single(x => x.Entity.Id == locationId);
            Assert.Equal(EntityState.Unchanged, location.State);

            await priceListService.SaveAsync(new SavePriceListCommand(
                priceList.Id,
                priceList.Code,
                "Bảng giá sau lỗi xóa",
                priceList.Description,
                priceList.CustomerId,
                priceList.EffectiveFrom,
                priceList.EffectiveTo,
                priceList.HasPriceFluctuation,
                priceList.LockReason,
                priceList.RowVersion));

            Assert.True(await db.Locations.AnyAsync(x => x.Id == locationId));
            Assert.Equal(
                "Bảng giá sau lỗi xóa",
                await db.PriceLists.Where(x => x.Id == priceList.Id).Select(x => x.Name).SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Report_queries_include_completed_orders_on_the_end_date()
    {
        var options = CreateOptions();
        try
        {
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var customer = new Customer { Code = "REPORT-CUSTOMER", Name = "Khách báo cáo" };
                setup.Customers.Add(customer);
                setup.DispatchOrders.Add(new DispatchOrder
                {
                    Code = "REPORT-END-DATE",
                    Customer = customer,
                    Status = DispatchStatus.Completed,
                    PickupAt = new DateTime(2026, 9, 11, 20, 30, 0),
                    BillingYear = 2026,
                    BillingMonth = 9,
                    UnitPrice = 1_000_000,
                    TotalAmount = 1_000_000
                });
                await setup.SaveChangesAsync();
            }

            await using var verify = new HmaDbContext(options);
            var dashboard = new DashboardQueryService(verify);
            var reports = new ReportQueryService(verify);
            var from = new DateTime(2026, 8, 11);
            var to = new DateTime(2026, 9, 11);

            var byCustomer = await dashboard.ByCustomerAsync(from, to);
            var byVehicle = await dashboard.ByVehicleAsync(from, to);
            var periodOrders = await reports.PeriodDispatchAsync(from, to);

            Assert.Collection(byCustomer, row =>
            {
                Assert.Equal("Khách báo cáo", row.Customer);
                Assert.Equal(1, row.Trips);
                Assert.Equal(1_000_000, row.Freight);
            });
            Assert.Collection(byVehicle, row => Assert.Equal(1, row.Trips));
            Assert.Collection(periodOrders, order => Assert.Equal("REPORT-END-DATE", order.Code));
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Seed_adds_missing_payment_methods_to_an_existing_catalog()
    {
        var options = CreateOptions();
        try
        {
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();
                setup.Users.Add(new AppUser
                {
                    UserName = "seed-test",
                    PasswordHash = "test",
                    IsManager = true
                });
                setup.PaymentMethods.Add(new PaymentMethod
                {
                    Code = PaymentMethodCodes.Credit,
                    Name = "Trả sau"
                });
                await setup.SaveChangesAsync();

                await DatabaseSeeder.SeedAsync(setup);
                await DatabaseSeeder.SeedAsync(setup);
            }

            await using var verify = new HmaDbContext(options);
            var methods = await verify.PaymentMethods.OrderBy(x => x.Code).ToListAsync();
            Assert.Equal(3, methods.Count);
            Assert.Contains(methods, x => x.Code == PaymentMethodCodes.Credit && x.Name == "Trả sau");
            Assert.Contains(methods, x => x.Code == PaymentMethodCodes.DriverCollect && x.Name == "Lái xe thu");
            Assert.Contains(methods, x => x.Code == PaymentMethodCodes.DispatcherCollect && x.Name == "Điều hành thu");
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task RowVersion_rejects_a_lost_sequence_update()
    {
        var options = CreateOptions();
        try
        {
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();
                setup.Sequences.Add(new DocumentSequence { Key = "concurrency-test", LastValue = 0 });
                await setup.SaveChangesAsync();
            }

            await using var first = new HmaDbContext(options);
            await using var second = new HmaDbContext(options);
            var firstValue = await first.Sequences.SingleAsync();
            var staleValue = await second.Sequences.SingleAsync();

            firstValue.LastValue++;
            await first.SaveChangesAsync();
            staleValue.LastValue++;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Approved_transport_exception_updates_order_and_audit_atomically()
    {
        var options = CreateOptions();
        try
        {
            int orderId;
            int codeId;
            AppUser maker;
            AppUser reviewer;
            await using (var setup = new HmaDbContext(options))
            {
                await setup.Database.MigrateAsync();
                maker = new AppUser { UserName = "maker", PasswordHash = "test", IsManager = true };
                reviewer = new AppUser { UserName = "reviewer", PasswordHash = "test", IsManager = true };
                var partner = new Partner { Code = "P01", Name = "Nhà xe 01", OperatingFeePercent = 10 };
                var code = new TransportExceptionCode { Code = "WAIT", Name = "Chờ bốc hàng" };
                setup.AddRange(maker, reviewer, partner, code);
                await setup.SaveChangesAsync();

                var order = new DispatchOrder
                {
                    Code = "DO-001",
                    Status = DispatchStatus.Completed,
                    PartnerId = partner.Id,
                    PartnerNameSnapshot = partner.Name,
                    UnitPrice = 1_000_000,
                    BuyUnitPrice = 700_000,
                    PartnerOperatingFeePercent = 10,
                    BillingYear = 2026,
                    BillingMonth = 9
                };
                order.RecalculateTotal();
                order.RecalculatePartnerAmounts();
                setup.DispatchOrders.Add(order);
                await setup.SaveChangesAsync();
                orderId = order.Id;
                codeId = code.Id;
            }

            await using (var db = new HmaDbContext(options))
            {
                var current = new MutableCurrentUser(maker);
                var log = new ChangeLogService(db, current, TimeProvider.System);
                var service = new TransportExceptionService(db, current, log, TimeProvider.System);
                var itemId = await service.SaveAsync(new SaveTransportExceptionCommand(
                    0, orderId, codeId, DateTime.Now, "Chờ bốc hàng quá thời gian", 100_000, 50_000, []));
                await service.SubmitAsync(itemId);
                current.User = reviewer;
                await service.ApproveAsync(itemId);
            }

            await using (var verify = new HmaDbContext(options))
            {
                var order = await verify.DispatchOrders.SingleAsync(x => x.Id == orderId);
                var item = await verify.TransportExceptions.SingleAsync();
                Assert.Equal(TransportExceptionStatus.Approved, item.Status);
                Assert.Equal(100_000, order.ApprovedExceptionRevenue);
                Assert.Equal(50_000, order.ApprovedExceptionCost);
                Assert.Equal(1_100_000, order.TotalAmount);
                Assert.Equal(AmountText.From(1_100_000), order.AmountInWords);
                Assert.Equal(750_000, order.BuyTotal);
                Assert.Equal(675_000, order.PartnerPayableAmount);
                Assert.Equal(425_000, order.GrossMargin);
                Assert.Equal(4, await verify.ChangeLogs.CountAsync());
            }
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Partner_commercial_migration_backfills_an_existing_trip()
    {
        var options = CreateOptions();
        try
        {
            await using (var setup = new HmaDbContext(options))
            {
                var migrator = setup.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260911010750_AddFreightPriceTraceability");
                await setup.Database.ExecuteSqlRawAsync("""
                    INSERT INTO Partner (Code, Name, OperatingFeePercent)
                    VALUES (N'P-LEGACY', N'Nhà xe legacy', 10);
                    DECLARE @PartnerId int = SCOPE_IDENTITY();

                    INSERT INTO Vehicle (PlateNumber, PartnerId)
                    VALUES (N'29C-12345', @PartnerId);
                    DECLARE @VehicleId int = SCOPE_IDENTITY();

                    INSERT INTO DispatchOrder
                        (Code, CreatedAt, Status, ReconciliationStatus, PickupAt,
                         VehicleId, BillingYear, BillingMonth, UnitPrice, Surcharge,
                         ExtraCost, TotalAmount, IsDeleted, IsFreightManual,
                         FreightOverrideReason)
                    VALUES
                        (N'DO-LEGACY', '2026-09-01', 2, 0, '2026-09-01',
                         @VehicleId, 2026, 9, 1000000, 100000,
                         50000, 1150000, 0, 1,
                         N'Dữ liệu kiểm thử nâng cấp');
                    """);

                await migrator.MigrateAsync();
            }

            await using var verify = new HmaDbContext(options);
            var order = await verify.DispatchOrders.SingleAsync(x => x.Code == "DO-LEGACY");
            Assert.NotNull(order.PartnerId);
            Assert.Equal("Nhà xe legacy", order.PartnerNameSnapshot);
            Assert.Equal(1_000_000, order.BuyUnitPrice);
            Assert.Equal(1_150_000, order.BuyTotal);
            Assert.Equal(1_035_000, order.PartnerPayableAmount);
            Assert.Equal(115_000, order.GrossMargin);
            Assert.True(order.IsBuyManual);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    private static DbContextOptions<HmaDbContext> CreateOptions()
    {
        var database = $"HmaIntegration_{Guid.NewGuid():N}";
        var connection = Environment.GetEnvironmentVariable("HMA_TEST_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";
        var builder = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = database,
            TrustServerCertificate = true
        };
        return new DbContextOptionsBuilder<HmaDbContext>().UseSqlServer(builder.ConnectionString).Options;
    }

    private static async Task DeleteDatabaseAsync(DbContextOptions<HmaDbContext> options)
    {
        await using var cleanup = new HmaDbContext(options);
        await cleanup.Database.EnsureDeletedAsync();
    }
}
