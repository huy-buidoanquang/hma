using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Desktop.Wpf.Behaviors;
using Hma.Desktop.Wpf.Converters;
using Hma.Desktop.Wpf.Theming;
using Hma.Desktop.Wpf.ViewModels;
using Hma.Desktop.Wpf.Views;
using Hma.Domain.Entities;
using Hma.Reporting;
using NSubstitute;

namespace Hma.Desktop.Wpf.Tests;

public class WpfBindingSmokeTests
{
    [Theory]
    [InlineData(FinancialDocumentStatus.Draft, "Nháp")]
    [InlineData(FinancialDocumentStatus.Submitted, "Chờ duyệt")]
    [InlineData(FinancialDocumentStatus.Finalized, "Đã chốt")]
    [InlineData(FinancialDocumentStatus.Voided, "Đã hủy")]
    [InlineData(DispatchDocumentKind.DispatchOrder, "Lệnh điều xe")]
    [InlineData(DispatchDocumentKind.DeliveryNote, "Biên bản giao hàng")]
    [InlineData(DispatchDocumentKind.Invoice, "Hóa đơn / chứng từ")]
    [InlineData(DispatchDocumentKind.Other, "Khác")]
    public void Business_enums_use_vietnamese_labels(object value, string expected)
    {
        var converter = new BusinessEnumLabelConverter();

        var actual = converter.Convert(value, typeof(string), null, System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Application_shell_and_all_workspaces_render_without_binding_errors()
    {
        Exception? failure = null;
        IReadOnlyList<string> bindingErrors = [];
        var thread = new Thread(() =>
        {
            var listener = new BindingErrorListener();
            try
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);

                var app = new App();
                app.InitializeComponent();
                var mainWindow = new MainWindow(null!);
                Assert.True(mainWindow.MinWidth <= 1180);
                Assert.True(mainWindow.MinHeight <= 700);
                Render(mainWindow, 1180, 700);

                var loginWindow = new LoginWindow(new LoginViewModel(Substitute.For<IAuthService>()));
                Render(loginWindow, 520, 420);

                var confirmDialog = new ConfirmDialog
                {
                    DataContext = new ConfirmDialogModel(
                        "Xác nhận",
                        "Nội dung kiểm thử",
                        "Đồng ý",
                        "Hủy",
                        false)
                };
                Render(confirmDialog, 520, 240);

                var views = CreateWorkspaceViews();
                foreach (var item in views)
                {
                    item.View.DataContext = item.ViewModel;
                    Render(item.View, 980, 660);
                }

                var theme = new ThemeService();
                theme.Apply(ThemeService.Dark, persist: false);
                var darkSecondaryText = Assert.IsType<SolidColorBrush>(
                    System.Windows.Application.Current.Resources["InkSecondaryBrush"]);
                Assert.Equal(Color.FromRgb(0x9C, 0xA3, 0xAF), darkSecondaryText.Color);
                foreach (var item in views)
                    Render(item.View, 980, 660);
                theme.Apply(ThemeService.Light, persist: false);

                var exceptionView = views.Single(x => x.View is TransportExceptionView).View;
                var statusField = FindTextBox(exceptionView, "Editor.StatusLabel");
                var statusBinding = Assert.IsType<Binding>(
                    BindingOperations.GetBindingBase(statusField, TextBox.TextProperty));
                Assert.Equal(BindingMode.OneWay, statusBinding.Mode);
                var exceptionOrderField = FindTextBox(exceptionView, "Editor.DispatchOrder.Code");
                Assert.True(exceptionOrderField.IsReadOnly);

                var dispatchView = views.Single(x => x.View is DispatchView).View;
                var vehicleSearch = FindTextBox(dispatchView, "VehicleLookupText");
                var driverSearch = FindTextBox(dispatchView, "DriverLookupText");
                Assert.Equal(UpdateSourceTrigger.PropertyChanged,
                    Assert.IsType<Binding>(BindingOperations.GetBindingBase(
                        vehicleSearch, TextBox.TextProperty)).UpdateSourceTrigger);
                Assert.Equal(UpdateSourceTrigger.PropertyChanged,
                    Assert.IsType<Binding>(BindingOperations.GetBindingBase(
                        driverSearch, TextBox.TextProperty)).UpdateSourceTrigger);
                Assert.False(FindComboBox(dispatchView, "Vehicles").IsEditable);
                Assert.False(FindComboBox(dispatchView, "Drivers").IsEditable);

                var statementView = views.Single(x => x.View is StatementView).View;
                var voidReasonField = FindTextBox(statementView, "VoidReason");
                Assert.False(BindingOperations.IsDataBound(
                    voidReasonField,
                    CommandBehaviors.EnterCommandProperty));

                var reconcileView = views.Single(x => x.View is ReconcileView).View;
                var reconcileGrid = FindDataGrid(reconcileView, "Khách");
                var customerColumn = Assert.Single(
                    reconcileGrid.Columns,
                    column => Equals(column.Header, "Khách"));
                Assert.True(customerColumn.Width.IsAbsolute);
                Assert.True(customerColumn.Width.Value >= 160);

                var settlementView = views.Single(x => x.View is PartnerSettlementView).View;
                var settlementGrid = FindDataGrid(settlementView, "Đối tác");
                Assert.False(BindingOperations.IsDataBound(
                    settlementGrid,
                    CommandBehaviors.DoubleClickCommandProperty));
                var settlementViewModel = Assert.IsType<PartnerSettlementWorkspaceViewModel>(
                    settlementView.DataContext);
                var selectedSettlement = new PartnerSettlement
                {
                    Id = 42,
                    PartnerId = 7,
                    Year = 2026,
                    Month = 9,
                    GrossAmount = 730_000m
                };
                settlementViewModel.Selected = selectedSettlement;
                Assert.Same(selectedSettlement, settlementViewModel.CurrentSettlement);

                var userView = views.Single(x => x.View is UserView).View;
                var userViewModel = Assert.IsType<UserWorkspaceViewModel>(userView.DataContext);
                var passwordBox = FindPasswordBox(userView, "NewPassword");
                Assert.True(PasswordBoxBehaviors.GetBindPassword(passwordBox));
                Assert.Null(TryFindTextBox(userView, "NewPassword"));

                passwordBox.Password = "masked-test-password";
                Assert.Equal("masked-test-password", userViewModel.NewPassword);

                userViewModel.NewPassword = "updated-from-view-model";
                Assert.Equal("updated-from-view-model", passwordBox.Password);

                bindingErrors = listener.Messages.ToList();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "WPF smoke test did not finish within 20 seconds.");
        Assert.Null(failure);
        Assert.Empty(bindingErrors);
    }

    private static IReadOnlyList<ViewCase> CreateWorkspaceViews()
    {
        var db = Substitute.For<IHmaDbContext>();
        var current = new CurrentUser
        {
            User = new AppUser { Id = 1, UserName = "ui-test", DisplayName = "UI test", IsManager = true }
        };
        var prompt = Substitute.For<IUserPrompt>();
        var printer = Substitute.For<IDocumentPrinter>();
        var numbers = Substitute.For<IDocumentNumberService>();
        var audit = Substitute.For<IChangeLogService>();
        var storage = Substitute.For<IFileStorage>();
        var navigator = new WorkspaceNavigator();

        var catalog = new CatalogService(db, current);
        var customers = new CustomerService(db, current);
        var company = new CompanyService(db, current);
        var prices = new PriceListService(db, current);
        var partnerRates = new PartnerRateService(db, current);
        var orders = new DispatchOrderService(db, numbers, prices, partnerRates, current, audit);
        var documents = new DispatchDocumentService(db, current, storage);
        var importer = new DispatchImportService(db, orders, prices, current);
        var cash = new CashDocumentService(db, numbers);
        var invoices = new VatInvoiceService(db, numbers);
        var dashboard = new DashboardQueryService(db);
        var reports = new ReportQueryService(db);
        var settings = new SettingsService(db, current);
        var health = new SystemHealthService(db, storage);
        var users = new UserAdminService(db, Substitute.For<IPasswordHasher>(), current);
        var locationService = new LocationService(db, current);
        var routeService = new RouteService(db, current);
        var locationAliases = new LocationAliasService(db, current);
        var routeAliases = new RouteAliasService(db, current);
        var customerAliases = new CustomerAliasService(db, current);
        var statements = new FreightStatementService(db, numbers, current, audit);
        var settlements = new PartnerSettlementService(db, numbers, current, audit);
        var exceptions = new TransportExceptionService(db, current, audit);
        var changeLogs = new ChangeLogService(db, current);

        var cityVm = new CityWorkspaceViewModel(catalog, current, prompt);
        var customerVm = new CustomerWorkspaceViewModel(customers, catalog, current, prompt, printer);
        var customerAliasVm = new CustomerAliasWorkspaceViewModel(customerAliases, customers, current, prompt);
        var departmentVm = new DepartmentWorkspaceViewModel(catalog, current, prompt);
        var dispatchVm = new DispatchWorkspaceViewModel(
            orders, documents, catalog, customers, company, printer, current, prompt);
        var dispatchGridVm = new DispatchGridEditWorkspaceViewModel(orders, catalog, customers, current, prompt);
        var dispatchImportVm = new DispatchImportWorkspaceViewModel(
            importer, Substitute.For<IDispatchImportParser>(), current, prompt);
        var dispatchHubVm = new DispatchHubWorkspaceViewModel(
            dispatchVm, dispatchGridVm, dispatchImportVm, current, prompt);
        var driverVm = new DriverWorkspaceViewModel(catalog, current, prompt, printer);
        var employeeVm = new EmployeeWorkspaceViewModel(catalog, current, prompt);
        var jobTitleVm = new JobTitleWorkspaceViewModel(catalog, current, prompt);
        var locationVm = new LocationWorkspaceViewModel(locationService, catalog, current, prompt);
        var routeVm = new RouteWorkspaceViewModel(routeService, catalog, current, prompt);
        var routeCatalogVm = new RouteCatalogWorkspaceViewModel(locationVm, routeVm, current, prompt);
        var locationAliasVm = new LocationAliasWorkspaceViewModel(locationAliases, catalog, current, prompt);
        var routeAliasVm = new RouteAliasWorkspaceViewModel(routeAliases, catalog, current, prompt);
        var partnerVm = new PartnerWorkspaceViewModel(catalog, current, prompt, printer);
        var priceVm = new PriceListWorkspaceViewModel(prices, catalog, customers, current, prompt);
        var userVm = new UserWorkspaceViewModel(users, catalog, current, prompt);
        var settingsVm = new SettingsWorkspaceViewModel(
            settings,
            health,
            company,
            cityVm,
            userVm,
            locationAliasVm,
            routeAliasVm,
            customerAliasVm,
            current,
            prompt,
            new ThemeService());

        return
        [
            new(new CashPaymentView(), new CashPaymentWorkspaceViewModel(cash, customers, catalog, company, printer, prompt)),
            new(new CashReceiptView(), new CashReceiptWorkspaceViewModel(cash, customers, orders, company, printer, prompt)),
            new(new CityView(), cityVm),
            new(new CustomerAliasView(), customerAliasVm),
            new(new CustomerView(), customerVm),
            new(new DashboardView(), new DashboardWorkspaceViewModel(dashboard, current)),
            new(new DepartmentView(), departmentVm),
            new(new DispatchGridEditView(), dispatchGridVm),
            new(new DispatchHubView(), dispatchHubVm),
            new(new DispatchImportView(), dispatchImportVm),
            new(new DispatchView(), dispatchVm),
            new(new DriverView(), driverVm),
            new(new EmployeeView(), employeeVm),
            new(new InvoiceView(), new InvoiceWorkspaceViewModel(invoices, customers, company, printer, prompt)),
            new(new JobTitleView(), jobTitleVm),
            new(new LocationAliasView(), locationAliasVm),
            new(new LocationView(), locationVm),
            new(new LookupView(), new LookupWorkspaceViewModel(orders, current, navigator)),
            new(new PartnerRateView(), new PartnerRateWorkspaceViewModel(partnerRates, catalog, current, prompt)),
            new(new PartnerSettlementView(), new PartnerSettlementWorkspaceViewModel(settlements, catalog, current)),
            new(new PartnerView(), partnerVm),
            new(new PriceListView(), priceVm),
            new(new ReconcileView(), new ReconcileWorkspaceViewModel(orders, changeLogs, current, navigator)),
            new(new ReportView(), new ReportWorkspaceViewModel(reports, dashboard, company, printer, current)),
            new(new RouteAliasView(), routeAliasVm),
            new(new RouteCatalogView(), routeCatalogVm),
            new(new RouteView(), routeVm),
            new(new SettingsView(), settingsVm),
            new(new StatementView(), new StatementWorkspaceViewModel(statements, customers, company, printer, current)),
            new(new TransportExceptionView(), new TransportExceptionWorkspaceViewModel(exceptions, current, prompt)),
            new(new UserView(), userVm),
            new(new VehicleView(), new VehicleWorkspaceViewModel(catalog, current, prompt, printer))
        ];
    }

    private static void Render(FrameworkElement view, double width, double height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
    }

    private static TextBox FindTextBox(DependencyObject parent, string bindingPath) =>
        TryFindTextBox(parent, bindingPath) ??
        throw new InvalidOperationException($"TextBox binding '{bindingPath}' was not found.");

    private static DataGrid FindDataGrid(DependencyObject parent, string columnHeader) =>
        TryFindDataGrid(parent, columnHeader) ??
        throw new InvalidOperationException($"DataGrid column '{columnHeader}' was not found.");

    private static ComboBox FindComboBox(DependencyObject parent, string itemsSourcePath) =>
        TryFindComboBox(parent, itemsSourcePath) ??
        throw new InvalidOperationException($"ComboBox ItemsSource binding '{itemsSourcePath}' was not found.");

    private static ComboBox? TryFindComboBox(DependencyObject parent, string itemsSourcePath)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ComboBox comboBox
                && BindingOperations.GetBindingBase(comboBox, ItemsControl.ItemsSourceProperty) is Binding binding
                && binding.Path.Path == itemsSourcePath)
            {
                return comboBox;
            }

            var descendant = TryFindComboBox(child, itemsSourcePath);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    private static DataGrid? TryFindDataGrid(DependencyObject parent, string columnHeader)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is DataGrid grid && grid.Columns.Any(column => Equals(column.Header, columnHeader)))
                return grid;

