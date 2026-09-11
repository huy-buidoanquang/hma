using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class PartnerRateService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<PartnerRate>> ListAsync(CancellationToken ct = default) =>
        db.PartnerRates.AsNoTracking()
            .Include(x => x.Partner)
            .Include(x => x.Route)
            .Include(x => x.VehicleType)
            .OrderBy(x => x.Partner!.Code)
            .ThenBy(x => x.Route!.Code)
            .ThenByDescending(x => x.EffectiveFrom)
            .Take(500)
            .ToListAsync(ct);

    public async Task SaveAsync(PartnerRate rate, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PartnerRates, rate.Id == 0);
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

        rate.Partner = null;
        rate.Route = null;
        rate.VehicleType = null;
        rate.CreatedByUser = null;
        if (rate.Id == 0)
        {
            rate.CreatedAt = DateTime.Now;
            rate.CreatedByUserId = current.User?.Id;
            db.Add(rate);
        }
        else
        {
            var originalVersion = rate.RowVersion.ToArray();
            db.Update(rate);
            db.ApplyOriginalRowVersion(rate, originalVersion);
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

    public Task<PartnerRate?> GetRateAsync(
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
