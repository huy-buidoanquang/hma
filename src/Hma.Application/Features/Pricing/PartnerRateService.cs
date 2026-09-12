using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Persistence;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Catalogs;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Pricing;

public class PartnerRateService(IHmaDbContext db, ICurrentUser current, TimeProvider timeProvider)
{
    public Task<List<PartnerRateSummary>> ListAsync(CancellationToken ct = default) =>
        db.PartnerRates.AsNoTracking()
            .OrderBy(x => x.Partner!.Code)
            .ThenBy(x => x.Route!.Code)
            .ThenByDescending(x => x.EffectiveFrom)
            .Take(500)
            .Select(x => new PartnerRateSummary(
                x.Id, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent),
                x.RouteId,
                x.Route == null ? null : new RouteOption(x.Route.Id, x.Route.Code, x.Route.Name, Array.Empty<RouteStopOption>()),
                x.VehicleTypeId,
                x.VehicleType == null ? null : new VehicleTypeOption(
                    x.VehicleType.Id, x.VehicleType.Code, x.VehicleType.Name, x.VehicleType.Tonnage),
                x.EffectiveFrom, x.EffectiveTo, x.UnitPrice, x.Surcharge, x.CreatedAt, x.CreatedByUserId,
                x.RowVersion))
            .ToListAsync(ct);

    public async Task SaveAsync(SavePartnerRateCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PartnerRates, command.Id == 0);
        var rate = command.Id == 0
            ? new PartnerRate()
            : await db.FindAsync<PartnerRate>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy giá mua đối tác.");
        rate.PartnerId = command.PartnerId;
        rate.RouteId = command.RouteId;
        rate.VehicleTypeId = command.VehicleTypeId;
        rate.EffectiveFrom = command.EffectiveFrom;
        rate.EffectiveTo = command.EffectiveTo;
        rate.UnitPrice = command.UnitPrice;
        rate.Surcharge = command.Surcharge;
        PartnerRateRules.EnsureCanSave(rate);
        if (!await db.Partners.AnyAsync(x => x.Id == rate.PartnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");
        if (!await db.Routes.AnyAsync(x => x.Id == rate.RouteId, ct))
            throw new InvalidOperationException("Tuyến không tồn tại.");
        if (!await db.VehicleTypes.AnyAsync(x => x.Id == rate.VehicleTypeId, ct))
            throw new InvalidOperationException("Loại xe không tồn tại.");

        var candidates = await db.PartnerRates.AsNoTracking()
            .Where(x => x.Id != rate.Id
                        && x.PartnerId == rate.PartnerId
                        && x.RouteId == rate.RouteId
                        && x.VehicleTypeId == rate.VehicleTypeId)
            .ToListAsync(ct);
        if (candidates.Any(x => PartnerRateRules.PeriodsOverlap(
                x.EffectiveFrom, x.EffectiveTo, rate.EffectiveFrom, rate.EffectiveTo)))
            throw new InvalidOperationException("Khoảng hiệu lực giá mua bị trùng với một dòng hiện có.");

        if (rate.Id == 0)
        {
            rate.CreatedAt = timeProvider.GetLocalNow().DateTime;
            rate.CreatedByUserId = current.UserId;
            db.Add(rate);
        }
        else
        {
            db.ApplyOriginalRowVersion(rate, command.VersionToken);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerRates, PermissionAction.Delete);
        var rate = await db.FindAsync<PartnerRate>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy giá mua đối tác.");
        db.Remove(rate);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    internal Task<PartnerRate?> GetRateAsync(
        int partnerId,
        int routeId,
        int vehicleTypeId,
        DateTime asOf,
        CancellationToken ct = default) =>
        db.PartnerRates.AsNoTracking()
            .Include(x => x.Partner)
            .Include(x => x.Route)
            .Include(x => x.VehicleType)
            .Where(x => x.PartnerId == partnerId
                        && x.RouteId == routeId
                        && x.VehicleTypeId == vehicleTypeId
                        && x.EffectiveFrom <= asOf.Date
                        && (x.EffectiveTo == null || x.EffectiveTo >= asOf.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
}
