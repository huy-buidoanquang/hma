using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Routes;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

public sealed partial class LocationEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? description;
    [ObservableProperty] private int? cityId;

    public static LocationEditorModel From(LocationSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        CityId = item.CityId,
    };

    public SaveLocationCommand ToCommand() => new(Id, Code, Name, Description, CityId);
}
