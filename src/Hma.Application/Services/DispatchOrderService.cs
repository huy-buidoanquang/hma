using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DispatchOrderService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    PriceListService prices,
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
        int? deliveryCityId = null,
        decimal? amountFrom = null,
        decimal? amountTo = null,
        int? vehicleId = null,
        int? driverId = null,
        CancellationToken ct = default)
    {
        var q = db.DispatchOrders
            .AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.SenderCustomer)
            .Include(d => d.ReceiverCustomer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle)
            .Include(d => d.VehicleType)
            .Include(d => d.PickupCity)
            .Include(d => d.DeliveryCity)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Documents)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(d => d.Code.Contains(code));
        if (from is not null) q = q.Where(d => d.PickupAt >= from);
        if (to is not null) q = q.Where(d => d.PickupAt <= to.Value.Date.AddDays(1).AddTicks(-1));
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId || d.SenderCustomerId == customerId || d.ReceiverCustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(plate))
            q = q.Where(d => d.Vehicle != null && d.Vehicle.PlateNumber.Contains(plate));
        if (status is not null) q = q.Where(d => (int)d.Status == status);
        if (recon is not null) q = q.Where(d => (int)d.ReconciliationStatus == recon);
        if (month is not null) q = q.Where(d => d.PickupAt.Month == month);
        if (year is not null) q = q.Where(d => d.PickupAt.Year == year);
        if (vehicleTypeId is not null) q = q.Where(d => d.VehicleTypeId == vehicleTypeId);
        if (deliveryCityId is not null) q = q.Where(d => d.DeliveryCityId == deliveryCityId);
        if (amountFrom is not null) q = q.Where(d => d.TotalAmount >= amountFrom);
        if (amountTo is not null) q = q.Where(d => d.TotalAmount <= amountTo);
        if (vehicleId is not null) q = q.Where(d => d.VehicleId == vehicleId);
        if (driverId is not null) q = q.Where(d => d.DriverId == driverId);
        return q.OrderByDescending(d => d.PickupAt).ThenByDescending(d => d.Id).Take(500).ToListAsync(ct);
    }

    public Task<DispatchOrder?> GetAsync(int id, CancellationToken ct = default) =>
        db.DispatchOrders
            .Include(d => d.Lines)
            .Include(d => d.Documents)
            .Include(d => d.Customer)
            .Include(d => d.SenderCustomer)
            .Include(d => d.ReceiverCustomer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle)
            .Include(d => d.VehicleType)
            .Include(d => d.PickupCity)
            .Include(d => d.DeliveryCity)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<DispatchOrder> CreateNewAsync(CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Create);
        return new DispatchOrder
        {
            Code = "",
            CreatedAt = DateTime.Now,
            PickupAt = DateTime.Now,
            CreatedByUserId = current.User?.Id,
            Status = DispatchStatus.Issued,
            Lines = { new DispatchOrderLine { LineNumber = 1 } }
        };
    }

    public async Task ApplyFreightAsync(DispatchOrder order, CancellationToken ct = default)
    {
        var rate = await prices.GetFreightAsync(order.CustomerId, order.PickupCityId, order.DeliveryCityId, order.VehicleTypeId, ct);
        if (rate is not null)
        {
            order.UnitPrice = rate.Value.UnitPrice;
            order.Surcharge = rate.Value.Surcharge;
        }
        order.RecalculateTotal();
        order.AmountInWords = AmountText.From(order.TotalAmount);
    }

    public async Task SaveAsync(DispatchOrder order, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.DispatchOrders, order.Id == 0);
        if (order.CustomerId is null)
            order.CustomerId = order.SenderCustomerId;
        if (order.CustomerId is null)
            throw new InvalidOperationException("Cần chọn khách hàng (người gửi / khách thanh toán).");
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

        DispatchOrderRules.EnsureCanSave(order);

        if (order.Id != 0)
        {
            var existing = await db.DispatchOrders.AsNoTracking().FirstOrDefaultAsync(d => d.Id == order.Id, ct);
            if (existing is not null)
            {
                if (existing.Status == DispatchStatus.Locked && current.User?.IsManager != true)
                    throw new InvalidOperationException("Lệnh đã khóa. Chỉ quản lý mới được sửa.");
                if (existing.ReconciliationStatus == ReconciliationStatus.Reconciled
                    && (existing.UnitPrice != order.UnitPrice || existing.Surcharge != order.Surcharge || existing.ExtraCost != order.ExtraCost))
                    throw new InvalidOperationException("Không được đổi cước sau khi đã đối soát.");
            }
        }

        if (order.VehicleTypeId is null && order.VehicleId is not null)
        {
            var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == order.VehicleId, ct);
            order.VehicleTypeId = vehicle?.VehicleTypeId;
        }

        order.RecalculateTotal();
        order.AmountInWords = AmountText.From(order.TotalAmount);

        var originalVersion = order.Id == 0 ? [] : order.RowVersion.ToArray();

        if (order.Id == 0 && string.IsNullOrWhiteSpace(order.Code))
            order.Code = await numbers.NextAsync("dispatch-order", ct);

        var incomingLines = order.Lines.ToList();
        order.Lines.Clear();

        var before = order.Id == 0 ? null : await db.DispatchOrders.AsNoTracking()
            .Where(d => d.Id == order.Id)
            .Select(d => new
            {
                d.UnitPrice, d.Surcharge, d.ExtraCost, d.TotalAmount,
                d.VehicleId, d.DriverId, d.PickupCityId, d.DeliveryCityId,
                d.PickupAddress, d.DeliveryAddress, d.Status
            })
            .FirstOrDefaultAsync(ct);

        if (order.Id == 0) db.Add(order);
        else
        {
            db.Update(order);
            db.ApplyOriginalRowVersion(order, originalVersion);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);

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
            if (before.VehicleId != order.VehicleId || before.DriverId != order.DriverId)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateVehicleDriver",
                    $"Xe {before.VehicleId}→{order.VehicleId}, tài xế {before.DriverId}→{order.DriverId}",
                    new { before.VehicleId, before.DriverId }, new { order.VehicleId, order.DriverId }, ct);
            }
            if (before.PickupCityId != order.PickupCityId || before.DeliveryCityId != order.DeliveryCityId
                || before.PickupAddress != order.PickupAddress || before.DeliveryAddress != order.DeliveryAddress)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateRoute",
                    "Đổi tuyến / địa điểm lấy-giao",
                    new { before.PickupCityId, before.DeliveryCityId, before.PickupAddress, before.DeliveryAddress },
                    new { order.PickupCityId, order.DeliveryCityId, order.PickupAddress, order.DeliveryAddress }, ct);
            }
            if (before.Status != order.Status)
            {
                await log.RecordAsync("DispatchOrder", order.Id, "UpdateStatus",
                    $"{before.Status} → {order.Status}", before.Status, order.Status, ct);
            }
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Delete);
        var entity = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
        if (entity.Status == DispatchStatus.Locked)
            throw new InvalidOperationException("Không xóa lệnh đã khóa.");
        if (entity.ReconciliationStatus == ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Không xóa lệnh đã đối soát.");
        var originalVersion = entity.RowVersion.ToArray();
        entity.IsDeleted = true;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, originalVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Delete", $"Ẩn lệnh {entity.Code}", null, null, ct);
    }

    public async Task LockAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        if (current.User?.IsManager != true)
            throw new InvalidOperationException("Chỉ quản lý được khóa lệnh.");
        var entity = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        entity.Status = DispatchStatus.Locked;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Lock", "Khóa lệnh", null, null, ct);
    }

    public async Task SetStatusAsync(int id, DispatchStatus status, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        var entity = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        if (entity.Status == DispatchStatus.Locked && current.User?.IsManager != true)
            throw new InvalidOperationException("Lệnh đã khóa.");
        var previous = entity.Status;
        entity.Status = status;
        db.Update(entity);
        db.ApplyOriginalRowVersion(entity, entity.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        if (previous != status)
            await log.RecordAsync("DispatchOrder", id, "UpdateStatus", $"{previous} → {status}", previous, status, ct);
    }

    public async Task ReconcileAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        if (order.Status != DispatchStatus.Completed)
            throw new InvalidOperationException("Chỉ đối soát lệnh đã hoàn thành chuyến.");
        if (!order.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote))
            throw new InvalidOperationException("Thiếu biên bản giao hàng — không đối soát được.");
        if (order.ReconciliationStatus == ReconciliationStatus.Reconciled)
            return;

        order.ReconciliationStatus = ReconciliationStatus.Reconciled;
        order.ReconciledAt = DateTime.Now;
        order.ReconciledByUserId = current.User?.Id;
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("DispatchOrder", id, "Reconcile", "Đã đối soát",
            new { Status = "Pending" }, new { Status = "Reconciled" }, ct);
    }

    public async Task UnreconcileAsync(int id, CancellationToken ct = default)
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
        await db.SaveChangesAsync(ct);
        return customer;
    }
}
