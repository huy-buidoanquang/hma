using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Customers;

namespace Hma.Desktop.Wpf.Presentation.Features.Customers.Models;

public sealed partial class CustomerEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? taxCode;
    [ObservableProperty] private string? contactName;
    [ObservableProperty] private string? email;
    [ObservableProperty] private int? cityId;
    [ObservableProperty] private int? accountantEmployeeId;
    [ObservableProperty] private bool isWalkIn;

    public static CustomerEditorModel From(CustomerSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Address = item.Address,
        Phone = item.Phone,
        TaxCode = item.TaxCode,
        ContactName = item.ContactName,
        Email = item.Email,
        CityId = item.CityId,
        AccountantEmployeeId = item.AccountantEmployeeId,
        IsWalkIn = item.IsWalkIn,
    };

    public SaveCustomerCommand ToCommand() => new(
        Id, Code, Name, Address, Phone, TaxCode, ContactName, Email, CityId, AccountantEmployeeId, IsWalkIn);
}
