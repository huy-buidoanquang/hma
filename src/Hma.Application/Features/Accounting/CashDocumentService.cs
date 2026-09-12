using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Formatting;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Accounting;

public class CashDocumentService(IHmaDbContext db, IDocumentNumberService numbers, TimeProvider timeProvider)
{
    public async Task<List<CashReceiptDetails>> SearchReceiptsAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.CashReceipts.AsNoTracking().Include(r => r.Customer).Include(r => r.DispatchOrder).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(r => r.Code.Contains(code));
        if (from is not null) q = q.Where(r => r.DocumentDate >= from);
        if (to is not null) q = q.Where(r => r.DocumentDate <= to);
        var rows = await q.OrderByDescending(r => r.DocumentDate).Take(500).ToListAsync(ct);
        return rows.Select(ToDetails).ToList();
    }

    public async Task<List<CashPaymentDetails>> SearchPaymentsAsync(string? code, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = db.CashPayments.AsNoTracking().Include(p => p.Customer).Include(p => p.DriverEmployee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(p => p.Code.Contains(code));
        if (from is not null) q = q.Where(p => p.DocumentDate >= from);
        if (to is not null) q = q.Where(p => p.DocumentDate <= to);
        var rows = await q.OrderByDescending(p => p.DocumentDate).Take(500).ToListAsync(ct);
        return rows.Select(ToDetails).ToList();
    }

    public async Task<CashReceiptDetails> NewReceiptAsync(CancellationToken ct = default) =>
        ToDetails(new CashReceipt { Code = await numbers.NextAsync("cash-receipt", ct), DocumentDate = timeProvider.GetLocalNow().Date, Kind = CashReceiptKind.Customer });

    public async Task<CashPaymentDetails> NewPaymentAsync(CancellationToken ct = default) =>
        ToDetails(new CashPayment { Code = await numbers.NextAsync("cash-payment", ct), DocumentDate = timeProvider.GetLocalNow().Date, Kind = CashPaymentKind.Customer });

    public async Task<CashReceiptDetails> SaveReceiptAsync(SaveCashReceiptCommand command, CancellationToken ct = default)
    {
        MoneyRules.EnsurePositive(command.Amount, "Số tiền");
        var receipt = command.Id == 0
            ? new CashReceipt()
            : await db.CashReceipts.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
              ?? throw new InvalidOperationException("Không tìm thấy phiếu thu.");
        receipt.Code = command.Code;
        receipt.DocumentDate = command.DocumentDate;
        receipt.Kind = command.Kind;
        receipt.DispatchOrderId = command.DispatchOrderId;
        receipt.CustomerId = command.CustomerId;
        receipt.Amount = command.Amount;
        receipt.PayerName = command.PayerName;
        receipt.Address = command.Address;
        receipt.Reason = command.Reason;
        receipt.EmployeeId = command.EmployeeId;
        receipt.AmountInWords = AmountText.From(receipt.Amount);
        if (receipt.Id == 0) db.Add(receipt);
        await PersistenceGuard.SaveAsync(db, ct);
        return ToDetails(receipt);
    }

    public async Task<CashPaymentDetails> SavePaymentAsync(SaveCashPaymentCommand command, CancellationToken ct = default)
    {
        MoneyRules.EnsurePositive(command.Amount, "Số tiền");
        var payment = command.Id == 0
            ? new CashPayment()
            : await db.CashPayments.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
              ?? throw new InvalidOperationException("Không tìm thấy phiếu chi.");
        payment.Code = command.Code;
        payment.DocumentDate = command.DocumentDate;
        payment.Kind = command.Kind;
        payment.CustomerId = command.CustomerId;
        payment.DriverEmployeeId = command.DriverEmployeeId;
        payment.Amount = command.Amount;
        payment.PayeeName = command.PayeeName;
        payment.Address = command.Address;
        payment.Reason = command.Reason;
        payment.EmployeeId = command.EmployeeId;
        payment.AmountInWords = AmountText.From(payment.Amount);
        if (payment.Id == 0) db.Add(payment);
        await PersistenceGuard.SaveAsync(db, ct);
        return ToDetails(payment);
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

    private static CashReceiptDetails ToDetails(CashReceipt receipt) => new(
        receipt.Id, receipt.Code, receipt.DocumentDate, receipt.Kind, receipt.DispatchOrderId,
        receipt.DispatchOrder is null ? null : new Hma.Application.Features.TransportExceptions.ExceptionOrderOption(
            receipt.DispatchOrder.Id, receipt.DispatchOrder.Code),
        receipt.CustomerId,
        receipt.Customer is null ? null : new Hma.Application.Features.Customers.CustomerOption(
            receipt.Customer.Id, receipt.Customer.Code, receipt.Customer.Name, receipt.Customer.Address,
            receipt.Customer.Phone, receipt.Customer.TaxCode, receipt.Customer.IsWalkIn),
        receipt.Amount, receipt.AmountInWords, receipt.PayerName, receipt.Address, receipt.Reason, receipt.EmployeeId);

    internal static CashPaymentDetails ToDetails(CashPayment payment) => new(
        payment.Id, payment.Code, payment.DocumentDate, payment.Kind, payment.CustomerId,
        payment.Customer is null ? null : new Hma.Application.Features.Customers.CustomerOption(
            payment.Customer.Id, payment.Customer.Code, payment.Customer.Name, payment.Customer.Address,
            payment.Customer.Phone, payment.Customer.TaxCode, payment.Customer.IsWalkIn),
        payment.DriverEmployeeId,
        payment.DriverEmployee is null ? null : new Hma.Application.Features.Catalogs.EmployeeOption(
            payment.DriverEmployee.Id, payment.DriverEmployee.Code, payment.DriverEmployee.Name,
            payment.DriverEmployee.DepartmentId, null, payment.DriverEmployee.JobTitleId, null),
        payment.Amount, payment.AmountInWords, payment.PayeeName, payment.Address, payment.Reason, payment.EmployeeId);
}
