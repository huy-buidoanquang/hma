using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Application.Features.Customers;

public sealed class WalkInCustomerService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    ICurrentUser current,
    TimeProvider timeProvider)
{
    public async Task<CustomerSummary> CreateAsync(
        string name,
        string? address,
        string? phone,
        string? taxCode,
        int? cityId,
        CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.Create);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên khách hàng là bắt buộc.");
        PhoneRules.EnsureOptional(phone);
        TaxCodeRules.EnsureOptional(taxCode);
        var customer = new Customer
        {
            Code = "VL-" + await numbers.NextAsync("walk-in-customer", ct),
            Name = name,
            Address = address,
            Phone = phone,
            TaxCode = taxCode,
            CityId = cityId,
            IsWalkIn = true,
            UpdatedAt = timeProvider.GetLocalNow().DateTime
        };
        db.Add(customer);
        await PersistenceGuard.SaveAsync(db, ct);
        return new CustomerSummary(
            customer.Id, customer.Code, customer.Name, customer.Address, customer.Phone, customer.TaxCode,
            customer.ContactName, customer.Email, customer.CityId, null, customer.AccountantEmployeeId, null,
            customer.IsWalkIn, customer.UpdatedAt);
    }
}
