using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Presentation.Common.Formatting;

public static class LocationAliasKindLabels
{
    public static string For(LocationAliasKind kind) => kind switch
    {
        LocationAliasKind.Both => "Cả hai",
        LocationAliasKind.Pickup => "Chỉ điểm đi",
        LocationAliasKind.Delivery => "Chỉ điểm đến",
        _ => kind.ToString()
    };
}
