using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CashDocumentService(IHmaDbContext db, IDocumentNumberService numbers)
{
    public Task<List<CashReceipt>> SearchReceiptsAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.CashReceipts.AsNoTracking().Include(r => r.Customer).Include(r => r.DispatchOrder).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(r => r.Code.Contains(code));
        if (from is not null) q = q.Where(r => r.DocumentDate >= from);
        if (to is not null) q = q.Where(r => r.DocumentDate <= to);
        return q.OrderByDescending(r => r.DocumentDate).Take(500).ToListAsync(ct);
    }

    public Task<List<CashPayment>> SearchPaymentsAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.CashPayments.AsNoTracking().Include(p => p.Customer).Include(p => p.DriverEmployee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(p => p.Code.Contains(code));
        if (from is not null) q = q.Where(p => p.DocumentDate >= from);
        if (to is not null) q = q.Where(p => p.DocumentDate <= to);
        return q.OrderByDescending(p => p.DocumentDate).Take(500).ToListAsync(ct);
    }

    public async Task<CashReceipt> NewReceiptAsync(CancellationToken ct = default) =>
        new() { Code = await numbers.NextAsync("cash-receipt", ct), DocumentDate = DateTime.Today, Kind = CashReceiptKind.Customer };

    public async Task<CashPayment> NewPaymentAsync(CancellationToken ct = default) =>
        new() { Code = await numbers.NextAsync("cash-payment", ct), DocumentDate = DateTime.Today, Kind = CashPaymentKind.Customer };

    public async Task SaveReceiptAsync(CashReceipt receipt, CancellationToken ct = default)
    {
        MoneyRules.EnsurePositive(receipt.Amount, "Số tiền");
        receipt.AmountInWords = AmountText.From(receipt.Amount);
        if (receipt.Id == 0) db.Add(receipt);
        else db.Update(receipt);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SavePaymentAsync(CashPayment payment, CancellationToken ct = default)
    {
        MoneyRules.EnsurePositive(payment.Amount, "Số tiền");
        payment.AmountInWords = AmountText.From(payment.Amount);
        if (payment.Id == 0) db.Add(payment);
        else db.Update(payment);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteReceiptAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.FindAsync<CashReceipt>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy phiếu thu.");
        db.Remove(entity);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeletePaymentAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.FindAsync<CashPayment>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy phiếu chi.");
        db.Remove(entity);
        await PersistenceGuard.SaveAsync(db, ct);
    }
}
