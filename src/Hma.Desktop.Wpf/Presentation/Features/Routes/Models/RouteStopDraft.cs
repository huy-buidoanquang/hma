using CommunityToolkit.Mvvm.ComponentModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

public partial class RouteStopDraft : ObservableObject
{
    [ObservableProperty] private int locationId;
}
