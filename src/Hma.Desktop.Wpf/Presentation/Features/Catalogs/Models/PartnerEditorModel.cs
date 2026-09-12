using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

public sealed partial class PartnerEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? taxCode;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? contactName;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? email;
    [ObservableProperty] private decimal operatingFeePercent;

    public static PartnerEditorModel From(PartnerSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        TaxCode = item.TaxCode,
        Address = item.Address,
        ContactName = item.ContactName,
        Phone = item.Phone,
        Email = item.Email,
        OperatingFeePercent = item.OperatingFeePercent,
    };

    public SavePartnerCommand ToCommand() => new(
        Id, Code, Name, TaxCode, Address, ContactName, Phone, Email, OperatingFeePercent);
}
