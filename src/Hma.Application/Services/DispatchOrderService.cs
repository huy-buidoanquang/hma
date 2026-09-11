using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DispatchOrderService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    PriceListService prices,
    PartnerRateService partnerRates,
    ICurrentUser current,
    IChangeLogService log)
{
    public Task<List<DispatchOrder>> SearchAsync(
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
        var q = db.DispatchOrders
            .AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.SenderCustomer)
            .Include(d => d.ReceiverCustomer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle).ThenInclude(v => v!.Partner)
            .Include(d => d.Partner)
            .Include(d => d.VehicleType)
            .Include(d => d.Route)
            .Include(d => d.Stops)
            .Include(d => d.CreatedByUser)
            .Include(d => d.PaymentMethod)
            .Include(d => d.Documents)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(d => d.Code.Contains(code));
        if (from is not null) q = q.Where(d => d.PickupAt >= from);
        if (to is not null) q = q.Where(d => d.PickupAt <= to.Value.Date.AddDays(1).AddTicks(-1));
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId || d.SenderCustomerId == customerId || d.ReceiverCustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(customerCode))
            q = q.Where(d => d.Customer != null && d.Customer.Code.Contains(customerCode));
        if (!string.IsNullOrWhiteSpace(plate))
            q = q.Where(d => d.Vehicle != null && d.Vehicle.PlateNumber.Contains(plate));
        if (status is not null) q = q.Where(d => (int)d.Status == status);
        if (recon is not null) q = q.Where(d => (int)d.ReconciliationStatus == recon);
        if (month is not null) q = q.Where(d => d.PickupAt.Month == month);
        if (year is not null) q = q.Where(d => d.PickupAt.Year == year);
        if (vehicleTypeId is not null) q = q.Where(d => d.VehicleTypeId == vehicleTypeId);
        if (deliveryLocationId is not null)
            q = q.Where(d => d.Stops.Any(s => s.LocationId == deliveryLocationId
                                              && s.Sequence == d.Stops.Max(x => x.Sequence)));
        if (pickupLocationId is not null)
            q = q.Where(d => d.Stops.Any(s => s.LocationId == pickupLocationId && s.Sequence == 0));
        if (amountFrom is not null) q = q.Where(d => d.TotalAmount >= amountFrom);
        if (amountTo is not null) q = q.Where(d => d.TotalAmount <= amountTo);
        if (vehicleId is not null) q = q.Where(d => d.VehicleId == vehicleId);
        if (driverId is not null) q = q.Where(d => d.DriverId == driverId);
        if (tonnage is not null)
            q = q.Where(d => (d.Vehicle != null && d.Vehicle.Tonnage == tonnage)
                             || (d.VehicleType != null && d.VehicleType.Tonnage == tonnage));
        if (billingYear is not null) q = q.Where(d => d.BillingYear == billingYear);
        if (billingMonth is not null) q = q.Where(d => d.BillingMonth == billingMonth);
        return q.OrderByDescending(d => d.PickupAt).ThenByDescending(d => d.Id).Take(500).ToListAsync(ct);
    }

    public Task<DispatchOrder?> GetAsync(int id, CancellationToken ct = default) =>
        db.DispatchOrders
            .AsNoTracking()
            .Include(d => d.Lines)
            .Include(d => d.Documents)
            .Include(d => d.TransportExceptions)
            .Include(d => d.Customer)
            .Include(d => d.SenderCustomer)
            .Include(d => d.ReceiverCustomer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle).ThenInclude(v => v!.VehicleType)
            .Include(d => d.Vehicle).ThenInclude(v => v!.Partner)
            .Include(d => d.Partner)
            .Include(d => d.PartnerRate)
            .Include(d => d.VehicleType)
            .Include(d => d.Route)
            .Include(d => d.Stops)
            .Include(d => d.CreatedByUser)
            .Include(d => d.PaymentMethod)
            .Include(d => d.ConfirmedByUser)
            .Include(d => d.ReconciliationSubmittedByUser)
            .Include(d => d.ReconciliationRejectedByUser)
            .Include(d => d.Customer).ThenInclude(c => c!.AccountantEmployee)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<DispatchOrder> CreateNewAsync(CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Create);
        var credit = await db.PaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == PaymentMethodCodes.Credit, ct);
        var now = DateTime.Now;
        return new DispatchOrder
        {
            Code = "",
            CreatedAt = now,
            PickupAt = now,
            BillingYear = now.Year,
            BillingMonth = now.Month,
            PaymentMethodId = credit?.Id,
            CreatedByUserId = current.User?.Id,
            Status = DispatchStatus.Issued
        };
    }

    public async Task<FreightQuote?> ApplyFreightAsync(DispatchOrder order, CancellationToken ct = default)
    {
        var rate = await prices.GetFreightAsync(
            order.CustomerId, order.RouteId, order.VehicleTypeId, order.PickupAt, ct);
        if (rate is not null)
        {
            order.UnitPrice = rate.UnitPrice;
            order.Surcharge = rate.Surcharge;
            order.PriceListItemId = rate.PriceListItemId;
            order.PriceSourceSnapshot = rate.SourceLabel;
            order.IsFreightManual = false;
            order.FreightOverrideReason = null;
        }
        order.RecalculateTotal();
        var vehicle = order.VehicleId is null
            ? null
            : await db.Vehicles.AsNoTracking().Include(x => x.Partner)
                .FirstOrDefaultAsync(x => x.Id == order.VehicleId, ct);
        var partner = vehicle?.Partner;
        var isAssigned = partner is not null && partner.Code != "UNASSIGNED";
        if (isAssigned && order.RouteId is int routeId && order.VehicleTypeId is int vehicleTypeId)
        {
            var buyRate = await partnerRates.GetRateAsync(partner!.Id, routeId, vehicleTypeId, order.PickupAt, ct);
            if (buyRate is not null)
            {
                order.BuyUnitPrice = buyRate.UnitPrice;
                order.BuySurcharge = buyRate.Surcharge;
                PartnerCommercialRules.Capture(order, partner, buyRate, isAssignedPartner: true);
            }
            else
            {
                order.PartnerId = partner!.Id;
                order.PartnerNameSnapshot = partner.Name;
                order.PartnerOperatingFeePercent = partner.OperatingFeePercent;
                order.PartnerRateId = null;
                order.BuyRateSourceSnapshot = null;
            }
        }
        else
        {
            PartnerCommercialRules.Capture(order, null, null, isAssignedPartner: false);
        }
        AmountText.Refresh(order);
        return rate;
    }

    public Task SaveAsync(DispatchOrder order, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SaveCoreAsync(order, token), ct);

    private async Task SaveCoreAsync(DispatchOrder order, CancellationToken ct)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.DispatchOrders, order.Id == 0);
        if (order.CustomerId is null)
            throw new InvalidOperationException("Cần chọn khách hàng theo mã đã tạo.");
        if (order.VehicleId is null || order.DriverId is null)
            throw new InvalidOperationException("Cần chọn xe và tài xế.");

        if (order.SenderCustomerId is int senderId)
        {
            var sender = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == senderId, ct);
            if (sender is not null)
            {
                order.SenderName = sender.Name;
                order.SenderPhone = sender.Phone;
                order.SenderAddress = sender.Address;
                order.SenderTaxCode = sender.TaxCode;
            }
        }
        if (order.ReceiverCustomerId is int receiverId)
        {
            var receiver = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == receiverId, ct);
            if (receiver is not null)
            {
                order.ReceiverName = receiver.Name;
                order.ReceiverPhone = receiver.Phone;
                order.ReceiverAddress = receiver.Address;
                order.ReceiverTaxCode = receiver.TaxCode;
            }
        }

        await ApplyRouteStopsAsync(order, ct);
        if (order.VehicleTypeId is null && order.VehicleId is not null)
        {
            var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == order.VehicleId, ct);
            order.VehicleTypeId = vehicle?.VehicleTypeId;
        }
        var freightQuote = await prices.GetFreightAsync(
            order.CustomerId, order.RouteId, order.VehicleTypeId, order.PickupAt, ct);
        FreightPricingRules.CaptureSource(order, freightQuote);
        DispatchOrderRules.EnsureCanSave(order);
        await EnsureDispatchFksAsync(order, ct);

        DispatchOrder? persistedOrder = null;
        if (order.Id != 0)
        {
            persistedOrder = await db.DispatchOrders.AsNoTracking()
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.Id == order.Id, ct);
            if (persistedOrder is null)
                throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
            DispatchWorkflowRules.EnsureCanEdit(persistedOrder);
            order.ApprovedExceptionRevenue = persistedOrder.ApprovedExceptionRevenue;
            order.ApprovedExceptionCost = persistedOrder.ApprovedExceptionCost;
        }
        else if (order.ApprovedExceptionRevenue != 0 || order.ApprovedExceptionCost != 0)
            throw new InvalidOperationException("Chi phí sự cố chỉ được cập nhật qua quy trình duyệt sự cố.");

        order.RecalculateTotal();
        var commercialVehicle = await db.Vehicles.AsNoTracking()
            .Include(x => x.Partner)
            .FirstOrDefaultAsync(x => x.Id == order.VehicleId, ct);
        var commercialDriver = await db.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == order.DriverId, ct);
        if (commercialVehicle is null || commercialDriver is null)
            throw new InvalidOperationException("Không tìm thấy xe hoặc tài xế.");
        DispatchAssignmentRules.EnsureSamePartner(commercialVehicle, commercialDriver);
        var commercialPartner = commercialVehicle?.Partner;
        var hasAssignedPartner = commercialPartner is not null && commercialPartner.Code != "UNASSIGNED";
        if (persistedOrder is not null)
            TransportExceptionRules.EnsurePartnerAssignmentCanChange(
                persistedOrder, hasAssignedPartner ? commercialPartner!.Id : null);
        PartnerRate? buyRate = null;
        if (hasAssignedPartner && order.RouteId is int commercialRouteId && order.VehicleTypeId is int commercialVehicleTypeId)
        {
            buyRate = await partnerRates.GetRateAsync(
                commercialPartner!.Id, commercialRouteId, commercialVehicleTypeId, order.PickupAt, ct);
        }
        PartnerCommercialRules.Capture(order, commercialPartner, buyRate, hasAssignedPartner);
        AmountText.Refresh(order);

        var originalVersion = order.Id == 0 ? [] : order.RowVersion.ToArray();

        if (order.Id == 0 && string.IsNullOrWhiteSpace(order.Code))
            order.Code = await numbers.NextAsync("dispatch-order", ct);

        var incomingLines = order.Lines.ToList();
        order.Lines.Clear();
        var incomingStops = order.Stops.ToList();
        order.Stops.Clear();
        order.Documents.Clear();
        order.TransportExceptions.Clear();

        var before = order.Id == 0 ? null : await db.DispatchOrders.AsNoTracking()
            .Where(d => d.Id == order.Id)
            .Select(d => new
            {
                d.UnitPrice,
                d.Surcharge,
                d.ExtraCost,
                d.TotalAmount,
                d.BuyUnitPrice,
                d.BuySurcharge,
                d.BuyExtraCost,
                d.PartnerPayableAmount,
                d.GrossMargin,
                d.VehicleId,
                d.DriverId,
                d.RouteId,
                d.PickupAddress,
                d.DeliveryAddress,
                d.Status
            })
            .FirstOrDefaultAsync(ct);

        DetachReferences(order);

        if (order.Id == 0) db.Add(order);
        else
        {
            db.Update(order);
            db.ApplyOriginalRowVersion(order, originalVersion);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);

        var oldStops = await db.DispatchOrderStops.Where(s => s.DispatchOrderId == order.Id).ToListAsync(ct);
        foreach (var stop in oldStops) db.Remove(stop);
        foreach (var stop in incomingStops)
        {
            db.Add(new DispatchOrderStop
            {
                DispatchOrderId = order.Id,
                Sequence = stop.Sequence,
                LocationId = stop.LocationId,
                NameSnapshot = stop.NameSnapshot
            });
        }

        var oldLines = await db.DispatchOrderLines.Where(l => l.DispatchOrderId == order.Id).ToListAsync(ct);
        foreach (var line in oldLines) db.Remove(line);
        var lineNumber = 1;
        foreach (var line in incomingLines)
        {
            if (string.IsNullOrWhiteSpace(line.GoodsName) && line.PackageCount is null
                && string.IsNullOrWhiteSpace(line.Route) && line.Kilometers is null && string.IsNullOrWhiteSpace(line.Notes))
                continue;
            db.Add(new DispatchOrderLine
            {
                DispatchOrderId = order.Id,
                LineNumber = lineNumber++,
                GoodsName = line.GoodsName,
                PackageCount = line.PackageCount,
                Route = line.Route,
                Kilometers = line.Kilometers,
                Notes = line.Notes
            });
        }
        await ConcurrencyConflict.SaveAsync(db, ct);

        if (before is null)
        {
            await log.RecordAsync("DispatchOrder", order.Id, "Create",
                $"Tạo lệnh {order.Code}", null, new { order.Code, order.TotalAmount }, ct);
        }
        else
        {
            if (before.UnitPrice != order.UnitPrice || before.Surcharge != order.Surcharge
                || before.ExtraCost != order.ExtraCost || before.TotalAmount != order.TotalAmount)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateFreight",
                    $"Cước {order.UnitPrice:N0} + phụ phí {order.Surcharge:N0} + phát sinh {order.ExtraCost:N0} = {order.TotalAmount:N0}",
                    before, new { order.UnitPrice, order.Surcharge, order.ExtraCost, order.TotalAmount }, ct);
            }
            if (before.BuyUnitPrice != order.BuyUnitPrice || before.BuySurcharge != order.BuySurcharge
                || before.BuyExtraCost != order.BuyExtraCost || before.PartnerPayableAmount != order.PartnerPayableAmount)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdatePartnerCost",
                    $"Giá mua {order.BuyUnitPrice:N0} + phụ phí {order.BuySurcharge:N0} + phát sinh {order.BuyExtraCost:N0}; phải trả {order.PartnerPayableAmount:N0}",
                    before, new { order.BuyUnitPrice, order.BuySurcharge, order.BuyExtraCost, order.PartnerPayableAmount, order.GrossMargin }, ct);
            }
            if (before.VehicleId != order.VehicleId || before.DriverId != order.DriverId)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateVehicleDriver",
                    $"Xe {before.VehicleId}→{order.VehicleId}, tài xế {before.DriverId}→{order.DriverId}",
                    new { before.VehicleId, before.DriverId }, new { order.VehicleId, order.DriverId }, ct);
            }
            if (before.RouteId != order.RouteId
                || before.PickupAddress != order.PickupAddress || before.DeliveryAddress != order.DeliveryAddress)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateRoute",
                    "Đổi tuyến / địa điểm lấy-giao",
                    new { before.RouteId, before.PickupAddress, before.DeliveryAddress },
                    new { order.RouteId, order.PickupAddress, order.DeliveryAddress }, ct);
            }
            if (before.Status != order.Status)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateStatus",
                    $"{before.Status} → {order.Status}", before.Status, order.Status, ct);
            }
        }
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => DeleteCoreAsync(id, token), ct);

    private async Task DeleteCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Delete);
        var entity = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
        DispatchWorkflowRules.EnsureCanDelete(entity);
        var originalVersion = entity.RowVersion.ToArray();
        entity.IsDeleted = true;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, originalVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Delete", $"Ẩn lệnh {entity.Code}", null, null, ct);
    }

    public Task LockAsync(int id, string? arNumber = null, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => LockCoreAsync(id, arNumber, token), ct);

    private async Task LockCoreAsync(int id, string? arNumber, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        var entity = await db.DispatchOrders
            .Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchConfirmRules.EnsureCanConfirm(current.User, entity.Customer);
        DispatchWorkflowRules.EnsureCanConfirm(entity);
        entity.ConfirmedByUserId = current.User?.Id;
        entity.ConfirmedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(arNumber))
            entity.ArNumber = arNumber.Trim();
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        var ar = string.IsNullOrWhiteSpace(entity.ArNumber) ? "" : $" AR {entity.ArNumber}";
        await log.RecordAsync("DispatchOrder", id, "Lock", $"Chốt lệnh{ar}", null, entity.ArNumber, ct);
    }

    public Task UnlockAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => UnlockCoreAsync(id, token), ct);

    private async Task UnlockCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        var entity = await db.DispatchOrders
            .Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchConfirmRules.EnsureCanUnlock(current.User, entity.Customer);
        DispatchWorkflowRules.EnsureCanUnconfirm(entity);
        if (entity.Status == DispatchStatus.Locked)
            entity.Status = DispatchStatus.Completed;
        entity.ConfirmedByUserId = null;
        entity.ConfirmedAt = null;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Unlock", "Bỏ chốt lệnh", null, null, ct);
    }

    public Task SetStatusAsync(int id, DispatchStatus status, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SetStatusCoreAsync(id, status, token), ct);

    private async Task SetStatusCoreAsync(int id, DispatchStatus status, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        var entity = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanChangeStatus(entity, status);
        var previous = entity.Status;
        entity.Status = status;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        if (previous != status)
            await log.RecordAsync("DispatchOrder", id, "UpdateStatus", $"{previous} → {status}", previous, status, ct);
    }

    public Task SubmitReconciliationAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitReconciliationCoreAsync(id, token), ct);

    private async Task SubmitReconciliationCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanSubmitReconciliation(order, current.User?.Id);

        var previous = order.ReconciliationStatus;
        order.ReconciliationStatus = ReconciliationStatus.Submitted;
        order.ReconciliationSubmittedAt = DateTime.Now;
        order.ReconciliationSubmittedByUserId = current.User?.Id;
        order.ReconciliationRejectedAt = null;
        order.ReconciliationRejectedByUserId = null;
        order.ReconciliationRejectionReason = null;
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "SubmitReconciliation", "Gửi duyệt đối soát",
            new { Status = previous }, new { Status = ReconciliationStatus.Submitted }, ct);
    }

    public Task ReconcileAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ReconcileCoreAsync(id, token), ct);

    private async Task ReconcileCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        if (order.ReconciliationStatus == ReconciliationStatus.Reconciled)
            return;
        DispatchWorkflowRules.EnsureCanApproveReconciliation(order, current.User?.Id);

        order.ReconciliationStatus = ReconciliationStatus.Reconciled;
        order.ReconciledAt = DateTime.Now;
        order.ReconciledByUserId = current.User?.Id;
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Reconcile", "Đã đối soát",
            new { Status = ReconciliationStatus.Submitted }, new { Status = ReconciliationStatus.Reconciled }, ct);
    }

    public Task RejectReconciliationAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => RejectReconciliationCoreAsync(id, reason, token), ct);

    private async Task RejectReconciliationCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanRejectReconciliation(order, current.User?.Id, reason);

        order.ReconciliationStatus = ReconciliationStatus.Rejected;
        order.ReconciliationRejectedAt = DateTime.Now;
        order.ReconciliationRejectedByUserId = current.User?.Id;
        order.ReconciliationRejectionReason = reason.Trim();
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "RejectReconciliation", "Từ chối đối soát",
            new { Status = ReconciliationStatus.Submitted },
            new { Status = ReconciliationStatus.Rejected, Reason = order.ReconciliationRejectionReason }, ct);
    }

    public Task UnreconcileAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => UnreconcileCoreAsync(id, token), ct);

    private async Task UnreconcileCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        if (current.User?.IsManager != true)
            throw new InvalidOperationException("Chỉ quản lý được hủy đối soát.");
        var order = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        var onStatement = await db.FreightStatementLines.AnyAsync(l => l.DispatchOrderId == id, ct);
        if (onStatement)
            throw new InvalidOperationException("Lệnh đã nằm trong bảng kê — không hủy đối soát.");
        order.ReconciliationStatus = ReconciliationStatus.Pending;
        order.ReconciledAt = null;
        order.ReconciledByUserId = null;
        order.ReconciliationSubmittedAt = null;
        order.ReconciliationSubmittedByUserId = null;
        order.ReconciliationRejectedAt = null;
        order.ReconciliationRejectedByUserId = null;
        order.ReconciliationRejectionReason = null;
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Unreconcile", "Hủy đối soát", null, null, ct);
    }

    public async Task<Customer> CreateWalkInAsync(string name, string? address, string? phone, string? taxCode, int? cityId, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.Create);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên khách hàng là bắt buộc.");
        PhoneRules.EnsureOptional(phone);
        TaxCodeRules.EnsureOptional(taxCode);
        var code = "VL-" + await numbers.NextAsync("walk-in-customer", ct);
        var customer = new Customer
        {
            Code = code,
            Name = name,
            Address = address,
            Phone = phone,
            TaxCode = taxCode,
            CityId = cityId,
            IsWalkIn = true,
            UpdatedAt = DateTime.Now
        };
        db.Add(customer);
        await PersistenceGuard.SaveAsync(db, ct);
        return customer;
    }

    public Task ShiftBillingPeriodAsync(IReadOnlyList<int> ids, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ShiftBillingPeriodCoreAsync(ids, token), ct);

    private async Task ShiftBillingPeriodCoreAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        RequireDispatchEdit();
        var entities = new List<DispatchOrder>();
        foreach (var id in ids.Distinct())
        {
            var entity = await db.DispatchOrders
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.Id == id, ct)
                ?? throw new InvalidOperationException($"Không tìm thấy lệnh #{id}.");
            DispatchWorkflowRules.EnsureCanEdit(entity);
            entities.Add(entity);
        }

        foreach (var entity in entities)
        {
            BillingPeriodRules.ApplyDefault(entity);
            var next = BillingPeriodRules.Next(entity.BillingYear, entity.BillingMonth);
            entity.BillingYear = next.Year;
            entity.BillingMonth = next.Month;
            db.Update(entity);
            db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);
        foreach (var entity in entities)
            await log.RecordAsync("DispatchOrder", entity.Id, "ShiftBilling",
                $"Kỳ kế toán → {entity.BillingMonth:00}/{entity.BillingYear}", null, null, ct);
    }

    public async Task SaveGridRowAsync(
        int id,
        decimal unitPrice,
        decimal extraCost,
        string? notes,
        int billingYear,
        int billingMonth,
        int? routeId,
        int? vehicleId,
        CancellationToken ct = default)
    {
        RequireDispatchEdit();
        var entity = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanEdit(entity);
        entity.UnitPrice = unitPrice;
        entity.ExtraCost = extraCost;
        entity.Notes = notes;
        entity.BillingYear = billingYear;
        entity.BillingMonth = billingMonth;
        if (routeId is not null) entity.RouteId = routeId;
        if (vehicleId is not null)
        {
            entity.VehicleId = vehicleId;
            var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, ct);
            entity.VehicleTypeId = vehicle?.VehicleTypeId;
        }
        await SaveAsync(entity, ct);
    }

    private void RequireDispatchEdit()
    {
        if (current.Can(ScreenKeys.DispatchGridEdit, PermissionAction.Update)
            || current.Can(ScreenKeys.DispatchOrders, PermissionAction.Update))
            return;
        throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
    }

    private async Task EnsureDispatchFksAsync(DispatchOrder order, CancellationToken ct)
    {
        await EnsureExistsAsync(db.Customers, order.CustomerId, "Khách hàng không tồn tại.", ct);
        await EnsureExistsAsync(db.Customers, order.SenderCustomerId, "Khách gửi không tồn tại.", ct);
        await EnsureExistsAsync(db.Customers, order.ReceiverCustomerId, "Khách nhận không tồn tại.", ct);
        await EnsureExistsAsync(db.Routes, order.RouteId, "Tuyến không tồn tại.", ct);
        await EnsureExistsAsync(db.Vehicles, order.VehicleId, "Xe không tồn tại.", ct);
        await EnsureExistsAsync(db.Drivers, order.DriverId, "Tài xế không tồn tại.", ct);
        await EnsureExistsAsync(db.VehicleTypes, order.VehicleTypeId, "Loại xe không tồn tại.", ct);
        await EnsureExistsAsync(db.PaymentMethods, order.PaymentMethodId, "Hình thức thanh toán không tồn tại.", ct);
        await EnsureExistsAsync(db.Employees, order.EmployeeId, "Nhân viên không tồn tại.", ct);
    }

    private static async Task EnsureExistsAsync<T>(IQueryable<T> set, int? id, string message, CancellationToken ct)
        where T : Entity
    {
        if (id is null) return;
        if (!await set.AnyAsync(x => x.Id == id, ct))
            throw new InvalidOperationException(message);
    }

    private static void DetachReferences(DispatchOrder order)
    {
        var customerId = order.CustomerId;
        var senderId = order.SenderCustomerId;
        var receiverId = order.ReceiverCustomerId;
        var routeId = order.RouteId;
        var vehicleId = order.VehicleId;
        var driverId = order.DriverId;
        var vehicleTypeId = order.VehicleTypeId;
        var employeeId = order.EmployeeId;
        var paymentMethodId = order.PaymentMethodId;
        var priceListItemId = order.PriceListItemId;
        var partnerId = order.PartnerId;
        var partnerRateId = order.PartnerRateId;
        var createdBy = order.CreatedByUserId;
        var confirmedBy = order.ConfirmedByUserId;
        var reconciledBy = order.ReconciledByUserId;
        var reconciliationSubmittedBy = order.ReconciliationSubmittedByUserId;
        var reconciliationRejectedBy = order.ReconciliationRejectedByUserId;

        order.Customer = null;
        order.SenderCustomer = null;
        order.ReceiverCustomer = null;
        order.Route = null;
        order.Vehicle = null;
        order.Driver = null;
        order.VehicleType = null;
        order.Employee = null;
        order.PaymentMethod = null;
        order.PriceListItem = null;
        order.Partner = null;
        order.PartnerRate = null;
        order.CreatedByUser = null;
        order.ConfirmedByUser = null;
        order.ReconciledByUser = null;
        order.ReconciliationSubmittedByUser = null;
        order.ReconciliationRejectedByUser = null;

        order.CustomerId = customerId;
        order.SenderCustomerId = senderId;
        order.ReceiverCustomerId = receiverId;
        order.RouteId = routeId;
        order.VehicleId = vehicleId;
        order.DriverId = driverId;
        order.VehicleTypeId = vehicleTypeId;
        order.EmployeeId = employeeId;
        order.PaymentMethodId = paymentMethodId;
        order.PriceListItemId = priceListItemId;
        order.PartnerId = partnerId;
        order.PartnerRateId = partnerRateId;
        order.CreatedByUserId = createdBy;
        order.ConfirmedByUserId = confirmedBy;
        order.ReconciledByUserId = reconciledBy;
        order.ReconciliationSubmittedByUserId = reconciliationSubmittedBy;
        order.ReconciliationRejectedByUserId = reconciliationRejectedBy;
    }

    private async Task ApplyRouteStopsAsync(DispatchOrder order, CancellationToken ct)
    {
        if (order.RouteId is not int routeId || routeId <= 0)
            return;
        var route = await db.Routes.AsNoTracking()
            .Include(r => r.Stops).ThenInclude(s => s.Location)
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);
        if (route is null)
            return;
        var stops = route.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => (s.LocationId, s.Location?.Name ?? ""))
            .ToList();
        order.ReplaceStops(stops);
    }
}
