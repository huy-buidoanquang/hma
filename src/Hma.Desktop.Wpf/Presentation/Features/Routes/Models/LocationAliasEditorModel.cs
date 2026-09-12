using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Routes;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

public sealed partial class LocationAliasEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string alias = "";
    [ObservableProperty] private int locationId;
    [ObservableProperty] private LocationAliasKind kind = LocationAliasKind.Both;

    public static LocationAliasEditorModel From(LocationAliasSummary item) => new()
    {
        Id = item.Id,
        Alias = item.Alias,
        LocationId = item.LocationId,
        Kind = item.Kind,
    };

    public SaveLocationAliasCommand ToCommand() => new(Id, Alias, LocationId, Kind);
}
