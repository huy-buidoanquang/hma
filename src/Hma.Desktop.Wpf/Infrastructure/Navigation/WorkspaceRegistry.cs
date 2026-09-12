using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Navigation;

public sealed class WorkspaceRegistry : IWorkspaceRegistry
{
    public WorkspaceRegistry(
        ICurrentUser currentUser,
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
        var definitions = new[]
        {
            new WorkspaceEntry(ScreenKeys.Dashboard, "Dashboard", dashboard),
            new WorkspaceEntry(ScreenKeys.Customers, "Khách hàng", customers),
            new WorkspaceEntry(ScreenKeys.Partners, "Đối tác", partners),
            new WorkspaceEntry(ScreenKeys.Drivers, "Tài xế", drivers),
            new WorkspaceEntry(ScreenKeys.Vehicles, "Xe", vehicles),
            new WorkspaceEntry(ScreenKeys.Employees, "Nhân viên", employees),
            new WorkspaceEntry(ScreenKeys.Departments, "Phòng ban", departments),
            new WorkspaceEntry(ScreenKeys.JobTitles, "Chức vụ", jobTitles),
            new WorkspaceEntry(WorkspaceKeys.RouteCatalog, "Tuyến đường", routeCatalog),
            new WorkspaceEntry(ScreenKeys.PriceLists, "Bảng giá", prices),
            new WorkspaceEntry(ScreenKeys.PartnerRates, "Giá mua đối tác", partnerRates),
            new WorkspaceEntry(ScreenKeys.PartnerSettlements, "Quyết toán đối tác", partnerSettlements),
            new WorkspaceEntry(ScreenKeys.TransportExceptions, "Sự cố vận tải", transportExceptions),
            new WorkspaceEntry(ScreenKeys.DispatchOrders, "Lệnh điều xe", dispatchHub),
            new WorkspaceEntry(ScreenKeys.Lookup, "Tra cứu", lookup),
            new WorkspaceEntry(ScreenKeys.Reconcile, "Đối soát", reconcile),
            new WorkspaceEntry(ScreenKeys.Statements, "Bảng kê tháng", statements),
            new WorkspaceEntry(ScreenKeys.Reports, "Báo cáo", reports),
            new WorkspaceEntry(ScreenKeys.Settings, "Cấu hình", settings)
        };

        Entries = definitions.Where(entry => IsVisible(currentUser, entry.Key)).ToArray();
    }

    public IReadOnlyList<WorkspaceEntry> Entries { get; }

    private static bool IsVisible(ICurrentUser user, string key)
    {
        if (user.IsManager)
            return true;

        if (key == ScreenKeys.Settings)
            return user.Can(ScreenKeys.Settings, PermissionAction.View)
                   || user.Can(ScreenKeys.Settings, PermissionAction.Update)
                   || user.Can(ScreenKeys.Cities, PermissionAction.View)
                   || user.Can(ScreenKeys.Cities, PermissionAction.Create);

        if (key == WorkspaceKeys.RouteCatalog)
            return user.Can(ScreenKeys.Locations, PermissionAction.View)
                   || user.Can(ScreenKeys.Locations, PermissionAction.Create)
                   || user.Can(ScreenKeys.Routes, PermissionAction.View)
                   || user.Can(ScreenKeys.Routes, PermissionAction.Create);

        return user.Can(key, PermissionAction.View)
               || user.Can(key, PermissionAction.Create);
    }
}
