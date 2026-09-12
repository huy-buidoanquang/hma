using Hma.Application.Features.Dispatching;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public sealed record DispatchStopModel(int Id, int Sequence, int LocationId, string NameSnapshot)
{
    public static DispatchStopModel From(DispatchStopDetails item) =>
        new(item.Id, item.Sequence, item.LocationId, item.NameSnapshot);

    public DispatchStopDetails ToDetails() => new(Id, Sequence, LocationId, NameSnapshot);
}
