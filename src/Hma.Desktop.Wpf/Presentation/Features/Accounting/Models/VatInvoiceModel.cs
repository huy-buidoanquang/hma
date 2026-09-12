using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Accounting;
using System.Collections.ObjectModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;

public sealed partial class VatInvoiceModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private DateTime invoiceDate = DateTime.Today;
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private CustomerOption? customer;
    [ObservableProperty] private int? employeeId;
    [ObservableProperty] private bool isByCustomer;
    [ObservableProperty] private string? goodsName;
    [ObservableProperty] private string? paymentMethodText;
    [ObservableProperty] private string? unit;
    [ObservableProperty] private decimal? quantity;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private decimal vatRate = 10;
    [ObservableProperty] private decimal vatAmount;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private string? amountInWords;

    public ObservableCollection<InvoiceOrderModel> Lines { get; } = [];

    public static VatInvoiceModel From(VatInvoiceDetails item)
    {
        var model = new VatInvoiceModel
        {
            Id = item.Id,
            Code = item.Code,
            InvoiceDate = item.InvoiceDate,
            CustomerId = item.CustomerId,
            Customer = item.Customer is null ? null : new CustomerOption(
                item.Customer.Id, item.Customer.Code, item.Customer.Name, item.Customer.Address,
                item.Customer.Phone, item.Customer.TaxCode, item.Customer.IsWalkIn),
            EmployeeId = item.EmployeeId,
            IsByCustomer = item.IsByCustomer,
            GoodsName = item.GoodsName,
            PaymentMethodText = item.PaymentMethodText,
            Unit = item.Unit,
            Quantity = item.Quantity,
            Amount = item.Amount,
            VatRate = item.VatRate,
            VatAmount = item.VatAmount,
            TotalAmount = item.TotalAmount,
            AmountInWords = item.AmountInWords,
        };
        foreach (var line in item.Lines)
            model.Lines.Add(InvoiceOrderModel.From(line));
        return model;
    }

    public void RecalculateFromTotal()
    {
        var amounts = VatInvoiceCalculation.FromTotal(TotalAmount, VatRate, Amount, VatAmount);
        Amount = amounts.Amount;
        VatAmount = amounts.VatAmount;
    }

    public SaveVatInvoiceCommand ToCommand() => new(
        Id, Code, InvoiceDate, CustomerId, EmployeeId, IsByCustomer, GoodsName,
        PaymentMethodText, Unit, Quantity, Amount, VatRate, VatAmount, TotalAmount,
        AmountInWords, Lines.Select(x => x.Id).ToList());

    public VatInvoiceDetails ToDetails() => new(
        Id, Code, InvoiceDate, CustomerId, Customer, EmployeeId, IsByCustomer, GoodsName,
        PaymentMethodText, Unit, Quantity, Amount, VatRate, VatAmount, TotalAmount, AmountInWords,
        Lines.Select(x => new InvoiceOrderOption(x.Id, x.Code, x.TotalAmount, x.SenderCustomer)).ToList());
}
