using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

public sealed partial class DriverEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? phone;
    [ObservableProperty] private DateTime? birthDate;
    [ObservableProperty] private string? identityNumber;
    [ObservableProperty] private int partnerId;

    public static DriverEditorModel From(DriverSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Phone = item.Phone,
        BirthDate = item.BirthDate,
        IdentityNumber = item.IdentityNumber,
        PartnerId = item.PartnerId,
    };

    public SaveDriverCommand ToCommand() => new(Id, Code, Name, Phone, BirthDate, IdentityNumber, PartnerId);
}
