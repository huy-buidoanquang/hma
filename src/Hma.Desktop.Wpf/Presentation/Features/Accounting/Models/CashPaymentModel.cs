using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Accounting;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;

public sealed partial class CashPaymentModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private DateTime documentDate = DateTime.Today;
    [ObservableProperty] private CashPaymentKind kind = CashPaymentKind.Customer;
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private CustomerOption? customer;
    [ObservableProperty] private int? driverEmployeeId;
    [ObservableProperty] private EmployeeOption? driverEmployee;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string? amountInWords;
    [ObservableProperty] private string? payeeName;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? reason;
    [ObservableProperty] private int? employeeId;

    public static CashPaymentModel From(CashPaymentDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        DocumentDate = item.DocumentDate,
        Kind = item.Kind,
        CustomerId = item.CustomerId,
        Customer = item.Customer is null ? null : new CustomerOption(
            item.Customer.Id, item.Customer.Code, item.Customer.Name, item.Customer.Address,
            item.Customer.Phone, item.Customer.TaxCode, item.Customer.IsWalkIn),
        DriverEmployeeId = item.DriverEmployeeId,
        DriverEmployee = item.DriverEmployee is null ? null : new EmployeeOption(
            item.DriverEmployee.Id, item.DriverEmployee.Code, item.DriverEmployee.Name,
            item.DriverEmployee.DepartmentId, null, item.DriverEmployee.JobTitleId, null),
        Amount = item.Amount,
        AmountInWords = item.AmountInWords,
        PayeeName = item.PayeeName,
        Address = item.Address,
        Reason = item.Reason,
        EmployeeId = item.EmployeeId,
    };

    public SaveCashPaymentCommand ToCommand() => new(
        Id, Code, DocumentDate, Kind, CustomerId, DriverEmployeeId, Amount, AmountInWords,
        PayeeName, Address, Reason, EmployeeId);

    public CashPaymentDetails ToDetails() => new(
        Id, Code, DocumentDate, Kind, CustomerId, Customer, DriverEmployeeId, DriverEmployee,
        Amount, AmountInWords, PayeeName, Address, Reason, EmployeeId);

    public CashPaymentModel Copy() => new()
    {
        Id = Id,
        Code = Code,
        DocumentDate = DocumentDate,
        Kind = Kind,
        CustomerId = CustomerId,
        Customer = Customer,
        DriverEmployeeId = DriverEmployeeId,
        DriverEmployee = DriverEmployee,
        Amount = Amount,
        AmountInWords = AmountInWords,
        PayeeName = PayeeName,
        Address = Address,
        Reason = Reason,
        EmployeeId = EmployeeId,
    };
}