            var descendant = TryFindDataGrid(child, columnHeader);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    private static TextBox? TryFindTextBox(DependencyObject parent, string bindingPath)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is TextBox textBox &&
                BindingOperations.GetBindingBase(textBox, TextBox.TextProperty) is Binding binding &&
                binding.Path.Path == bindingPath)
            {
                return textBox;
            }

            var descendant = TryFindTextBox(child, bindingPath);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    private static PasswordBox FindPasswordBox(DependencyObject parent, string bindingPath) =>
        TryFindPasswordBox(parent, bindingPath) ??
        throw new InvalidOperationException($"PasswordBox binding '{bindingPath}' was not found.");

    private static PasswordBox? TryFindPasswordBox(DependencyObject parent, string bindingPath)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is PasswordBox passwordBox &&
                BindingOperations.GetBindingBase(passwordBox, PasswordBoxBehaviors.PasswordProperty) is Binding binding &&
                binding.Path.Path == bindingPath)
            {
                return passwordBox;
            }

            var descendant = TryFindPasswordBox(child, bindingPath);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    private sealed class BindingErrorListener : TraceListener
    {
        public List<string> Messages { get; } = [];

        public override void Write(string? message) => Add(message);

        public override void WriteLine(string? message) => Add(message);

        private void Add(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Messages.Add(message);
        }
    }

    private sealed record ViewCase(Control View, object ViewModel);
}
