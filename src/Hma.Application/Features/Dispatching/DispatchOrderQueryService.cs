using Hma.Application.Abstractions.Persistence;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Dispatching;

public sealed class DispatchOrderQueryService(IHmaDbContext db)
{
    public async Task<List<DispatchOrderSummary>> SearchAsync(
        string? code,
        DateTime? from,
        DateTime? to,
        int? customerId,
        string? plate,
        int? status,
        int? recon,
        int? month,
        int? year,
        int? vehicleTypeId = null,
        int? deliveryLocationId = null,
        decimal? amountFrom = null,
        decimal? amountTo = null,
        int? vehicleId = null,
        int? driverId = null,
        int? pickupLocationId = null,
        string? customerCode = null,
        decimal? tonnage = null,
        int? billingYear = null,
        int? billingMonth = null,
        CancellationToken ct = default)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.Code.Contains(code));
        if (from is not null) query = query.Where(x => x.PickupAt >= from);
        if (to is not null) query = query.Where(x => x.PickupAt <= to.Value.Date.AddDays(1).AddTicks(-1));
        if (customerId is not null) query = query.Where(x => x.CustomerId == customerId || x.SenderCustomerId == customerId || x.ReceiverCustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(customerCode)) query = query.Where(x => x.Customer != null && x.Customer.Code.Contains(customerCode));
        if (!string.IsNullOrWhiteSpace(plate)) query = query.Where(x => x.Vehicle != null && x.Vehicle.PlateNumber.Contains(plate));
        if (status is not null) query = query.Where(x => (int)x.Status == status);
        if (recon is not null) query = query.Where(x => (int)x.ReconciliationStatus == recon);
        if (month is not null) query = query.Where(x => x.PickupAt.Month == month);
        if (year is not null) query = query.Where(x => x.PickupAt.Year == year);
        if (vehicleTypeId is not null) query = query.Where(x => x.VehicleTypeId == vehicleTypeId);
        if (deliveryLocationId is not null)
            query = query.Where(x => x.Stops.Any(stop => stop.LocationId == deliveryLocationId && stop.Sequence == x.Stops.Max(candidate => candidate.Sequence)));
        if (pickupLocationId is not null) query = query.Where(x => x.Stops.Any(stop => stop.LocationId == pickupLocationId && stop.Sequence == 0));
        if (amountFrom is not null) query = query.Where(x => x.TotalAmount >= amountFrom);
        if (amountTo is not null) query = query.Where(x => x.TotalAmount <= amountTo);
        if (vehicleId is not null) query = query.Where(x => x.VehicleId == vehicleId);
        if (driverId is not null) query = query.Where(x => x.DriverId == driverId);
        if (tonnage is not null)
            query = query.Where(x => (x.Vehicle != null && x.Vehicle.Tonnage == tonnage)
                                     || (x.VehicleType != null && x.VehicleType.Tonnage == tonnage));
        if (billingYear is not null) query = query.Where(x => x.BillingYear == billingYear);
        if (billingMonth is not null) query = query.Where(x => x.BillingMonth == billingMonth);
        var rows = await query.OrderByDescending(x => x.PickupAt).ThenByDescending(x => x.Id).Take(500).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public async Task<DispatchOrderDetails?> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await GetEntityAsync(id, ct);
        return order is null ? null : DispatchOrderMapping.ToDetails(order);
    }

    internal Task<DispatchOrder?> GetEntityAsync(int id, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Documents)
            .Include(x => x.TransportExceptions)
            .Include(x => x.Customer).ThenInclude(x => x!.AccountantEmployee)
            .Include(x => x.SenderCustomer)
            .Include(x => x.ReceiverCustomer)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle).ThenInclude(x => x!.VehicleType)
            .Include(x => x.Vehicle).ThenInclude(x => x!.Partner)
            .Include(x => x.Partner)
            .Include(x => x.PartnerRate)
            .Include(x => x.PriceListItem).ThenInclude(x => x!.PriceListRevision).ThenInclude(x => x!.PriceList)
            .Include(x => x.VehicleType)
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .Include(x => x.CreatedByUser)
            .Include(x => x.PaymentMethod)
            .Include(x => x.ConfirmedByUser)
            .Include(x => x.ReconciliationSubmittedByUser)
            .Include(x => x.ReconciliationRejectedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<DispatchOrderPrintDetails?> GetPrintDetailsAsync(int id, CancellationToken ct = default)
    {
        var order = await GetEntityAsync(id, ct);
        return order is null ? null : ToPrintDetails(order);
    }

    public async Task<List<DispatchOrderSummary>> TripsByVehicleAsync(int vehicleId, CancellationToken ct = default)
    {
        var rows = await BaseQuery().Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.PickupAt).Take(200).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public async Task<List<DispatchOrderSummary>> TripsByDriverAsync(int driverId, CancellationToken ct = default)
    {
        var rows = await BaseQuery().Where(x => x.DriverId == driverId)
            .OrderByDescending(x => x.PickupAt).Take(200).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    private IQueryable<DispatchOrder> BaseQuery() =>
        db.DispatchOrders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.SenderCustomer)
            .Include(x => x.ReceiverCustomer)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle).ThenInclude(x => x!.Partner)
            .Include(x => x.Partner)
            .Include(x => x.VehicleType)
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .Include(x => x.CreatedByUser)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Documents);

    internal static DispatchOrderSummary ToSummary(DispatchOrder order) => new()
    {
        Id = order.Id,
        Code = order.Code,
        PickupAt = order.PickupAt,
        Status = order.Status,
        ReconciliationStatus = order.ReconciliationStatus,
        Customer = order.Customer is null ? null : new CustomerOption(
            order.Customer.Id, order.Customer.Code, order.Customer.Name, order.Customer.Address,
            order.Customer.Phone, order.Customer.TaxCode, order.Customer.IsWalkIn,
            order.Customer.AccountantEmployeeId),
        RouteId = order.RouteId,
        Driver = order.Driver is null ? null : new DriverOption(
            order.Driver.Id, order.Driver.Code, order.Driver.Name, order.Driver.Phone, order.Driver.PartnerId,
            order.Driver.Partner is null ? null : new PartnerOption(
                order.Driver.Partner.Id, order.Driver.Partner.Code, order.Driver.Partner.Name,
                order.Driver.Partner.OperatingFeePercent)),
        DriverId = order.DriverId,
        Vehicle = order.Vehicle is null ? null : new VehicleOption(
            order.Vehicle.Id, order.Vehicle.PlateNumber, order.Vehicle.PartnerId,
            order.Vehicle.Partner is null ? null : new PartnerOption(
                order.Vehicle.Partner.Id, order.Vehicle.Partner.Code, order.Vehicle.Partner.Name,
                order.Vehicle.Partner.OperatingFeePercent),
            order.Vehicle.VehicleTypeId,
            order.Vehicle.VehicleType is null ? null : new VehicleTypeOption(
                order.Vehicle.VehicleType.Id, order.Vehicle.VehicleType.Code, order.Vehicle.VehicleType.Name,
                order.Vehicle.VehicleType.Tonnage),
            order.Vehicle.Tonnage),
        VehicleId = order.VehicleId,
        VehicleType = order.VehicleType is null ? null : new VehicleTypeOption(
            order.VehicleType.Id, order.VehicleType.Code, order.VehicleType.Name, order.VehicleType.Tonnage),
        PaymentMethod = order.PaymentMethod is null ? null : new PaymentMethodOption(
            order.PaymentMethod.Id, order.PaymentMethod.Code, order.PaymentMethod.Name),
        RouteLabel = order.RouteLabel,
        UnitPrice = order.UnitPrice,
        Surcharge = order.Surcharge,
        ExtraCost = order.ExtraCost,
        ApprovedExceptionRevenue = order.ApprovedExceptionRevenue,
        TotalAmount = order.TotalAmount,
        BillingYear = order.BillingYear,
        BillingMonth = order.BillingMonth,
        Notes = order.Notes,
        SenderName = order.SenderName,
        PartnerNameSnapshot = order.PartnerNameSnapshot,
        PartnerOperatingFeePercent = order.PartnerOperatingFeePercent,
        PartnerPayableAmount = order.PartnerPayableAmount,
        ArNumber = order.ArNumber,
        HasDeliveryNote = order.HasDeliveryNote,
        ReconciliationSubmittedByUserId = order.ReconciliationSubmittedByUserId,
        ReconciliationRejectionReason = order.ReconciliationRejectionReason,
        CreatedByUser = order.CreatedByUser is null ? null : new UserOption(
            order.CreatedByUser.Id, order.CreatedByUser.UserName, order.CreatedByUser.DisplayName),
        CanEdit = order.CanEdit,
    };

    private static DispatchOrderPrintDetails ToPrintDetails(DispatchOrder order) => new(
        ToSummary(order),
        order.CreatedAt,
        order.CreatedByUser?.DisplayName ?? order.CreatedByUser?.UserName,
        ToCustomer(order.SenderCustomer),
        order.SenderName,
        order.SenderPhone,
        order.SenderAddress,
        order.PickupAddress,
        ToCustomer(order.ReceiverCustomer),
        order.ReceiverName,
        order.ReceiverPhone,
        order.ReceiverAddress,
        order.DeliveryAddress,
        order.Lines.OrderBy(x => x.LineNumber).Select(x => new DispatchOrderLineDetails(
            x.Id, x.LineNumber, x.GoodsName, x.PackageCount, x.Route, x.Kilometers, x.Notes)).ToList());

    private static CustomerOption? ToCustomer(Customer? customer) => customer is null ? null : new CustomerOption(
        customer.Id, customer.Code, customer.Name, customer.Address, customer.Phone, customer.TaxCode,
        customer.IsWalkIn);
}
