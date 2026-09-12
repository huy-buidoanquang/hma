using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Routes;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

public sealed partial class RouteAliasEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string alias = "";
    [ObservableProperty] private int routeId;

    public static RouteAliasEditorModel From(RouteAliasSummary item) => new()
    {
        Id = item.Id,
        Alias = item.Alias,
        RouteId = item.RouteId,
    };

    public SaveRouteAliasCommand ToCommand() => new(Id, Alias, RouteId);
}
