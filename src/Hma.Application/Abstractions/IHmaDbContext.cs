using Hma.Domain.Entities;

namespace Hma.Application.Abstractions;

public interface IHmaDbContext
{
    IQueryable<City> Cities { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<JobTitle> JobTitles { get; }
    IQueryable<VehicleType> VehicleTypes { get; }
    IQueryable<PaymentMethod> PaymentMethods { get; }
    IQueryable<Employee> Employees { get; }
    IQueryable<Partner> Partners { get; }
    IQueryable<Driver> Drivers { get; }
    IQueryable<Vehicle> Vehicles { get; }
    IQueryable<Customer> Customers { get; }
    IQueryable<PriceList> PriceLists { get; }
    IQueryable<PriceListRevision> PriceListRevisions { get; }
    IQueryable<PriceListItem> PriceListItems { get; }
    IQueryable<DispatchOrder> DispatchOrders { get; }
    IQueryable<DispatchOrderLine> DispatchOrderLines { get; }
    IQueryable<DispatchDocument> DispatchDocuments { get; }
    IQueryable<FreightStatement> FreightStatements { get; }
    IQueryable<FreightStatementLine> FreightStatementLines { get; }
    IQueryable<ChangeLog> ChangeLogs { get; }
    IQueryable<CashReceipt> CashReceipts { get; }
    IQueryable<CashPayment> CashPayments { get; }
    IQueryable<VatInvoice> VatInvoices { get; }
    IQueryable<VatInvoiceLine> VatInvoiceLines { get; }
    IQueryable<AppUser> Users { get; }
    IQueryable<AppScreen> Screens { get; }
    IQueryable<UserPermission> UserPermissions { get; }
    IQueryable<DocumentSequence> Sequences { get; }
    IQueryable<SystemParameter> Parameters { get; }
    IQueryable<Company> Companies { get; }

    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    void ApplyOriginalRowVersion<T>(T entity, byte[] originalRowVersion) where T : class;
    Task ReloadAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task<T?> FindAsync<T>(int id, CancellationToken cancellationToken = default) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task EnsureCreatedAndSeededAsync(CancellationToken cancellationToken = default);
}
