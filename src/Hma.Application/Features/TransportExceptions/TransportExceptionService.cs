using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Persistence;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Formatting;
using Hma.Application.Abstractions.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.TransportExceptions;

public class TransportExceptionService(
    IHmaDbContext db,
    ICurrentUser current,
    IChangeLogService log,
    TimeProvider timeProvider)
{
    public async Task<List<TransportExceptionSummary>> ListAsync(
        TransportExceptionStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.TransportExceptions.AsNoTracking()
            .Include(x => x.DispatchOrder).ThenInclude(x => x!.Partner)
            .Include(x => x.ExceptionCode)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ReviewedByUser)
            .AsQueryable();
        if (status is not null)
            query = query.Where(x => x.Status == status);
        var rows = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Take(500).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public Task<List<TransportExceptionCodeOption>> CodesAsync(CancellationToken ct = default) =>
        db.TransportExceptionCodes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new TransportExceptionCodeOption(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

    public Task<List<ExceptionOrderOption>> EligibleOrdersAsync(CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Partner)
            .Where(x => !x.IsDeleted
                        && x.Status != DispatchStatus.Cancelled
                        && x.ReconciliationStatus != ReconciliationStatus.Submitted
                        && x.ReconciliationStatus != ReconciliationStatus.Reconciled)
            .OrderByDescending(x => x.PickupAt)
            .Take(500)
            .Select(x => new ExceptionOrderOption(x.Id, x.Code))
            .ToListAsync(ct);

    public Task<int> SaveAsync(SaveTransportExceptionCommand command, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(async token =>
        {
            var item = new TransportException
            {
                Id = command.Id,
                DispatchOrderId = command.DispatchOrderId,
                TransportExceptionCodeId = command.TransportExceptionCodeId,
                OccurredAt = command.OccurredAt,
                Description = command.Description,
                CustomerCharge = command.CustomerCharge,
                PartnerCost = command.PartnerCost,
                RowVersion = command.VersionToken.ToArray(),
            };
            await SaveCoreAsync(item, token);
            return item.Id;
        }, ct);

    private async Task SaveCoreAsync(TransportException item, CancellationToken ct)
    {
        var isNew = item.Id == 0;
        PermissionGuard.RequireSave(current, ScreenKeys.TransportExceptions, isNew);
        var order = await db.DispatchOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == item.DispatchOrderId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
        var code = await db.TransportExceptionCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == item.TransportExceptionCodeId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mã sự cố.");
        if (!isNew)
        {
            var existing = await db.TransportExceptions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == item.Id, ct)
                ?? throw new InvalidOperationException("Không tìm thấy sự cố vận tải.");
            item.Status = existing.Status;
            item.CreatedAt = existing.CreatedAt;
            item.CreatedByUserId = existing.CreatedByUserId;
            item.VoidedAt = existing.VoidedAt;
            item.VoidedByUserId = existing.VoidedByUserId;
            item.VoidReason = existing.VoidReason;
        }
        TransportExceptionRules.EnsureCanSave(item, order, code);

        var originalVersion = isNew ? [] : item.RowVersion.ToArray();
        item.CodeSnapshot = code.Code;
        item.NameSnapshot = code.Name;
        item.Description = item.Description.Trim();
        item.DispatchOrder = null;
        item.ExceptionCode = null;
        item.CreatedByUser = null;
        item.SubmittedByUser = null;
        item.ReviewedByUser = null;
        item.VoidedByUser = null;
        if (isNew)
        {
            item.CreatedAt = timeProvider.GetLocalNow().DateTime;
            item.CreatedByUserId = current.UserId;
            db.Add(item);
        }
        else
        {
            item.Status = TransportExceptionStatus.Draft;
            item.SubmittedAt = null;
            item.SubmittedByUserId = null;
            item.ReviewedAt = null;
            item.ReviewedByUserId = null;
            item.ReviewNote = null;
            db.Update(item);
            db.ApplyOriginalRowVersion(item, originalVersion);
        }

        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", item.Id, isNew ? "Create" : "Update",
            $"{item.CodeSnapshot} · {item.Description}", null,
            new { item.DispatchOrderId, item.CustomerCharge, item.PartnerCost }, ct);
    }

    public Task SubmitAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitCoreAsync(id, token), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => DeleteCoreAsync(id, token), ct);

    private async Task DeleteCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.TransportExceptions, PermissionAction.Delete);
        var item = await db.FindAsync<TransportException>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy sự cố vận tải.");
        TransportExceptionRules.EnsureCanDelete(item);
        var summary = $"{item.CodeSnapshot} · {item.Description}";
        db.Remove(item);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", id, "Delete", $"Xóa sự cố nháp: {summary}", null, null, ct);
    }

    private async Task SubmitCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.TransportExceptions, PermissionAction.Update);
        var item = await FindTrackedAsync(id, ct);
        TransportExceptionRules.EnsureCanSubmit(item, current.UserId);
        item.Status = TransportExceptionStatus.Submitted;
        item.SubmittedAt = timeProvider.GetLocalNow().DateTime;
        item.SubmittedByUserId = current.UserId;
        item.ReviewedAt = null;
        item.ReviewedByUserId = null;
        item.ReviewNote = null;
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", id, "Submit", "Gửi duyệt sự cố vận tải", null, null, ct);
    }

    public Task ApproveAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ApproveCoreAsync(id, token), ct);

    private async Task ApproveCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.TransportExceptions, PermissionAction.Update);
        var item = await FindTrackedAsync(id, ct);
        var order = item.DispatchOrder!;
        TransportExceptionRules.EnsureCanReview(item, order, current.UserId, current.IsManager);
        order.ApplyApprovedException(item.CustomerCharge, item.PartnerCost);
        AmountText.Refresh(order);
        item.Status = TransportExceptionStatus.Approved;
        item.ReviewedAt = timeProvider.GetLocalNow().DateTime;
        item.ReviewedByUserId = current.UserId;
        item.ReviewNote = null;
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", id, "Approve", "Duyệt sự cố vận tải", null,
            new { item.CustomerCharge, item.PartnerCost }, ct);
        await log.RecordAsync("DispatchOrder", order.Id, "ApplyTransportException",
            $"Áp dụng sự cố {item.CodeSnapshot}: thu {item.CustomerCharge:N0}, chi {item.PartnerCost:N0}",
            null, new { order.ApprovedExceptionRevenue, order.ApprovedExceptionCost, order.TotalAmount, order.BuyTotal }, ct);
    }

    public Task RejectAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => RejectCoreAsync(id, reason, token), ct);

    private async Task RejectCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.TransportExceptions, PermissionAction.Update);
        var item = await FindTrackedAsync(id, ct);
        TransportExceptionRules.EnsureCanReview(
            item, item.DispatchOrder!, current.UserId, current.IsManager, reason);
        item.Status = TransportExceptionStatus.Rejected;
        item.ReviewedAt = timeProvider.GetLocalNow().DateTime;
        item.ReviewedByUserId = current.UserId;
        item.ReviewNote = reason.Trim();
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", id, "Reject", "Từ chối sự cố vận tải", null,
            new { Reason = item.ReviewNote }, ct);
    }

    public Task VoidAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => VoidCoreAsync(id, reason, token), ct);

    private async Task VoidCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.TransportExceptions, PermissionAction.Update);
        var item = await FindTrackedAsync(id, ct);
        var order = item.DispatchOrder!;
        TransportExceptionRules.EnsureCanVoid(item, order, current.IsManager, reason);
        order.ReverseApprovedException(item.CustomerCharge, item.PartnerCost);
        AmountText.Refresh(order);
        item.Status = TransportExceptionStatus.Voided;
        item.VoidedAt = timeProvider.GetLocalNow().DateTime;
        item.VoidedByUserId = current.UserId;
        item.VoidReason = reason.Trim();
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("TransportException", id, "Void", "Hủy sự cố vận tải đã duyệt", null,
            new { Reason = item.VoidReason }, ct);
        await log.RecordAsync("DispatchOrder", order.Id, "ReverseTransportException",
            $"Hoàn sự cố {item.CodeSnapshot}: thu {item.CustomerCharge:N0}, chi {item.PartnerCost:N0}",
            null, new { order.ApprovedExceptionRevenue, order.ApprovedExceptionCost, order.TotalAmount, order.BuyTotal }, ct);
    }

    private async Task<TransportException> FindTrackedAsync(int id, CancellationToken ct) =>
        await db.TransportExceptions
            .Include(x => x.DispatchOrder)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new InvalidOperationException("Không tìm thấy sự cố vận tải.");

    private static TransportExceptionSummary ToSummary(TransportException item) => new(
        item.Id,
        item.DispatchOrderId,
        item.DispatchOrder is null ? null : new ExceptionOrderOption(item.DispatchOrder.Id, item.DispatchOrder.Code),
        item.TransportExceptionCodeId,
        item.ExceptionCode is null ? null : new TransportExceptionCodeOption(
            item.ExceptionCode.Id, item.ExceptionCode.Code, item.ExceptionCode.Name),
        item.CodeSnapshot,
        item.NameSnapshot,
        item.OccurredAt,
        item.Description,
        item.CustomerCharge,
        item.PartnerCost,
        item.Status,
        item.SubmittedByUserId,
        item.ReviewNote,
        item.VoidReason,
        item.RowVersion.ToArray());
}
