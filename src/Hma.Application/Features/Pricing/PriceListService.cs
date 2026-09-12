using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Domain.Entities;
using Hma.Domain.Models;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Pricing;

public class PriceListService(IHmaDbContext db, ICurrentUser current, TimeProvider timeProvider)
{
    public async Task<List<PriceListSummary>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.PriceLists.AsNoTracking().Include(p => p.Customer).OrderBy(p => p.Code).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public async Task<PriceListDetails?> GetAsync(int id, CancellationToken ct = default)
    {
        var list = await db.PriceLists.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.Route)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.DeliveryLocation)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (list is null) return null;

        var revision = list.Revisions.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        var fluctuations = await db.PriceListFluctuations.AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Where(x => x.PriceListId == list.Id)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return new PriceListDetails(
            ToSummary(list),
            revision?.Id,
            revision?.Items.Select(ToSummary).ToList() ?? [],
            fluctuations.Select(x => ToSummary(x)).ToList());
    }

    public async Task<PriceListSummary> SaveAsync(SavePriceListCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PriceLists, command.Id == 0);
        var list = command.Id == 0
            ? new PriceList()
            : await db.FindAsync<PriceList>(command.Id, ct)
              ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        list.Code = command.Code;
        list.Name = command.Name;
        list.Description = command.Description;
        list.CustomerId = command.CustomerId;
        list.EffectiveFrom = command.EffectiveFrom;
        list.EffectiveTo = command.EffectiveTo;
        list.HasPriceFluctuation = command.HasPriceFluctuation;
        list.LockReason = command.LockReason;

        PriceListRules.EnsureCanSave(list);
        if (await db.PriceLists.AnyAsync(p => p.Code == list.Code && p.Id != list.Id, ct))
            throw new InvalidOperationException("Mã bảng giá đã tồn tại.");
        if (list.CustomerId is int customerId && !await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new InvalidOperationException("Khách hàng không tồn tại.");

        if (list.Id == 0)
        {
            list.CreatedAt = timeProvider.GetLocalNow().Date;
            db.Add(list);
        }
        else
        {
            var existing = await db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == list.Id, ct)
                ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
            PriceListRules.EnsureCanModify(existing);
            if (list.IsLocked)
                throw new InvalidOperationException("Dùng thao tác khóa bảng giá để chốt dữ liệu.");
            db.ApplyOriginalRowVersion(list, command.VersionToken);
        }

        await ConcurrencyConflict.SaveAsync(db, ct);
        return ToSummary(list);
    }

    public async Task<int> EnsureRevisionAsync(int priceListId, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        var currentRevisionId = await db.PriceListRevisions.AsNoTracking()
            .Where(x => x.PriceListId == priceListId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);
        if (currentRevisionId is int existingId) return existingId;

        var priceList = await db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == priceListId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        PriceListRules.EnsureCanModify(priceList);
        var revision = new PriceListRevision
        {
            PriceListId = priceListId,
            CreatedAt = timeProvider.GetLocalNow().Date,
        };
        db.Add(revision);
        await PersistenceGuard.SaveAsync(db, ct);
        return revision.Id;
    }

    public async Task AddItemAsync(AddPriceListItemCommand command, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        var item = new PriceListItem
        {
            PriceListRevisionId = command.PriceListRevisionId,
            RouteId = command.RouteId,
            DeliveryLocationId = command.DeliveryLocationId,
            VehicleTypeId = command.VehicleTypeId,
            UnitPrice = command.UnitPrice,
            Surcharge = command.Surcharge,
        };
        PriceListItemRules.EnsureCanSave(item);
        var revision = await db.PriceListRevisions.AsNoTracking()
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == item.PriceListRevisionId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản bảng giá.");
        PriceListRules.EnsureCanModify(revision.PriceList
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá."));
        if (item.RouteId is int routeIdValue && !await db.Routes.AnyAsync(r => r.Id == routeIdValue, ct))
            throw new InvalidOperationException("Tuyến không tồn tại.");
        if (item.DeliveryLocationId is int locationId && !await db.Locations.AnyAsync(x => x.Id == locationId, ct))
            throw new InvalidOperationException("Điểm đến không tồn tại.");
        if (await db.PriceListItems.AnyAsync(x =>
                x.PriceListRevisionId == item.PriceListRevisionId
                && x.RouteId == item.RouteId
                && x.DeliveryLocationId == item.DeliveryLocationId
                && x.VehicleTypeId == item.VehicleTypeId, ct))
            throw new InvalidOperationException("Dòng giá cho tuyến và loại xe này đã tồn tại trong phiên bản.");
        db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteItemAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Delete);
        var item = await db.FindAsync<PriceListItem>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy dòng giá.");
        var revision = await db.PriceListRevisions.AsNoTracking()
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == item.PriceListRevisionId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản bảng giá.");
        PriceListRules.EnsureCanModify(revision.PriceList
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá."));
        db.Remove(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task LockAsync(int id, string? reason, CancellationToken ct = default)
    {
        if (!current.IsManager)
            throw new InvalidOperationException("Chỉ quản lý được khóa bảng giá.");
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        var list = await db.PriceLists.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        list.LockReason = reason?.Trim();
        PriceListRules.EnsureCanLock(list);
        list.IsLocked = true;
        list.LockedAt = timeProvider.GetLocalNow().DateTime;
        await ConcurrencyConflict.SaveAsync(db, ct);
    }

    public async Task<PriceListFluctuationSummary> SaveFluctuationAsync(
        SavePriceListFluctuationCommand command,
        CancellationToken ct = default)
    {
        RequireManagerForFluctuation();
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        var persisted = command.Id == 0
            ? null
            : await db.PriceListFluctuations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.Id, ct)
              ?? throw new InvalidOperationException("Không tìm thấy biến động giá.");

        if (persisted is not null && persisted.PriceListId != command.PriceListId)
            throw new InvalidOperationException("Biến động giá không thuộc bảng giá đang mở.");

        var fluctuation = new PriceListFluctuation
        {
            Id = command.Id,
            PriceListId = command.PriceListId,
            Type = command.Type,
            Value = command.Value,
            EffectiveFrom = command.EffectiveFrom.Date,
            EffectiveTo = command.EffectiveTo?.Date,
            Reason = command.Reason.Trim(),
            CreatedAt = persisted?.CreatedAt ?? timeProvider.GetLocalNow().DateTime,
            CreatedByUserId = persisted?.CreatedByUserId ?? current.UserId,
            LegacyId = persisted?.LegacyId,
            RowVersion = command.VersionToken.ToArray(),
        };
        PriceFluctuationRules.EnsureCanSave(fluctuation);

        var priceList = await db.PriceLists.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.PriceListId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        PriceFluctuationRules.EnsureCanManage(priceList);

        var existingPeriods = await db.PriceListFluctuations.AsNoTracking()
            .Where(x => x.PriceListId == command.PriceListId && x.Id != command.Id)
            .Select(x => new { x.EffectiveFrom, x.EffectiveTo })
            .ToListAsync(ct);
        if (existingPeriods.Any(x => PriceFluctuationRules.PeriodsOverlap(
                x.EffectiveFrom,
                x.EffectiveTo,
                fluctuation.EffectiveFrom,
                fluctuation.EffectiveTo)))
            throw new InvalidOperationException("Khoảng hiệu lực biến động giá bị trùng với một khoảng hiện có.");

        await EnsureFluctuationKeepsCurrentPricesNonNegativeAsync(fluctuation, ct);

        if (fluctuation.Id == 0)
        {
            db.Add(fluctuation);
        }
        else
        {
            db.Update(fluctuation);
            db.ApplyOriginalRowVersion(fluctuation, command.VersionToken);
        }

        await ConcurrencyConflict.SaveAsync(db, ct);
        var saved = await db.PriceListFluctuations.AsNoTracking()
            .Include(x => x.CreatedByUser)
            .FirstAsync(x => x.Id == fluctuation.Id, ct);
        return ToSummary(saved);
    }

    public async Task DeleteFluctuationAsync(int id, CancellationToken ct = default)
    {
        RequireManagerForFluctuation();
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Delete);
        var fluctuation = await db.FindAsync<PriceListFluctuation>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy biến động giá.");
        var priceList = await db.PriceLists.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fluctuation.PriceListId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        PriceFluctuationRules.EnsureCanManage(priceList);
        db.Remove(fluctuation);
        await ReferentialConflict.SaveAsync(db, fluctuation, ct);
    }

    public async Task<FreightQuote?> GetFreightAsync(
        int? customerId,
        int? routeId,
        int? vehicleTypeId,
        DateTime asOf,
        CancellationToken ct = default)
    {
        if (routeId is null or <= 0 || vehicleTypeId is null) return null;

        var deliveryLocationId = await db.RouteStops.AsNoTracking()
            .Where(x => x.RouteId == routeId)
            .OrderByDescending(x => x.Sequence)
            .Select(x => (int?)x.LocationId)
            .FirstOrDefaultAsync(ct);

        var items = await db.PriceListItems
            .AsNoTracking()
            .Include(i => i.Route)
            .Include(i => i.DeliveryLocation)
            .Include(i => i.VehicleType)
            .Include(i => i.PriceListRevision).ThenInclude(r => r!.PriceList)!.ThenInclude(p => p!.Customer)
            .Where(i => i.VehicleTypeId == vehicleTypeId
                        && (i.RouteId == routeId
                            || (i.RouteId == null && i.DeliveryLocationId == deliveryLocationId)))
            .ToListAsync(ct);

        var hit = PriceListMatchRules.Pick(items, customerId, asOf, routeId);
        if (hit is null) return null;

        var priceListId = hit.PriceListRevision?.PriceListId;
        var fluctuations = priceListId is null
            ? []
            : await db.PriceListFluctuations.AsNoTracking()
                .Where(x => x.PriceListId == priceListId)
                .ToListAsync(ct);
        var fluctuation = PriceFluctuationRules.Pick(fluctuations, asOf);
        return PriceListMatchRules.ToQuote(hit, fluctuation);
    }

    private async Task EnsureFluctuationKeepsCurrentPricesNonNegativeAsync(
        PriceListFluctuation fluctuation,
        CancellationToken ct)
    {
        if (fluctuation.Value >= 0) return;

        var currentRevisionId = await db.PriceListRevisions.AsNoTracking()
            .Where(x => x.PriceListId == fluctuation.PriceListId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);
        if (currentRevisionId is null) return;

        var unitPrices = await db.PriceListItems.AsNoTracking()
            .Where(x => x.PriceListRevisionId == currentRevisionId.Value)
            .Select(x => x.UnitPrice)
            .ToListAsync(ct);
        foreach (var unitPrice in unitPrices)
            PriceFluctuationRules.CalculateAmount(unitPrice, fluctuation);
    }

    private void RequireManagerForFluctuation()
    {
        if (!current.IsManager)
            throw new InvalidOperationException("Chỉ quản lý được điều chỉnh biến động giá.");
    }

    private static PriceListSummary ToSummary(PriceList list) => new(
        list.Id,
        list.Code,
        list.Name,
        list.Description,
        list.CustomerId,
        list.Customer is null ? null : new CustomerOption(
            list.Customer.Id,
            list.Customer.Code,
            list.Customer.Name,
            list.Customer.Address,
            list.Customer.Phone,
            list.Customer.TaxCode,
            list.Customer.IsWalkIn),
        list.EffectiveFrom,
        list.EffectiveTo,
        list.HasPriceFluctuation,
        list.IsLocked,
        list.LockReason,
        list.RowVersion.ToArray());

    private static PriceListItemSummary ToSummary(PriceListItem item) => new(
        item.Id,
        item.PriceListRevisionId,
        item.RouteId,
        item.Route is null ? null : new RouteOption(item.Route.Id, item.Route.Code, item.Route.Name, []),
        item.DeliveryLocationId,
        item.DeliveryLocation is null ? null : new LocationOption(
            item.DeliveryLocation.Id,
            item.DeliveryLocation.Code,
            item.DeliveryLocation.Name,
            item.DeliveryLocation.CityId,
            null),
        item.VehicleTypeId,
        item.VehicleType is null ? null : new VehicleTypeOption(
            item.VehicleType.Id,
            item.VehicleType.Code,
            item.VehicleType.Name,
            item.VehicleType.Tonnage),
        item.UnitPrice,
        item.Surcharge);

    private static PriceListFluctuationSummary ToSummary(PriceListFluctuation fluctuation) => new(
        fluctuation.Id,
        fluctuation.PriceListId,
        fluctuation.Type,
        fluctuation.Value,
        fluctuation.EffectiveFrom,
        fluctuation.EffectiveTo,
        fluctuation.Reason,
        fluctuation.CreatedAt,
        fluctuation.CreatedByUserId,
        fluctuation.CreatedByUser?.DisplayName,
        fluctuation.RowVersion.ToArray());
}
