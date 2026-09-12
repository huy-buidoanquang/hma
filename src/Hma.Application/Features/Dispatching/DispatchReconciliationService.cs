using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Dispatching;

public sealed class DispatchReconciliationService(
    IHmaDbContext db,
    DispatchOrderQueryService queries,
    ICurrentUser current,
    IChangeLogService log,
    TimeProvider timeProvider)
{
    public Task SubmitAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitCoreAsync(id, token), ct);

    public Task ApproveAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ApproveCoreAsync(id, token), ct);

    public Task RejectAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => RejectCoreAsync(id, reason, token), ct);

    public Task ReopenAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => ReopenCoreAsync(id, token), ct);

    private async Task SubmitCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await queries.GetEntityAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanSubmitReconciliation(order, current.UserId);
        var previous = order.ReconciliationStatus;
        order.ReconciliationStatus = ReconciliationStatus.Submitted;
        order.ReconciliationSubmittedAt = timeProvider.GetLocalNow().DateTime;
        order.ReconciliationSubmittedByUserId = current.UserId;
        order.ReconciliationRejectedAt = null;
        order.ReconciliationRejectedByUserId = null;
        order.ReconciliationRejectionReason = null;
        await SaveAsync(order, ct);
        await log.RecordAsync("DispatchOrder", id, "SubmitReconciliation", "Gửi duyệt đối soát",
            new { Status = previous }, new { Status = ReconciliationStatus.Submitted }, ct);
    }

    private async Task ApproveCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await queries.GetEntityAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        if (order.ReconciliationStatus == ReconciliationStatus.Reconciled)
            return;
        DispatchWorkflowRules.EnsureCanApproveReconciliation(order, current.UserId);
        order.ReconciliationStatus = ReconciliationStatus.Reconciled;
        order.ReconciledAt = timeProvider.GetLocalNow().DateTime;
        order.ReconciledByUserId = current.UserId;
        await SaveAsync(order, ct);
        await log.RecordAsync("DispatchOrder", id, "Reconcile", "Đã đối soát",
            new { Status = ReconciliationStatus.Submitted }, new { Status = ReconciliationStatus.Reconciled }, ct);
    }

    private async Task RejectCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        var order = await queries.GetEntityAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        DispatchWorkflowRules.EnsureCanRejectReconciliation(order, current.UserId, reason);
        order.ReconciliationStatus = ReconciliationStatus.Rejected;
        order.ReconciliationRejectedAt = timeProvider.GetLocalNow().DateTime;
        order.ReconciliationRejectedByUserId = current.UserId;
        order.ReconciliationRejectionReason = reason.Trim();
        await SaveAsync(order, ct);
        await log.RecordAsync("DispatchOrder", id, "RejectReconciliation", "Từ chối đối soát",
            new { Status = ReconciliationStatus.Submitted },
            new { Status = ReconciliationStatus.Rejected, Reason = order.ReconciliationRejectionReason }, ct);
    }

    private async Task ReopenCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Reconcile, PermissionAction.Update);
        if (!current.IsManager)
            throw new InvalidOperationException("Chỉ quản lý được hủy đối soát.");
        var order = await db.FindAsync<DispatchOrder>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy lệnh.");
        if (await db.FreightStatementLines.AnyAsync(x => x.DispatchOrderId == id, ct))
            throw new InvalidOperationException("Lệnh đã nằm trong bảng kê — không hủy đối soát.");
        order.ReconciliationStatus = ReconciliationStatus.Pending;
        order.ReconciledAt = null;
        order.ReconciledByUserId = null;
        order.ReconciliationSubmittedAt = null;
        order.ReconciliationSubmittedByUserId = null;
        order.ReconciliationRejectedAt = null;
        order.ReconciliationRejectedByUserId = null;
        order.ReconciliationRejectionReason = null;
        await SaveAsync(order, ct);
        await log.RecordAsync("DispatchOrder", id, "Unreconcile", "Hủy đối soát", null, null, ct);
    }

    private async Task SaveAsync(DispatchOrder order, CancellationToken ct)
    {
        db.Update(order);
        db.ApplyOriginalRowVersion(order, order.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
    }
}
