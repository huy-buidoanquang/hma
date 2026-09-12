using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Dispatching;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public sealed partial class DispatchOrderLineModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int lineNumber;
    [ObservableProperty] private string? goodsName;
    [ObservableProperty] private int? packageCount;
    [ObservableProperty] private string? route;
    [ObservableProperty] private decimal? kilometers;
    [ObservableProperty] private string? notes;

    public static DispatchOrderLineModel From(DispatchOrderLineDetails item) => new()
    {
        Id = item.Id,
        LineNumber = item.LineNumber,
        GoodsName = item.GoodsName,
        PackageCount = item.PackageCount,
        Route = item.Route,
        Kilometers = item.Kilometers,
        Notes = item.Notes,
    };

    public DispatchOrderLineDetails ToDetails() => new(
        Id, LineNumber, GoodsName, PackageCount, Route, Kilometers, Notes);
}
