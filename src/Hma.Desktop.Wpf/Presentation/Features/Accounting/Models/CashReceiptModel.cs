using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.TransportExceptions;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;

public sealed partial class CashReceiptModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private DateTime documentDate = DateTime.Today;
    [ObservableProperty] private CashReceiptKind kind = CashReceiptKind.Customer;
    [ObservableProperty] private int? dispatchOrderId;
    [ObservableProperty] private ExceptionOrderOption? dispatchOrder;
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private CustomerOption? customer;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string? amountInWords;
    [ObservableProperty] private string? payerName;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? reason;
    [ObservableProperty] private int? employeeId;

    public static CashReceiptModel From(CashReceiptDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        DocumentDate = item.DocumentDate,
        Kind = item.Kind,
        DispatchOrderId = item.DispatchOrderId,
        DispatchOrder = item.DispatchOrder is null ? null : new ExceptionOrderOption(item.DispatchOrder.Id, item.DispatchOrder.Code),
        CustomerId = item.CustomerId,
        Customer = item.Customer is null ? null : new CustomerOption(
            item.Customer.Id, item.Customer.Code, item.Customer.Name, item.Customer.Address,
            item.Customer.Phone, item.Customer.TaxCode, item.Customer.IsWalkIn),
        Amount = item.Amount,
        AmountInWords = item.AmountInWords,
        PayerName = item.PayerName,
        Address = item.Address,
        Reason = item.Reason,
        EmployeeId = item.EmployeeId,
    };

    public SaveCashReceiptCommand ToCommand() => new(
        Id, Code, DocumentDate, Kind, DispatchOrderId, CustomerId, Amount, AmountInWords,
        PayerName, Address, Reason, EmployeeId);

    public CashReceiptDetails ToDetails() => new(
        Id, Code, DocumentDate, Kind, DispatchOrderId, DispatchOrder, CustomerId, Customer,
        Amount, AmountInWords, PayerName, Address, Reason, EmployeeId);

    public CashReceiptModel Copy() => new()
    {
        Id = Id,
        Code = Code,
        DocumentDate = DocumentDate,
        Kind = Kind,
        DispatchOrderId = DispatchOrderId,
        DispatchOrder = DispatchOrder,
        CustomerId = CustomerId,
        Customer = Customer,
        Amount = Amount,
        AmountInWords = AmountInWords,
        PayerName = PayerName,
        Address = Address,
        Reason = Reason,
        EmployeeId = EmployeeId,
    };
}
