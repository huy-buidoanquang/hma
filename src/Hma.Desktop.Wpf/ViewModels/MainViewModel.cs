using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object? current;
    [ObservableProperty] private NavItem? selected;
    [ObservableProperty] private string userLabel = "";

    public System.Collections.ObjectModel.ObservableCollection<NavItem> Items { get; } = [];

    public MainViewModel(
        ICurrentUser currentUser,
        IAuthService auth,
        IUserPrompt prompt,
        ISessionHost session,
        IWorkspaceNavigator navigator,
        CustomerWorkspaceViewModel customers,
        PartnerWorkspaceViewModel partners,
        DriverWorkspaceViewModel drivers,
        VehicleWorkspaceViewModel vehicles,
        EmployeeWorkspaceViewModel employees,
        DepartmentWorkspaceViewModel departments,
        JobTitleWorkspaceViewModel jobTitles,
        RouteCatalogWorkspaceViewModel routeCatalog,
        PriceListWorkspaceViewModel prices,
        PartnerRateWorkspaceViewModel partnerRates,
        PartnerSettlementWorkspaceViewModel partnerSettlements,
        TransportExceptionWorkspaceViewModel transportExceptions,
        DispatchHubWorkspaceViewModel dispatchHub,
        ReconcileWorkspaceViewModel reconcile,
        StatementWorkspaceViewModel statements,
        LookupWorkspaceViewModel lookup,
        DashboardWorkspaceViewModel dashboard,
        ReportWorkspaceViewModel reports,
        SettingsWorkspaceViewModel settings)
    {
        _auth = auth;
        _prompt = prompt;
        _session = session;
        UserLabel = currentUser.User?.DisplayName ?? currentUser.User?.UserName ?? "";
        if (navigator is WorkspaceNavigator nav)
        {
            nav.OpenDispatchHandler = id =>
            {
                var item = Items.FirstOrDefault(i => i.Key == ScreenKeys.DispatchOrders);
                if (item is not null) Selected = item;
                _ = dispatchHub.OpenOrderAsync(id);
            };
        }

        var map = new Dictionary<string, object>
        {
            [ScreenKeys.Dashboard] = dashboard,
            [ScreenKeys.Customers] = customers,
            [ScreenKeys.Partners] = partners,
            [ScreenKeys.Drivers] = drivers,
            [ScreenKeys.Vehicles] = vehicles,
            [ScreenKeys.Employees] = employees,
            [ScreenKeys.Departments] = departments,
            [ScreenKeys.JobTitles] = jobTitles,
            [WorkspaceKeys.RouteCatalog] = routeCatalog,
            [ScreenKeys.PriceLists] = prices,
            [ScreenKeys.PartnerRates] = partnerRates,
            [ScreenKeys.PartnerSettlements] = partnerSettlements,
            [ScreenKeys.TransportExceptions] = transportExceptions,
            [ScreenKeys.DispatchOrders] = dispatchHub,
            [ScreenKeys.Reconcile] = reconcile,
            [ScreenKeys.Statements] = statements,
            [ScreenKeys.Lookup] = lookup,
            [ScreenKeys.Reports] = reports,
            [ScreenKeys.Settings] = settings
        };

        var manager = currentUser.User?.IsManager == true;
        foreach (var (key, title) in new (string Key, string Title)[]
                 {
                     (ScreenKeys.Dashboard, "Dashboard"),
                     (ScreenKeys.Customers, "Khách hàng"),
                     (ScreenKeys.Partners, "Đối tác"),
                     (ScreenKeys.Drivers, "Tài xế"),
                     (ScreenKeys.Vehicles, "Xe"),
                     (ScreenKeys.Employees, "Nhân viên"),
                     (ScreenKeys.Departments, "Phòng ban"),
                     (ScreenKeys.JobTitles, "Chức vụ"),
                     (WorkspaceKeys.RouteCatalog, "Tuyến đường"),
                     (ScreenKeys.PriceLists, "Bảng giá"),
                     (ScreenKeys.PartnerRates, "Giá mua đối tác"),
                     (ScreenKeys.PartnerSettlements, "Quyết toán đối tác"),
                     (ScreenKeys.TransportExceptions, "Sự cố vận tải"),
                     (ScreenKeys.DispatchOrders, "Lệnh điều xe"),
                     (ScreenKeys.Lookup, "Tra cứu"),
                     (ScreenKeys.Reconcile, "Đối soát"),
                     (ScreenKeys.Statements, "Bảng kê tháng"),
                     (ScreenKeys.Reports, "Báo cáo"),
                     (ScreenKeys.Settings, "Cấu hình")
                 })
        {
            if (key == ScreenKeys.Settings)
            {
                if (manager
                    || currentUser.Can(ScreenKeys.Settings, PermissionAction.View)
                    || currentUser.Can(ScreenKeys.Settings, PermissionAction.Update)
                    || currentUser.Can(ScreenKeys.Cities, PermissionAction.View)
                    || currentUser.Can(ScreenKeys.Cities, PermissionAction.Create))
                    Items.Add(new NavItem { Key = key, Title = title });
                continue;
            }

            if (key == WorkspaceKeys.RouteCatalog)
            {
                if (manager
                    || currentUser.Can(ScreenKeys.Locations, PermissionAction.View)
                    || currentUser.Can(ScreenKeys.Locations, PermissionAction.Create)
                    || currentUser.Can(ScreenKeys.Routes, PermissionAction.View)
                    || currentUser.Can(ScreenKeys.Routes, PermissionAction.Create))
                    Items.Add(new NavItem { Key = key, Title = title });
                continue;
            }

            if (manager
                || currentUser.Can(key, PermissionAction.View)
                || currentUser.Can(key, PermissionAction.Create))
                Items.Add(new NavItem { Key = key, Title = title });
        }

        _workspaces = map;
        if (Items.Count > 0) Selected = Items[0];
    }

    private readonly Dictionary<string, object> _workspaces;
    private readonly IAuthService _auth;
    private readonly IUserPrompt _prompt;
    private readonly ISessionHost _session;

    [RelayCommand]
    private void Logout()
    {
        var message = Current is WorkspaceBase { HasUnsavedChanges: true }
            ? "Bỏ thay đổi chưa lưu và đăng xuất?"
            : "Đăng xuất khỏi phiên này?";
        if (!_prompt.Confirm(message, "Đăng xuất"))
            return;
        _auth.Logout();
        _session.SignOut();
    }

    partial void OnSelectedChanged(NavItem? value)
    {
        if (value is null) return;
        Current = _workspaces[value.Key];
        if (Current is ILoadableWorkspace loadable)
            _ = LoadWorkspaceAsync(loadable);
    }

    private static async Task LoadWorkspaceAsync(ILoadableWorkspace loadable)
    {
        try
        {
            await loadable.LoadAsync();
        }
        catch (Exception)
        {
            // WorkspaceBase.RunAsync already toasts; this swallows unobserved fire-and-forget faults.
        }
    }
}
