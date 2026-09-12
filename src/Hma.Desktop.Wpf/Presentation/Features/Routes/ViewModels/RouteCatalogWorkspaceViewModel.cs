using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.ViewModels;

public partial class RouteCatalogWorkspaceViewModel(
    LocationWorkspaceViewModel locations,
    RouteWorkspaceViewModel routes,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private SettingsSection? selectedSection;
    private bool _suppressSection;

    public LocationWorkspaceViewModel Locations { get; } = locations;
    public RouteWorkspaceViewModel Routes { get; } = routes;
    public IReadOnlyList<SettingsSection> Sections { get; } = BuildSections(user);

    public bool IsLocationsSelected => SelectedSection?.Key == RouteCatalogSectionKeys.Locations;
    public bool IsRoutesSelected => SelectedSection?.Key == RouteCatalogSectionKeys.Routes;

    public override bool HasUnsavedChanges => Locations.HasUnsavedChanges || Routes.HasUnsavedChanges;

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Locations.DiscardUnsavedEditor();
        Routes.DiscardUnsavedEditor();
        if (SelectedSection is null && Sections.Count > 0)
            SelectedSection = Sections[0];
        if (Sections.Any(s => s.Key == RouteCatalogSectionKeys.Locations))
            await Locations.LoadAsync();
        if (Sections.Any(s => s.Key == RouteCatalogSectionKeys.Routes))
            await Routes.LoadAsync();
    }

    partial void OnSelectedSectionChanged(SettingsSection? oldValue, SettingsSection? newValue)
    {
        OnPropertyChanged(nameof(IsLocationsSelected));
        OnPropertyChanged(nameof(IsRoutesSelected));
        if (_suppressSection || oldValue is null || newValue is null || ReferenceEquals(oldValue, newValue))
            return;
        if (!HasUnsavedChanges)
            return;
        if (prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
        {
            _ = LoadAsync();
            return;
        }

        _suppressSection = true;
        SelectedSection = oldValue;
        _suppressSection = false;
    }

    private static IReadOnlyList<SettingsSection> BuildSections(ICurrentUser current)
    {
        var manager = current.IsManager;
        var list = new List<SettingsSection>();
        if (manager || current.Can(ScreenKeys.Locations, PermissionAction.View)
                    || current.Can(ScreenKeys.Locations, PermissionAction.Create))
            list.Add(new() { Key = RouteCatalogSectionKeys.Locations, Title = "Điểm" });
        if (manager || current.Can(ScreenKeys.Routes, PermissionAction.View)
                    || current.Can(ScreenKeys.Routes, PermissionAction.Create))
            list.Add(new() { Key = RouteCatalogSectionKeys.Routes, Title = "Tuyến" });
        return list;
    }
}
