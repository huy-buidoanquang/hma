using Hma.Application.Services;
using Hma.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hma.Infrastructure.SqlServer.Tests;

public class SqlServerWorkflowTests
{
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

            var current = new CurrentUser { User = manager };
            var locationService = new LocationService(db, current);
            var priceListService = new PriceListService(db, current);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => locationService.DeleteAsync(locationId));

            Assert.Equal(ReferentialConflict.Message, error.Message);
            var location = db.ChangeTracker.Entries<Location>().Single(x => x.Entity.Id == locationId);
            Assert.Equal(EntityState.Unchanged, location.State);

            priceList.Name = "Bảng giá sau lỗi xóa";
            await priceListService.SaveAsync(priceList);

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
                var current = new CurrentUser { User = maker };
                var log = new ChangeLogService(db, current);
                var service = new TransportExceptionService(db, current, log);
                var item = new TransportException
                {
                    DispatchOrderId = orderId,
                    TransportExceptionCodeId = codeId,
                    Description = "Chờ bốc hàng quá thời gian",
                    CustomerCharge = 100_000,
                    PartnerCost = 50_000
                };

                await service.SaveAsync(item);
                await service.SubmitAsync(item.Id);
                current.User = reviewer;
                await service.ApproveAsync(item.Id);
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
