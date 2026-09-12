using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Application.Features.Dispatching.Import;
using Hma.Domain.Entities;
using Hma.Domain.Normalization;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class VehicleService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<VehicleSummary>> SearchAsync(string? plate = null, CancellationToken ct = default)
    {
        var query = db.Vehicles.AsNoTracking().Include(x => x.Partner).Include(x => x.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(plate)) query = query.Where(x => x.PlateNumber.Contains(plate));
        return query.OrderBy(x => x.PlateNumber).Take(500)
            .Select(x => new VehicleSummary(x.Id, x.PlateNumber, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent),
                x.VehicleTypeId,
                x.VehicleType == null ? null : new VehicleTypeOption(
                    x.VehicleType.Id, x.VehicleType.Code, x.VehicleType.Name, x.VehicleType.Tonnage),
                x.Tonnage))
            .ToListAsync(ct);
    }

    public async Task SaveAsync(SaveVehicleCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Vehicles, command.Id == 0);
        var item = command.Id == 0
            ? new Vehicle()
            : await db.FindAsync<Vehicle>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        item.PlateNumber = command.PlateNumber;
        item.PartnerId = command.PartnerId;
        item.VehicleTypeId = command.VehicleTypeId;
        item.Tonnage = command.Tonnage;
        VehicleRules.EnsureCanSave(item);
        var plateKey = DispatchImportMatching.NormalizePlate(item.PlateNumber);
        var otherPlates = await db.Vehicles.AsNoTracking().Where(x => x.Id != item.Id).Select(x => x.PlateNumber).ToListAsync(ct);
        if (otherPlates.Any(existing => DispatchImportMatching.NormalizePlate(existing) == plateKey))
            throw new InvalidOperationException("Biển số đã tồn tại.");
        if (!await db.Partners.AnyAsync(x => x.Id == item.PartnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");
        if (item.VehicleTypeId is int typeId && !await db.VehicleTypes.AnyAsync(x => x.Id == typeId, ct))
            throw new InvalidOperationException("Loại xe không tồn tại.");

        if (item.Id == 0) db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
        await SyncAliasesAsync(item, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Vehicles, PermissionAction.Delete);
        var entity = await db.FindAsync<Vehicle>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }

    private async Task SyncAliasesAsync(Vehicle item, CancellationToken ct)
    {
        var wanted = VehiclePlateRules.DictionaryForms(item.PlateNumber).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var currentAliases = await db.VehicleAliases.Where(x => x.VehicleId == item.Id).ToListAsync(ct);
        foreach (var extra in currentAliases.Where(x => wanted.All(alias => !AliasText.EqualsNormalized(alias, x.Alias))))
            db.Remove(extra);
        var taken = await db.VehicleAliases.AsNoTracking().Where(x => x.VehicleId != item.Id).Select(x => x.Alias).ToListAsync(ct);
        foreach (var form in wanted)
        {
            if (currentAliases.Any(x => AliasText.EqualsNormalized(x.Alias, form))) continue;
            if (taken.Any(x => AliasText.EqualsNormalized(x, form)))
                throw new InvalidOperationException($"Bí danh biển «{form}» đã gắn với xe khác.");
            db.Add(new VehicleAlias { Alias = form, VehicleId = item.Id });
        }
        await PersistenceGuard.SaveAsync(db, ct);
    }
}
