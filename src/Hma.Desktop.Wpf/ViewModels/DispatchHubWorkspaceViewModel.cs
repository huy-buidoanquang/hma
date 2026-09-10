using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Abstractions;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DispatchHubWorkspaceViewModel(
    DispatchWorkspaceViewModel orders,
    DispatchGridEditWorkspaceViewModel gridEdit,
    DispatchImportWorkspaceViewModel import,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private SettingsSection? selectedSection;
    private bool _suppressSection;

    public DispatchWorkspaceViewModel Orders { get; } = orders;
    public DispatchGridEditWorkspaceViewModel GridEdit { get; } = gridEdit;
    public DispatchImportWorkspaceViewModel Import { get; } = import;
    public IReadOnlyList<SettingsSection> Sections { get; } = BuildSections(user);

    public bool IsOrdersSelected => SelectedSection?.Key == DispatchHubSectionKeys.Orders;
    public bool IsGridEditSelected => SelectedSection?.Key == DispatchHubSectionKeys.GridEdit;
    public bool IsImportSelected => SelectedSection?.Key == DispatchHubSectionKeys.Import;

    public override bool HasUnsavedChanges =>
        Orders.HasUnsavedChanges || GridEdit.HasUnsavedChanges || Import.HasUnsavedChanges;

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        if (SelectedSection is null && Sections.Count > 0)
            SelectedSection = Sections[0];
        await LoadSelectedAsync();
    }

    public async Task OpenOrderAsync(int id)
    {
        if (!IsOrdersSelected)
        {
            if (HasUnsavedChanges && !prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
                return;
            DiscardSection(SelectedSection);
            _suppressSection = true;
            SelectedSection = Sections.FirstOrDefault(s => s.Key == DispatchHubSectionKeys.Orders);
            _suppressSection = false;
            OnPropertyChanged(nameof(IsOrdersSelected));
            OnPropertyChanged(nameof(IsGridEditSelected));
            OnPropertyChanged(nameof(IsImportSelected));
        }

        await Orders.LoadAsync();
        await Orders.OpenByIdAsync(id);
    }

    partial void OnSelectedSectionChanged(SettingsSection? oldValue, SettingsSection? newValue)
    {
        OnPropertyChanged(nameof(IsOrdersSelected));
        OnPropertyChanged(nameof(IsGridEditSelected));
        OnPropertyChanged(nameof(IsImportSelected));
        if (_suppressSection || oldValue is null || newValue is null || ReferenceEquals(oldValue, newValue))
            return;
        if (HasUnsavedChanges)
        {
            if (!prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
            {
                _suppressSection = true;
                SelectedSection = oldValue;
                _suppressSection = false;
                return;
            }

            DiscardSection(oldValue);
        }

        _ = LoadSelectedAsync();
    }

    private async Task LoadSelectedAsync()
    {
        if (IsOrdersSelected)
            await Orders.LoadAsync();
        else if (IsGridEditSelected)
            await GridEdit.LoadAsync();
        else if (IsImportSelected)
            await Import.LoadAsync();
    }

    private void DiscardSection(SettingsSection? section)
    {
        if (section?.Key == DispatchHubSectionKeys.Orders)
            Orders.DiscardUnsavedEditor();
        if (section?.Key == DispatchHubSectionKeys.GridEdit)
            GridEdit.DiscardPendingEdits();
        if (section?.Key == DispatchHubSectionKeys.Import)
            Import.ClearDrafts();
    }

    private static IReadOnlyList<SettingsSection> BuildSections(ICurrentUser current)
    {
        var manager = current.User?.IsManager == true;
        var list = new List<SettingsSection>
        {
            new() { Key = DispatchHubSectionKeys.Orders, Title = "Lệnh điều xe" }
        };
        if (manager
            || current.Can(ScreenKeys.DispatchGridEdit, PermissionAction.View)
            || current.Can(ScreenKeys.DispatchGridEdit, PermissionAction.Update)
            || current.Can(ScreenKeys.DispatchOrders, PermissionAction.Update))
            list.Add(new() { Key = DispatchHubSectionKeys.GridEdit, Title = "Sửa lệnh theo khách" });
        if (manager
            || current.Can(ScreenKeys.DispatchOrders, PermissionAction.Create))
            list.Add(new() { Key = DispatchHubSectionKeys.Import, Title = "Nhập Excel" });
        return list;
    }
}
