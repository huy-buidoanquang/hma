using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Dispatching;

public sealed class DispatchGridService(
    IHmaDbContext db,
    DispatchOrderQueryService queries,
    DispatchOrderEditorService editor,
    ICurrentUser current,
    IChangeLogService log)
{
    public Task ShiftBillingPeriodAsync(IReadOnlyList<int> ids, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ShiftBillingPeriodCoreAsync(ids, token), ct);

    public async Task SaveRowAsync(
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
        RequireEdit();
        var entity = await queries.GetEntityAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
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
            var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vehicleId, ct);
            entity.VehicleTypeId = vehicle?.VehicleTypeId;
        }
        await editor.SaveEntityAsync(entity, ct);
    }

    private async Task ShiftBillingPeriodCoreAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        RequireEdit();
        var entities = new List<DispatchOrder>();
        foreach (var id in ids.Distinct())
        {
            var entity = await db.DispatchOrders.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id, ct)
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

    private void RequireEdit()
    {
        if (current.Can(ScreenKeys.DispatchGridEdit, PermissionAction.Update)
            || current.Can(ScreenKeys.DispatchOrders, PermissionAction.Update))
            return;
        throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
    }
}
