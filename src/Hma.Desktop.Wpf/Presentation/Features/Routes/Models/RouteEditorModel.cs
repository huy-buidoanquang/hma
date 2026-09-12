using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Routes;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

public sealed partial class RouteEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string fingerprint = "";
    [ObservableProperty] private string? description;

    public static RouteEditorModel From(RouteSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Fingerprint = item.Fingerprint,
        Description = item.Description,
    };
}
