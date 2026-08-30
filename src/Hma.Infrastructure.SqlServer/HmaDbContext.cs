using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hma.Infrastructure.SqlServer;

public sealed class HmaDbContext(DbContextOptions<HmaDbContext> options) : DbContext(options), IHmaDbContext
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListRevision> PriceListRevisions => Set<PriceListRevision>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<DispatchOrder> DispatchOrders => Set<DispatchOrder>();
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
    IQueryable<Department> IHmaDbContext.Departments => Departments;
    IQueryable<JobTitle> IHmaDbContext.JobTitles => JobTitles;
    IQueryable<VehicleType> IHmaDbContext.VehicleTypes => VehicleTypes;
    IQueryable<PaymentMethod> IHmaDbContext.PaymentMethods => PaymentMethods;
    IQueryable<Employee> IHmaDbContext.Employees => Employees;
    IQueryable<Partner> IHmaDbContext.Partners => Partners;
    IQueryable<Driver> IHmaDbContext.Drivers => Drivers;
    IQueryable<Vehicle> IHmaDbContext.Vehicles => Vehicles;
    IQueryable<Customer> IHmaDbContext.Customers => Customers;
    IQueryable<PriceList> IHmaDbContext.PriceLists => PriceLists;
    IQueryable<PriceListRevision> IHmaDbContext.PriceListRevisions => PriceListRevisions;
    IQueryable<PriceListItem> IHmaDbContext.PriceListItems => PriceListItems;
    IQueryable<DispatchOrder> IHmaDbContext.DispatchOrders => DispatchOrders;
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
            entry = Set<T>().Update(entity);

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

    public async Task EnsureCreatedAndSeededAsync(CancellationToken cancellationToken = default)
    {
        var rebuild = false;
        if (await Database.CanConnectAsync(cancellationToken))
        {
            rebuild = !await PartnerTableExistsAsync(cancellationToken)
                      || !await ColumnExistsAsync("DispatchOrder", "SenderCustomerId", cancellationToken)
                      || !await ColumnExistsAsync("DispatchOrder", "SenderName", cancellationToken)
                      || !await ColumnExistsAsync("DispatchOrder", "RowVersion", cancellationToken)
                      || !await ColumnExistsAsync("DispatchOrder", "IsDeleted", cancellationToken);
            await Database.CloseConnectionAsync();
        }

        if (rebuild)
            await Database.EnsureDeletedAsync(cancellationToken);

        await Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(this, cancellationToken);
    }

    private async Task<bool> PartnerTableExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var conn = Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await Database.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.Partner', N'U') IS NULL THEN 0 ELSE 1 END";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ColumnExistsAsync(string table, string column, CancellationToken cancellationToken)
    {
        try
        {
            var conn = Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await Database.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT CASE WHEN COL_LENGTH(N'dbo.{table}', N'{column}') IS NULL THEN 0 ELSE 1 END";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }
        catch
        {
            return false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().ToTable("City");
        modelBuilder.Entity<Department>().ToTable("Department");
        modelBuilder.Entity<JobTitle>().ToTable("JobTitle");
        modelBuilder.Entity<VehicleType>().ToTable("VehicleType");
        modelBuilder.Entity<PaymentMethod>().ToTable("PaymentMethod");
        modelBuilder.Entity<Employee>().ToTable("Employee");
        modelBuilder.Entity<Partner>().ToTable("Partner");
        modelBuilder.Entity<Driver>().ToTable("Driver");
        modelBuilder.Entity<Vehicle>().ToTable("Vehicle");
        modelBuilder.Entity<Customer>().ToTable("Customer");
        modelBuilder.Entity<PriceList>().ToTable("PriceList");
        modelBuilder.Entity<PriceListRevision>().ToTable("PriceListRevision");
        modelBuilder.Entity<PriceListItem>().ToTable("PriceListItem");
        modelBuilder.Entity<DispatchOrder>().ToTable("DispatchOrder");
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

        modelBuilder.Entity<Customer>().HasIndex(x => x.Code);
        modelBuilder.Entity<DispatchOrder>().HasIndex(x => x.Code);
        modelBuilder.Entity<Partner>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(x => x.PlateNumber).IsUnique();
        modelBuilder.Entity<UserPermission>().HasIndex(x => new { x.AppUserId, x.AppScreenId }).IsUnique();
        modelBuilder.Entity<FreightStatement>().HasIndex(x => new { x.CustomerId, x.Year, x.Month }).IsUnique();

        modelBuilder.Entity<DispatchOrder>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceList>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<FreightStatement>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<DocumentSequence>().Property(x => x.RowVersion).IsRowVersion();

        modelBuilder.Entity<VehicleType>().Property(x => x.Tonnage).HasPrecision(9, 2);
        modelBuilder.Entity<Vehicle>().Property(x => x.Tonnage).HasPrecision(9, 2);
        modelBuilder.Entity<PriceListItem>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<PriceListItem>().Property(x => x.Surcharge).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.UnitPrice).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.Surcharge).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.ExtraCost).HasPrecision(20, 2);
        modelBuilder.Entity<DispatchOrder>().Property(x => x.TotalAmount).HasPrecision(20, 2);
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
            .HasOne(d => d.PickupCity).WithMany().HasForeignKey(d => d.PickupCityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.DeliveryCity).WithMany().HasForeignKey(d => d.DeliveryCityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.ReconciledByUser).WithMany().HasForeignKey(d => d.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DispatchOrder>()
            .HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListItem>()
            .HasOne(i => i.PickupCity).WithMany().HasForeignKey(i => i.PickupCityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListItem>()
            .HasOne(i => i.DeliveryCity).WithMany().HasForeignKey(i => i.DeliveryCityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.AccountantEmployee).WithMany().HasForeignKey(c => c.AccountantEmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashPayment>()
            .HasOne(p => p.DriverEmployee).WithMany().HasForeignKey(p => p.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChangeLog>()
            .HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.HasDeliveryNote);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.RouteLabel);
        modelBuilder.Entity<DispatchOrder>().Ignore(d => d.CanEdit);
        modelBuilder.Entity<DispatchOrder>().HasQueryFilter(d => !d.IsDeleted);
    }
}
