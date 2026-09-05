using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class VatInvoiceService(IHmaDbContext db, IDocumentNumberService numbers)
{
    public Task<List<VatInvoice>> SearchAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.VatInvoices.AsNoTracking().Include(i => i.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(i => i.Code.Contains(code));
        if (from is not null) q = q.Where(i => i.InvoiceDate >= from);
        if (to is not null) q = q.Where(i => i.InvoiceDate <= to);
        return q.OrderByDescending(i => i.InvoiceDate).Take(500).ToListAsync(ct);
    }

    public Task<VatInvoice?> GetAsync(int id, CancellationToken ct = default) =>
        db.VatInvoices.Include(i => i.Lines).ThenInclude(l => l.DispatchOrder).Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<VatInvoice> CreateNewAsync(CancellationToken ct = default)
    {
        var vat = await db.Parameters.FirstOrDefaultAsync(p => p.Key == "VatRate", ct);
        var rate = 10m;
        if (vat?.Value is not null && decimal.TryParse(vat.Value, out var parsed)) rate = parsed;
        return new VatInvoice
        {
            Code = await numbers.NextAsync("vat-invoice", ct),
            InvoiceDate = DateTime.Today,
            VatRate = rate,
            PaymentMethodText = "TM/CK",
            Unit = "KG",
            GoodsName = "Cước vận chuyển"
        };
    }

    public Task<List<DispatchOrder>> AvailableOrdersAsync(int? customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var used = db.VatInvoiceLines.Select(l => l.DispatchOrderId);
        var q = db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Where(d => !used.Contains(d.Id));
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId);
        if (from is not null) q = q.Where(d => d.PickupAt >= from);
        if (to is not null) q = q.Where(d => d.PickupAt <= to);
        return q.OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);
    }

    public async Task SaveAsync(VatInvoice invoice, CancellationToken ct = default)
    {
        invoice.TotalAmount = invoice.Lines
            .Select(l => l.DispatchOrder?.TotalAmount ?? 0)
            .DefaultIfEmpty(invoice.TotalAmount)
            .Sum();
        if (invoice.Lines.Count > 0)
            invoice.TotalAmount = invoice.Lines.Sum(l => l.DispatchOrder?.TotalAmount ?? 0);
        invoice.RecalculateFromTotal();
        invoice.AmountInWords = AmountText.From(invoice.TotalAmount);
        if (invoice.Id == 0) db.Add(invoice);
        else db.Update(invoice);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.FindAsync<VatInvoice>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        db.Remove(entity);
        await PersistenceGuard.SaveAsync(db, ct);
    }
}
