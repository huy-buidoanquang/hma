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
        CityWorkspaceViewModel cities,
        PriceListWorkspaceViewModel prices,
        DispatchWorkspaceViewModel dispatch,
        ReconcileWorkspaceViewModel reconcile,
        StatementWorkspaceViewModel statements,
        LookupWorkspaceViewModel lookup,
        DashboardWorkspaceViewModel dashboard,
        ReportWorkspaceViewModel reports,
        SettingsWorkspaceViewModel settings,
        UserWorkspaceViewModel users)
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
                _ = dispatch.OpenByIdAsync(id);
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
            [ScreenKeys.Cities] = cities,
            [ScreenKeys.PriceLists] = prices,
            [ScreenKeys.DispatchOrders] = dispatch,
            [ScreenKeys.Reconcile] = reconcile,
            [ScreenKeys.Statements] = statements,
            [ScreenKeys.Lookup] = lookup,
            [ScreenKeys.Reports] = reports,
            [ScreenKeys.Settings] = settings,
            [ScreenKeys.Users] = users
        };

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
                     (ScreenKeys.Cities, "Thành phố"),
                     (ScreenKeys.PriceLists, "Bảng giá"),
                     (ScreenKeys.DispatchOrders, "Lệnh điều xe"),
                     (ScreenKeys.Lookup, "Tra cứu"),
                     (ScreenKeys.Reconcile, "Đối soát"),
                     (ScreenKeys.Statements, "Bảng kê tháng"),
                     (ScreenKeys.Reports, "Báo cáo"),
                     (ScreenKeys.Settings, "Tham số"),
                     (ScreenKeys.Users, "Người dùng")
                 })
        {
            if (key == ScreenKeys.Users && currentUser.User?.IsManager != true) continue;
            if (key == ScreenKeys.Settings && currentUser.User?.IsManager != true
                && !currentUser.Can(key, PermissionAction.View) && !currentUser.Can(key, PermissionAction.Update))
                continue;
            if (currentUser.User?.IsManager == true || currentUser.Can(key, PermissionAction.View) || currentUser.Can(key, PermissionAction.Create))
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
        var message = Current is WorkspaceBase { IsEditing: true }
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
            _ = loadable.LoadAsync();
    }
}
