using Hma.Application.Abstractions.Persistence;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hma.Infrastructure.SqlServer;

public sealed class HmaDbContext(DbContextOptions<HmaDbContext> options) : DbContext(options), IHmaDbContext
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<LocationAlias> LocationAliases => Set<LocationAlias>();
    public DbSet<RouteAlias> RouteAliases => Set<RouteAlias>();
    public DbSet<CustomerAlias> CustomerAliases => Set<CustomerAlias>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAlias> VehicleAliases => Set<VehicleAlias>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListRevision> PriceListRevisions => Set<PriceListRevision>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<PriceListFluctuation> PriceListFluctuations => Set<PriceListFluctuation>();
    public DbSet<PartnerRate> PartnerRates => Set<PartnerRate>();
    public DbSet<PartnerSettlement> PartnerSettlements => Set<PartnerSettlement>();
    public DbSet<PartnerSettlementLine> PartnerSettlementLines => Set<PartnerSettlementLine>();
    public DbSet<TransportExceptionCode> TransportExceptionCodes => Set<TransportExceptionCode>();
    public DbSet<TransportException> TransportExceptions => Set<TransportException>();
    public DbSet<DispatchOrder> DispatchOrders => Set<DispatchOrder>();
    public DbSet<DispatchOrderStop> DispatchOrderStops => Set<DispatchOrderStop>();
    public DbSet<DispatchOrderLine> DispatchOrderLines => Set<DispatchOrderLine>();
    public DbSet<DispatchDocument> DispatchDocuments => Set<DispatchDocument>();
    public DbSet<FreightStatement> FreightStatements => Set<FreightStatement>();
    public DbSet<FreightStatementLine> FreightStatementLines => Set<FreightStatementLine>();
    public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
    public DbSet<CashReceipt> CashReceipts => Set<CashReceipt>();
    public DbSet<CashPayment> CashPayments => Set<CashPayment>();
    public DbSet<VatInvoice> VatInvoices => Set<VatInvoice>();
    public DbSet<VatInvoiceLine> VatInvoiceLines => Set<VatInvoiceLine>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppScreen> Screens => Set<AppScreen>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<DocumentSequence> Sequences => Set<DocumentSequence>();
    public DbSet<SystemParameter> Parameters => Set<SystemParameter>();
    public DbSet<Company> Companies => Set<Company>();

    IQueryable<City> IHmaDbContext.Cities => Cities;
    IQueryable<Location> IHmaDbContext.Locations => Locations;
    IQueryable<Route> IHmaDbContext.Routes => Routes;
    IQueryable<RouteStop> IHmaDbContext.RouteStops => RouteStops;
    IQueryable<LocationAlias> IHmaDbContext.LocationAliases => LocationAliases;
    IQueryable<RouteAlias> IHmaDbContext.RouteAliases => RouteAliases;
    IQueryable<CustomerAlias> IHmaDbContext.CustomerAliases => CustomerAliases;
    IQueryable<Department> IHmaDbContext.Departments => Departments;
    IQueryable<JobTitle> IHmaDbContext.JobTitles => JobTitles;
    IQueryable<VehicleType> IHmaDbContext.VehicleTypes => VehicleTypes;
    IQueryable<PaymentMethod> IHmaDbContext.PaymentMethods => PaymentMethods;
    IQueryable<Employee> IHmaDbContext.Employees => Employees;
    IQueryable<Partner> IHmaDbContext.Partners => Partners;
    IQueryable<Driver> IHmaDbContext.Drivers => Drivers;
    IQueryable<Vehicle> IHmaDbContext.Vehicles => Vehicles;
    IQueryable<VehicleAlias> IHmaDbContext.VehicleAliases => VehicleAliases;
    IQueryable<Customer> IHmaDbContext.Customers => Customers;
    IQueryable<PriceList> IHmaDbContext.PriceLists => PriceLists;
    IQueryable<PriceListRevision> IHmaDbContext.PriceListRevisions => PriceListRevisions;
    IQueryable<PriceListItem> IHmaDbContext.PriceListItems => PriceListItems;
    IQueryable<PriceListFluctuation> IHmaDbContext.PriceListFluctuations => PriceListFluctuations;
    IQueryable<PartnerRate> IHmaDbContext.PartnerRates => PartnerRates;
    IQueryable<PartnerSettlement> IHmaDbContext.PartnerSettlements => PartnerSettlements;
    IQueryable<PartnerSettlementLine> IHmaDbContext.PartnerSettlementLines => PartnerSettlementLines;
    IQueryable<TransportExceptionCode> IHmaDbContext.TransportExceptionCodes => TransportExceptionCodes;
    IQueryable<TransportException> IHmaDbContext.TransportExceptions => TransportExceptions;
    IQueryable<DispatchOrder> IHmaDbContext.DispatchOrders => DispatchOrders;
    IQueryable<DispatchOrderStop> IHmaDbContext.DispatchOrderStops => DispatchOrderStops;
    IQueryable<DispatchOrderLine> IHmaDbContext.DispatchOrderLines => DispatchOrderLines;
    IQueryable<DispatchDocument> IHmaDbContext.DispatchDocuments => DispatchDocuments;
    IQueryable<FreightStatement> IHmaDbContext.FreightStatements => FreightStatements;
    IQueryable<FreightStatementLine> IHmaDbContext.FreightStatementLines => FreightStatementLines;
    IQueryable<ChangeLog> IHmaDbContext.ChangeLogs => ChangeLogs;
    IQueryable<CashReceipt> IHmaDbContext.CashReceipts => CashReceipts;
    IQueryable<CashPayment> IHmaDbContext.CashPayments => CashPayments;
    IQueryable<VatInvoice> IHmaDbContext.VatInvoices => VatInvoices;
    IQueryable<VatInvoiceLine> IHmaDbContext.VatInvoiceLines => VatInvoiceLines;
    IQueryable<AppUser> IHmaDbContext.Users => Users;
    IQueryable<AppScreen> IHmaDbContext.Screens => Screens;
    IQueryable<UserPermission> IHmaDbContext.UserPermissions => UserPermissions;
    IQueryable<DocumentSequence> IHmaDbContext.Sequences => Sequences;
    IQueryable<SystemParameter> IHmaDbContext.Parameters => Parameters;
    IQueryable<Company> IHmaDbContext.Companies => Companies;

    void IHmaDbContext.Add<T>(T entity) => Set<T>().Add(entity);
    void IHmaDbContext.Update<T>(T entity)
    {
        var tracked = FindTracked(entity);
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry;
        if (tracked is not null)
        {
            entry = Entry(tracked);
            if (!ReferenceEquals(tracked, entity))
                entry.CurrentValues.SetValues(entity);
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                entry.State = EntityState.Modified;
        }
        else
        {
            entry = Entry(entity);
            entry.State = EntityState.Modified;
        }

        StampConcurrencyOriginal(entry);
    }

    void IHmaDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    void IHmaDbContext.ApplyOriginalRowVersion<T>(T entity, byte[] originalRowVersion)
    {
        if (originalRowVersion.Length == 0)
            return;
        var target = FindTracked(entity) ?? entity;
        var entry = Entry(target);
        if (entry.State == EntityState.Detached)
            entry = Set<T>().Attach(target);
        var property = entry.Property("RowVersion");
        property.OriginalValue = originalRowVersion;
        property.CurrentValue = originalRowVersion;
        property.IsModified = false;
    }

    async Task IHmaDbContext.ReloadAsync<T>(T entity, CancellationToken cancellationToken)
    {
        await Entry(entity).ReloadAsync(cancellationToken);
    }

    async Task IHmaDbContext.ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            return;
        }

        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    async Task<T> IHmaDbContext.ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is not null)
            return await action(cancellationToken);

        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private T? FindTracked<T>(T entity) where T : class
    {
        var set = Set<T>();
        foreach (var local in set.Local)
        {
            if (ReferenceEquals(local, entity))
                return local;
        }

        var entityType = Model.FindEntityType(typeof(T));
        var pk = entityType?.FindPrimaryKey();
        if (pk is null)
            return null;

        var incoming = PrimaryKeyValues(entity, pk);
        if (incoming.All(IsUnsetKey))
            return null;

        foreach (var local in set.Local)
        {
            if (KeysEqual(incoming, PrimaryKeyValues(local, pk)))
                return local;
        }

        return null;
    }

    private static object?[] PrimaryKeyValues<T>(T entity, IKey pk) where T : class =>
        pk.Properties.Select(p => p.GetGetter().GetClrValue(entity)).ToArray();

    private static bool KeysEqual(object?[] left, object?[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (var i = 0; i < left.Length; i++)
        {
            if (!Equals(left[i], right[i]))
                return false;
        }

        return true;
    }

    private static bool IsUnsetKey(object? value) =>
        value is null or 0 or 0L or "";

    private static void StampConcurrencyOriginal(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            if (!property.Metadata.IsConcurrencyToken)
                continue;
            if (property.CurrentValue is byte[] token && token.Length > 0)
                property.OriginalValue = token;
            property.IsModified = false;
        }
    }

    Task<T?> IHmaDbContext.FindAsync<T>(int id, CancellationToken cancellationToken) where T : class =>
        Set<T>().FindAsync([id], cancellationToken).AsTask();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().ToTable("City");
        modelBuilder.Entity<Location>().ToTable("Location");
        modelBuilder.Entity<Route>().ToTable("Route");
        modelBuilder.Entity<RouteStop>().ToTable("RouteStop");
        modelBuilder.Entity<LocationAlias>().ToTable("LocationAlias");
        modelBuilder.Entity<RouteAlias>().ToTable("RouteAlias");
        modelBuilder.Entity<CustomerAlias>().ToTable("CustomerAlias");
        modelBuilder.Entity<Department>().ToTable("Department");
        modelBuilder.Entity<JobTitle>().ToTable("JobTitle");
        modelBuilder.Entity<VehicleType>().ToTable("VehicleType");
        modelBuilder.Entity<PaymentMethod>().ToTable("PaymentMethod");
        modelBuilder.Entity<Employee>().ToTable("Employee");
        modelBuilder.Entity<Partner>().ToTable("Partner");
        modelBuilder.Entity<Driver>().ToTable("Driver");
        modelBuilder.Entity<Vehicle>().ToTable("Vehicle");
        modelBuilder.Entity<VehicleAlias>().ToTable("VehicleAlias");
        modelBuilder.Entity<Customer>().ToTable("Customer");
        modelBuilder.Entity<PriceList>().ToTable("PriceList");
        modelBuilder.Entity<PriceListRevision>().ToTable("PriceListRevision");
        modelBuilder.Entity<PriceListItem>().ToTable("PriceListItem");
        modelBuilder.Entity<PriceListFluctuation>().ToTable("PriceListFluctuation");
        modelBuilder.Entity<PartnerRate>().ToTable("PartnerRate");
        modelBuilder.Entity<PartnerSettlement>().ToTable("PartnerSettlement");
        modelBuilder.Entity<PartnerSettlementLine>().ToTable("PartnerSettlementLine");
        modelBuilder.Entity<TransportExceptionCode>().ToTable("TransportExceptionCode");
        modelBuilder.Entity<TransportException>().ToTable("TransportException");
        modelBuilder.Entity<DispatchOrder>().ToTable("DispatchOrder");
        modelBuilder.Entity<DispatchOrderStop>().ToTable("DispatchOrderStop");
        modelBuilder.Entity<DispatchOrderLine>().ToTable("DispatchOrderLine");
        modelBuilder.Entity<DispatchDocument>().ToTable("DispatchDocument");
        modelBuilder.Entity<FreightStatement>().ToTable("FreightStatement");
        modelBuilder.Entity<FreightStatementLine>().ToTable("FreightStatementLine");
        modelBuilder.Entity<ChangeLog>().ToTable("ChangeLog");
        modelBuilder.Entity<CashReceipt>().ToTable("CashReceipt");
        modelBuilder.Entity<CashPayment>().ToTable("CashPayment");
        modelBuilder.Entity<VatInvoice>().ToTable("VatInvoice");
        modelBuilder.Entity<VatInvoiceLine>().ToTable("VatInvoiceLine");
        modelBuilder.Entity<AppUser>().ToTable("AppUser");
        modelBuilder.Entity<AppScreen>().ToTable("AppScreen");
        modelBuilder.Entity<UserPermission>().ToTable("UserPermission");
        modelBuilder.Entity<DocumentSequence>().ToTable("DocumentSequence");
        modelBuilder.Entity<SystemParameter>().ToTable("SystemParameter");
        modelBuilder.Entity<Company>().ToTable("Company");

        modelBuilder.Entity<AppScreen>().Property(x => x.Key).HasColumnName("Key");
        modelBuilder.Entity<DocumentSequence>().Property(x => x.Key).HasColumnName("Key");
        modelBuilder.Entity<SystemParameter>().Property(x => x.Key).HasColumnName("Key");
        modelBuilder.Entity<SystemParameter>().Property(x => x.Value).HasColumnName("Value");
        modelBuilder.Entity<FreightStatement>().Property(x => x.Year).HasColumnName("Year");
        modelBuilder.Entity<FreightStatement>().Property(x => x.Month).HasColumnName("Month");
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.Year).HasColumnName("Year");
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.Month).HasColumnName("Month");

        modelBuilder.Entity<Location>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<Location>().Property(x => x.Name).HasMaxLength(255);
        modelBuilder.Entity<Location>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Route>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<Route>().Property(x => x.Name).HasMaxLength(255);
        modelBuilder.Entity<Route>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Route>().Property(x => x.Fingerprint).HasMaxLength(200);
        modelBuilder.Entity<DispatchOrderStop>().Property(x => x.NameSnapshot).HasMaxLength(255);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.ReconciliationRejectionReason).HasMaxLength(500);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.PriceSourceSnapshot).HasMaxLength(500);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.FreightOverrideReason).HasMaxLength(500);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.PartnerNameSnapshot).HasMaxLength(255);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.BuyRateSourceSnapshot).HasMaxLength(500);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.BuyOverrideReason).HasMaxLength(500);
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.Notes).HasMaxLength(500);
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.VoidReason).HasMaxLength(500);
        modelBuilder.Entity<PartnerSettlementLine>().Property(x => x.DispatchCode).HasMaxLength(50);
        modelBuilder.Entity<PartnerSettlementLine>().Property(x => x.Route).HasMaxLength(255);
        modelBuilder.Entity<PartnerSettlementLine>().Property(x => x.PlateNumber).HasMaxLength(50);
        modelBuilder.Entity<PartnerSettlementLine>().Property(x => x.DriverName).HasMaxLength(255);
        modelBuilder.Entity<TransportExceptionCode>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<TransportExceptionCode>().Property(x => x.Name).HasMaxLength(255);
        modelBuilder.Entity<TransportExceptionCode>().Property(x => x.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<TransportException>().Property(x => x.CodeSnapshot).HasMaxLength(50);
        modelBuilder.Entity<TransportException>().Property(x => x.NameSnapshot).HasMaxLength(255);
        modelBuilder.Entity<TransportException>().Property(x => x.Description).HasMaxLength(1000);
        modelBuilder.Entity<TransportException>().Property(x => x.ReviewNote).HasMaxLength(500);
        modelBuilder.Entity<TransportException>().Property(x => x.VoidReason).HasMaxLength(500);
        modelBuilder.Entity<FreightStatement>().Property(x => x.VoidReason).HasMaxLength(500);
        modelBuilder.Entity<AppUser>().Property(x => x.UserName).HasMaxLength(50);
        modelBuilder.Entity<AppUser>().Property(x => x.PasswordHash).HasMaxLength(255);
        modelBuilder.Entity<AppUser>().Property(x => x.DisplayName).HasMaxLength(255);
        modelBuilder.Entity<LocationAlias>().Property(x => x.Alias).HasMaxLength(100);
        modelBuilder.Entity<RouteAlias>().Property(x => x.Alias).HasMaxLength(100);
        modelBuilder.Entity<CustomerAlias>().Property(x => x.Alias).HasMaxLength(100);
        modelBuilder.Entity<VehicleAlias>().Property(x => x.Alias).HasMaxLength(100);
        modelBuilder.Entity<PriceList>().Property(x => x.HasPriceFluctuation).HasDefaultValue(false);
        modelBuilder.Entity<PriceListFluctuation>().Property(x => x.Reason).HasMaxLength(500);
        modelBuilder.Entity<PriceListFluctuation>().Property(x => x.EffectiveFrom).HasColumnType("date");
        modelBuilder.Entity<PriceListFluctuation>().Property(x => x.EffectiveTo).HasColumnType("date");
        modelBuilder.Entity<LocationAlias>().HasIndex(x => x.Alias).IsUnique();
        modelBuilder.Entity<RouteAlias>().HasIndex(x => x.Alias).IsUnique();
        modelBuilder.Entity<CustomerAlias>().HasIndex(x => x.Alias).IsUnique();
        modelBuilder.Entity<VehicleAlias>().HasIndex(x => x.Alias).IsUnique();
        modelBuilder.Entity<Location>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Route>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Route>().HasIndex(x => x.Fingerprint).IsUnique();
        modelBuilder.Entity<RouteStop>().HasIndex(x => new { x.RouteId, x.Sequence }).IsUnique();
        modelBuilder.Entity<DispatchOrderStop>().HasIndex(x => new { x.DispatchOrderId, x.Sequence }).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.Code);
        modelBuilder.Entity<DispatchOrder>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Partner>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(x => x.PlateNumber).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<UserPermission>().HasIndex(x => new { x.AppUserId, x.AppScreenId }).IsUnique();
        modelBuilder.Entity<FreightStatement>().HasIndex(x => new { x.CustomerId, x.Year, x.Month }).IsUnique();
        modelBuilder.Entity<FreightStatementLine>().HasIndex(x => x.DispatchOrderId).IsUnique();
        modelBuilder.Entity<PriceListItem>()
            .HasIndex(x => new { x.PriceListRevisionId, x.RouteId, x.VehicleTypeId })
            .IsUnique()
            .HasFilter("[RouteId] IS NOT NULL");
        modelBuilder.Entity<PriceListItem>()
            .HasIndex(x => new { x.PriceListRevisionId, x.DeliveryLocationId, x.VehicleTypeId })
            .IsUnique()
            .HasFilter("[RouteId] IS NULL AND [DeliveryLocationId] IS NOT NULL");
        modelBuilder.Entity<PriceListFluctuation>()
            .HasIndex(x => new { x.PriceListId, x.EffectiveFrom })
            .IsUnique();
        modelBuilder.Entity<PartnerRate>()
            .HasIndex(x => new { x.PartnerId, x.RouteId, x.VehicleTypeId, x.EffectiveFrom })
            .IsUnique();
        modelBuilder.Entity<PartnerSettlement>()
            .HasIndex(x => new { x.PartnerId, x.Year, x.Month }).IsUnique();
        modelBuilder.Entity<PartnerSettlementLine>()
            .HasIndex(x => x.DispatchOrderId).IsUnique();
        modelBuilder.Entity<TransportExceptionCode>().HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<DispatchOrder>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceList>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceListFluctuation>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<PartnerRate>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<PartnerSettlement>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<TransportException>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<FreightStatement>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<DocumentSequence>().Property(x => x.RowVersion).IsRowVersion();

        modelBuilder.Entity<Partner>().Property(x => x.OperatingFeePercent).HasPrecision(9, 2);
        modelBuilder.Entity<VehicleType>().Property(x => x.Tonnage).HasPrecision(9, 2);
        modelBuilder.Entity<Vehicle>().Property(x => x.Tonnage).HasPrecision(9, 2);
        modelBuilder.Entity<PriceListItem>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<PriceListItem>().Property(x => x.Surcharge).HasPrecision(20, 2);
        modelBuilder.Entity<PriceListFluctuation>().Property(x => x.Value).HasPrecision(20, 4);
        modelBuilder.Entity<PartnerRate>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<PartnerRate>().Property(x => x.Surcharge).HasPrecision(20, 2);
        foreach (var name in new[] { nameof(PartnerSettlement.GrossAmount),
                     nameof(PartnerSettlement.OperatingFeeAmount), nameof(PartnerSettlement.PayableAmount) })
            modelBuilder.Entity<PartnerSettlement>().Property(name).HasPrecision(20, 2);
        foreach (var name in new[] { nameof(PartnerSettlementLine.BuyTotal),
                     nameof(PartnerSettlementLine.OperatingFeeAmount), nameof(PartnerSettlementLine.PayableAmount) })
            modelBuilder.Entity<PartnerSettlementLine>().Property(name).HasPrecision(20, 2);
        modelBuilder.Entity<PartnerSettlementLine>().Property(x => x.OperatingFeePercent).HasPrecision(9, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.Surcharge).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.ExtraCost).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.TotalAmount).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.ApprovedExceptionRevenue).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.ApprovedExceptionCost).HasPrecision(20, 2);
        foreach (var name in new[] { nameof(DispatchOrder.BuyUnitPrice), nameof(DispatchOrder.BuySurcharge),
                     nameof(DispatchOrder.BuyExtraCost), nameof(DispatchOrder.BuyTotal),
                     nameof(DispatchOrder.PartnerPayableAmount), nameof(DispatchOrder.GrossMargin) })
            modelBuilder.Entity<DispatchOrder>().Property(name).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.PartnerOperatingFeePercent).HasPrecision(9, 2);
        modelBuilder.Entity<TransportException>().Property(x => x.CustomerCharge).HasPrecision(20, 2);
        modelBuilder.Entity<TransportException>().Property(x => x.PartnerCost).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrderLine>().Property(x => x.Kilometers).HasPrecision(18, 2);
        foreach (var name in new[] { nameof(FreightStatement.FreightTotal), nameof(FreightStatement.SurchargeTotal),
                     nameof(FreightStatement.ExtraCostTotal), nameof(FreightStatement.GrandTotal),
                     nameof(FreightStatement.VatAmount), nameof(FreightStatement.TotalWithVat) })
            modelBuilder.Entity<FreightStatement>().Property(name).HasPrecision(20, 2);
        modelBuilder.Entity<FreightStatement>().Property(x => x.VatRate).HasPrecision(9, 2);
        modelBuilder.Entity<FreightStatementLine>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<FreightStatementLine>().Property(x => x.Surcharge).HasPrecision(20, 2);
        modelBuilder.Entity<FreightStatementLine>().Property(x => x.ExtraCost).HasPrecision(20, 2);
        modelBuilder.Entity<FreightStatementLine>().Property(x => x.LineTotal).HasPrecision(20, 2);
        modelBuilder.Entity<CashReceipt>().Property(x => x.Amount).HasPrecision(18, 0);
        modelBuilder.Entity<CashPayment>().Property(x => x.Amount).HasPrecision(18, 0);
        modelBuilder.Entity<VatInvoice>().Property(x => x.Amount).HasPrecision(18, 0);
        modelBuilder.Entity<VatInvoice>().Property(x => x.VatAmount).HasPrecision(18, 0);
        modelBuilder.Entity<VatInvoice>().Property(x => x.TotalAmount).HasPrecision(18, 0);
        modelBuilder.Entity<VatInvoice>().Property(x => x.VatRate).HasPrecision(9, 2);
        modelBuilder.Entity<VatInvoice>().Property(x => x.Quantity).HasPrecision(18, 2);

        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.SenderCustomer).WithMany().HasForeignKey(d => d.SenderCustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ReceiverCustomer).WithMany().HasForeignKey(d => d.ReceiverCustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.Route).WithMany().HasForeignKey(d => d.RouteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ReconciledByUser).WithMany().HasForeignKey(d => d.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ReconciliationSubmittedByUser).WithMany()
            .HasForeignKey(d => d.ReconciliationSubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ReconciliationRejectedByUser).WithMany()
            .HasForeignKey(d => d.ReconciliationRejectedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ConfirmedByUser).WithMany().HasForeignKey(d => d.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.PaymentMethod).WithMany().HasForeignKey(d => d.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.PriceListItem).WithMany().HasForeignKey(d => d.PriceListItemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.PriceListFluctuation).WithMany().HasForeignKey(d => d.PriceListFluctuationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.Partner).WithMany().HasForeignKey(d => d.PartnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.PartnerRate).WithMany().HasForeignKey(d => d.PartnerRateId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListItem>()
            .HasOne(i => i.Route).WithMany().HasForeignKey(i => i.RouteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListItem>()
            .HasOne(i => i.DeliveryLocation).WithMany().HasForeignKey(i => i.DeliveryLocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListFluctuation>()
            .HasOne(x => x.PriceList).WithMany(x => x.Fluctuations).HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListFluctuation>()
            .HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerRate>()
            .HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerRate>()
            .HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerRate>()
            .HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerRate>()
            .HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlement>()
            .HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlement>()
            .HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlement>()
            .HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlement>()
            .HasOne(x => x.FinalizedByUser).WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlement>()
            .HasOne(x => x.VoidedByUser).WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartnerSettlementLine>()
            .HasOne(x => x.PartnerSettlement).WithMany(x => x.Lines).HasForeignKey(x => x.PartnerSettlementId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PartnerSettlementLine>()
            .HasOne(x => x.DispatchOrder).WithMany().HasForeignKey(x => x.DispatchOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.DispatchOrder).WithMany(x => x.TransportExceptions)
            .HasForeignKey(x => x.DispatchOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.ExceptionCode).WithMany()
            .HasForeignKey(x => x.TransportExceptionCodeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.SubmittedByUser).WithMany()
            .HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.ReviewedByUser).WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransportException>()
            .HasOne(x => x.VoidedByUser).WithMany()
            .HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Location>()
            .HasOne(l => l.City).WithMany().HasForeignKey(l => l.CityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LocationAlias>()
            .HasOne(a => a.Location).WithMany().HasForeignKey(a => a.LocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RouteAlias>()
            .HasOne(a => a.Route).WithMany().HasForeignKey(a => a.RouteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RouteStop>()
            .HasOne(s => s.Route).WithMany(r => r.Stops).HasForeignKey(s => s.RouteId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RouteStop>()
            .HasOne(s => s.Location).WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrderStop>()
            .HasOne(s => s.DispatchOrder).WithMany(d => d.Stops).HasForeignKey(s => s.DispatchOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DispatchOrderStop>()
            .HasOne(s => s.Location).WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerAlias>()
            .HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VehicleAlias>()
            .HasOne(a => a.Vehicle).WithMany().HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.AccountantEmployee).WithMany().HasForeignKey(c => c.AccountantEmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashPayment>()
            .HasOne(p => p.DriverEmployee).WithMany().HasForeignKey(p => p.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChangeLog>()
            .HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FreightStatement>()
            .HasOne(s => s.CreatedByUser).WithMany().HasForeignKey(s => s.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FreightStatement>()
            .HasOne(s => s.SubmittedByUser).WithMany().HasForeignKey(s => s.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FreightStatement>()
            .HasOne(s => s.FinalizedByUser).WithMany().HasForeignKey(s => s.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FreightStatement>()
            .HasOne(s => s.VoidedByUser).WithMany().HasForeignKey(s => s.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.HasDeliveryNote);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.BillableExtraCost);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.PartnerBillableExtraCost);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.RouteLabel);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.PickupLocationName);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.DeliveryLocationName);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.CanEdit);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.CustomerCodeName);
        modelBuilder.Entity<TransportExceptionCode>().Ignore(x => x.DisplayName);
        modelBuilder.Entity<TransportException>().Ignore(x => x.StatusLabel);
        modelBuilder.Entity<Customer>().Ignore(c => c.CodeName);
        modelBuilder.Entity<DispatchOrder>().HasQueryFilter(d => !d.IsDeleted);
    }
}
