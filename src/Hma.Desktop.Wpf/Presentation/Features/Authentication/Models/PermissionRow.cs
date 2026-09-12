using CommunityToolkit.Mvvm.ComponentModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Authentication.Models;

public partial class PermissionRow : ObservableObject
{
    public int ScreenId { get; set; }
    public string ScreenKey { get; set; } = "";
    public string ScreenName { get; set; } = "";
    [ObservableProperty] private bool canView;
    [ObservableProperty] private bool canCreate;
    [ObservableProperty] private bool canUpdate;
    [ObservableProperty] private bool canDelete;
    [ObservableProperty] private bool canPrint;
}
