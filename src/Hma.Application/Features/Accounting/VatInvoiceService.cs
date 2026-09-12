using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Formatting;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Accounting;

public class VatInvoiceService(IHmaDbContext db, IDocumentNumberService numbers, TimeProvider timeProvider)
{
    public async Task<List<VatInvoiceDetails>> SearchAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.VatInvoices.AsNoTracking().Include(i => i.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(i => i.Code.Contains(code));
        if (from is not null) q = q.Where(i => i.InvoiceDate >= from);
        if (to is not null) q = q.Where(i => i.InvoiceDate <= to);
        var rows = await q.OrderByDescending(i => i.InvoiceDate).Take(500).ToListAsync(ct);
        return rows.Select(ToDetails).ToList();
    }

    public async Task<VatInvoiceDetails?> GetAsync(int id, CancellationToken ct = default)
    {
        var invoice = await db.VatInvoices.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.DispatchOrder).ThenInclude(d => d!.SenderCustomer)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return invoice is null ? null : ToDetails(invoice);
    }

    public async Task<VatInvoiceDetails> CreateNewAsync(CancellationToken ct = default)
    {
        var vat = await db.Parameters.FirstOrDefaultAsync(p => p.Key == "VatRate", ct);
        var rate = 10m;
        if (vat?.Value is not null && decimal.TryParse(vat.Value, out var parsed)) rate = parsed;
        return ToDetails(new VatInvoice
        {
            Code = await numbers.NextAsync("vat-invoice", ct),
            InvoiceDate = timeProvider.GetLocalNow().Date,
            VatRate = rate,
            PaymentMethodText = "TM/CK",
            Unit = "KG",
            GoodsName = "Cước vận chuyển"
        });
    }

    public async Task<List<InvoiceOrderOption>> AvailableOrdersAsync(int? customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var used = db.VatInvoiceLines.Select(l => l.DispatchOrderId);
        var q = db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Include(d => d.SenderCustomer)
            .Where(d => !used.Contains(d.Id));
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId);
        if (from is not null) q = q.Where(d => d.PickupAt >= from);
        if (to is not null) q = q.Where(d => d.PickupAt <= to);
        var rows = await q.OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);
        return rows.Select(ToOption).ToList();
    }

    public async Task<VatInvoiceDetails> SaveAsync(SaveVatInvoiceCommand command, CancellationToken ct = default)
    {
        var invoice = command.Id == 0
            ? new VatInvoice()
            : await db.VatInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == command.Id, ct)
              ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        invoice.Code = command.Code;
        invoice.InvoiceDate = command.InvoiceDate;
        invoice.CustomerId = command.CustomerId;
        invoice.EmployeeId = command.EmployeeId;
        invoice.IsByCustomer = command.IsByCustomer;
        invoice.GoodsName = command.GoodsName;
        invoice.PaymentMethodText = command.PaymentMethodText;
        invoice.Unit = command.Unit;
        invoice.Quantity = command.Quantity;
        invoice.VatRate = command.VatRate;
        invoice.TotalAmount = command.TotalAmount;
        if (command.DispatchOrderIds.Count > 0)
        {
            var orderAmounts = await db.DispatchOrders.AsNoTracking()
                .Where(x => command.DispatchOrderIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.TotalAmount, ct);
            invoice.TotalAmount = command.DispatchOrderIds.Sum(id => orderAmounts.GetValueOrDefault(id));
        }
        var amounts = VatInvoiceCalculation.FromTotal(
            invoice.TotalAmount, invoice.VatRate, command.Amount, command.VatAmount);
        invoice.Amount = amounts.Amount;
        invoice.VatAmount = amounts.VatAmount;
        invoice.AmountInWords = AmountText.From(invoice.TotalAmount);
        foreach (var line in invoice.Lines.ToList()) db.Remove(line);
        invoice.Lines.Clear();
        foreach (var dispatchOrderId in command.DispatchOrderIds)
            invoice.Lines.Add(new VatInvoiceLine { DispatchOrderId = dispatchOrderId });
        if (invoice.Id == 0) db.Add(invoice);
        await PersistenceGuard.SaveAsync(db, ct);
        return await GetAsync(invoice.Id, ct) ?? ToDetails(invoice);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.FindAsync<VatInvoice>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        db.Remove(entity);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    private static VatInvoiceDetails ToDetails(VatInvoice invoice) => new(
        invoice.Id, invoice.Code, invoice.InvoiceDate, invoice.CustomerId,
        invoice.Customer is null ? null : new Hma.Application.Features.Customers.CustomerOption(
            invoice.Customer.Id, invoice.Customer.Code, invoice.Customer.Name, invoice.Customer.Address,
            invoice.Customer.Phone, invoice.Customer.TaxCode, invoice.Customer.IsWalkIn),
        invoice.EmployeeId, invoice.IsByCustomer, invoice.GoodsName, invoice.PaymentMethodText,
        invoice.Unit, invoice.Quantity, invoice.Amount, invoice.VatRate, invoice.VatAmount,
        invoice.TotalAmount, invoice.AmountInWords,
        invoice.Lines.Where(x => x.DispatchOrder is not null)
            .Select(x => ToOption(x.DispatchOrder!)).ToList());

    private static InvoiceOrderOption ToOption(DispatchOrder order) => new(
        order.Id, order.Code, order.TotalAmount,
        order.SenderCustomer is null ? null : new Hma.Application.Features.Customers.CustomerOption(
            order.SenderCustomer.Id, order.SenderCustomer.Code, order.SenderCustomer.Name,
            order.SenderCustomer.Address, order.SenderCustomer.Phone, order.SenderCustomer.TaxCode,
            order.SenderCustomer.IsWalkIn));
}
