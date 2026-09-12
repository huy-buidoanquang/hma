using Hma.Application.Features.Reconciliation;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.PartnerSettlements;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Dispatching.Import;
using Hma.Application.Features.Accounting;
using Hma.Application.Abstractions.Import;
using Hma.Application.Features.Customers;
using Hma.Application.Features.TransportExceptions;
using Hma.Application.Features.Reporting;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Routes;
using Hma.Application.Features.Statements;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Pricing;
using Hma.Application.Features.Settings;
using Hma.Application.Abstractions.Storage;
using Hma.Application.Features.Authentication;
using Hma.Application.Abstractions.Persistence;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Hma.Desktop.Wpf.Behaviors;
using Hma.Desktop.Wpf.Controls;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Converters;
using Hma.Desktop.Wpf.Infrastructure.Session;
using Hma.Desktop.Wpf.Infrastructure.Navigation;
using Hma.Desktop.Wpf.Infrastructure.Notifications;
using Hma.Desktop.Wpf.Infrastructure.Theming;
using Hma.Desktop.Wpf.Presentation.Common.Views;
using Hma.Desktop.Wpf.Presentation.Shell.ViewModels;
using Hma.Domain.Entities;
using Hma.Domain.Enums;
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
    [InlineData(PriceFluctuationType.Percentage, "Phần trăm")]
    [InlineData(PriceFluctuationType.FixedAmount, "Số tiền cố định")]
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
                var shellRegistry = Substitute.For<IWorkspaceRegistry>();
                shellRegistry.Entries.Returns([]);
                var shellToast = new ToastService(TimeProvider.System);
                var mainWindow = new MainWindow(new MainViewModel(
                    Substitute.For<ICurrentUser>(),
                    Substitute.For<IDesktopUserSession>(),
                    Substitute.For<IUserPrompt>(),
                    Substitute.For<ISessionHost>(),
                    Substitute.For<IWorkspaceNavigator>(),
                    shellRegistry,
                    shellToast));
                mainWindow.ShowActivated = false;
                mainWindow.ShowInTaskbar = false;
                mainWindow.Opacity = 0;
                Assert.True(mainWindow.MinWidth <= 1366);
                Assert.True(mainWindow.MinHeight <= 768);
                shellToast.Show("Thông báo kiểm thử", isError: true);
                mainWindow.Show();
                Render(mainWindow, 1366, 768);
                var toastHost = Assert.IsType<Border>(mainWindow.FindName("ToastHost"));
                var toastText = Assert.IsType<TextBlock>(mainWindow.FindName("ToastText"));
                toastHost.GetBindingExpression(UIElement.VisibilityProperty)?.UpdateTarget();
                toastText.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
                Assert.Equal(Visibility.Visible, toastHost.Visibility);
                Assert.Equal("Thông báo kiểm thử", toastText.Text);
                mainWindow.Close();

                var loginWindow = new LoginWindow(new LoginViewModel(
                    Substitute.For<IAuthService>(),
                    Substitute.For<IDesktopUserSession>()));
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
                var exceptionCodeField = FindComboBox(exceptionView, "Codes");
                Assert.Equal(nameof(TransportExceptionCodeOption.Name), exceptionCodeField.DisplayMemberPath);
                var statusField = FindTextBox(exceptionView, "Editor.StatusLabel");
                var statusBinding = Assert.IsType<Binding>(
                    BindingOperations.GetBindingBase(statusField, TextBox.TextProperty));
                Assert.Equal(BindingMode.OneWay, statusBinding.Mode);
                var exceptionOrderField = FindTextBox(exceptionView, "Editor.DispatchOrder.Code");
                Assert.True(exceptionOrderField.IsReadOnly);

                var dispatchView = views.Single(x => x.View is DispatchView).View;
                var vehicleSearch = FindComboBox(dispatchView, "Vehicles");
                var driverSearch = FindComboBox(dispatchView, "Drivers");
                Assert.Equal(UpdateSourceTrigger.PropertyChanged,
                    Assert.IsType<Binding>(BindingOperations.GetBindingBase(
                        vehicleSearch, ComboBox.TextProperty)).UpdateSourceTrigger);
                Assert.Equal(UpdateSourceTrigger.PropertyChanged,
                    Assert.IsType<Binding>(BindingOperations.GetBindingBase(
                        driverSearch, ComboBox.TextProperty)).UpdateSourceTrigger);
                Assert.True(vehicleSearch.IsEditable);
                Assert.True(driverSearch.IsEditable);
                Assert.True(AutoCompleteComboBoxBehavior.GetIsEnabled(vehicleSearch));
                Assert.True(AutoCompleteComboBoxBehavior.GetIsEnabled(driverSearch));
                Assert.Null(TryFindTextBox(dispatchView, "NewPartyName"));
                Assert.Equal(nameof(CustomerSummary.CodeName),
                    FindComboBox(dispatchView, "CustomerOptions").DisplayMemberPath);
                var dispatchViewModel = Assert.IsType<DispatchWorkspaceViewModel>(dispatchView.DataContext);
                var pickupAt = new DateTime(2026, 9, 12, 13, 47, 0);
                dispatchViewModel.Editor.PickupAt = pickupAt;
                dispatchViewModel.BindEditorChrome();
                Assert.Equal(13, dispatchViewModel.PickupHour);
                Assert.Equal(47, dispatchViewModel.PickupMinute);
                Assert.Equal(pickupAt, dispatchViewModel.Editor.PickupAt);

                var priceView = views.Single(x => x.View is PriceListView).View;
                Assert.IsType<PriceListWorkspaceViewModel>(priceView.DataContext);
                Render(priceView, 980, 760);
                var adjustmentInput = new SignedAdjustmentInput
                {
                    Type = PriceFluctuationType.FixedAmount,
                };
                Render(adjustmentInput, 220, 32);
                var adjustmentText = FindDescendant<TextBox>(adjustmentInput);
                adjustmentText.Text = "-100000";
                Assert.Equal(-100_000m, adjustmentInput.Value);
                Assert.True(adjustmentInput.IsDecreaseSelected);
                var signButtons = FindDescendants<Button>(adjustmentInput)
                    .Where(x => Equals(x.Content, "+") || Equals(x.Content, "−"))
                    .ToList();
                var decreaseButton = Assert.Single(signButtons, x => Equals(x.Content, "−"));
                var increaseButton = Assert.Single(signButtons, x => Equals(x.Content, "+"));
                Assert.Same(System.Windows.Application.Current.Resources["ErrorBrush"], decreaseButton.Foreground);
                increaseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(100_000m, adjustmentInput.Value);
                Assert.False(adjustmentInput.IsDecreaseSelected);
                Assert.Same(System.Windows.Application.Current.Resources["SuccessBrush"], increaseButton.Foreground);
                adjustmentInput.Type = PriceFluctuationType.Percentage;
                adjustmentText.Text = "-10%";
                Assert.Equal(-10m, adjustmentInput.Value);
                Assert.True(adjustmentInput.IsDecreaseSelected);

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
                var selectedSettlement = new PartnerSettlementDetails(
                    42, "QT-42", 7, null, 2026, 9, FinancialDocumentStatus.Draft,
                    null, null, 0, 730_000m, 0, 730_000m, null, []);
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
        var current = new DesktopUserSession();
        current.SignIn(new AuthenticatedUser(
            1,
            null,
            "ui-test",
            "UI test",
            true,
            new Dictionary<string, PermissionGrant>()));
        var prompt = Substitute.For<IUserPrompt>();
        var printer = Substitute.For<IDocumentRenderer>();
        var documentInteraction = Substitute.For<IDocumentInteractionService>();
        var gate = new UiOperationGate();
        var toast = new ToastService(TimeProvider.System);
        var numbers = Substitute.For<IDocumentNumberService>();
        var audit = Substitute.For<IChangeLogService>();
        var storage = Substitute.For<IFileStorage>();
        var navigator = new WorkspaceNavigator();

        var catalog = new CatalogOptionQueryService(db);
        var cityService = new CityService(db, current);
        var departmentService = new DepartmentService(db, current);
        var jobTitleService = new JobTitleService(db, current);
        var employeeService = new EmployeeService(db, current, TimeProvider.System);
        var partnerService = new PartnerService(db, current);
        var driverService = new DriverService(db, current);
        var vehicleService = new VehicleService(db, current);
        var customers = new CustomerService(db, current, TimeProvider.System);
        var company = new CompanyService(db, current);
        var prices = new PriceListService(db, current, TimeProvider.System);
        var partnerRates = new PartnerRateService(db, current, TimeProvider.System);
        var dispatchQueries = new DispatchOrderQueryService(db);
        var dispatchEditor = new DispatchOrderEditorService(db, numbers, prices, partnerRates, current, audit, TimeProvider.System);
        var reconciliation = new DispatchReconciliationService(db, dispatchQueries, current, audit, TimeProvider.System);
        var grid = new DispatchGridService(db, dispatchQueries, dispatchEditor, current, audit);
        var documents = new DispatchDocumentService(db, current, storage, TimeProvider.System);
        var importer = new DispatchImportService(db, dispatchEditor, prices, current, TimeProvider.System);
        var cash = new CashDocumentService(db, numbers, TimeProvider.System);
        var invoices = new VatInvoiceService(db, numbers, TimeProvider.System);
        var dashboard = new DashboardQueryService(db);
        var reports = new ReportQueryService(db);
        var settings = new SettingsService(db, current);
        var health = new SystemHealthService(db, storage, TimeProvider.System);
        var users = new UserAdminService(db, Substitute.For<IPasswordHasher>(), current, TimeProvider.System);
        var locationService = new LocationService(db, current);
        var routeService = new RouteService(db, current);
        var locationAliases = new LocationAliasService(db, current);
        var routeAliases = new RouteAliasService(db, current);
        var customerAliases = new CustomerAliasService(db, current);
        var statements = new FreightStatementService(db, numbers, current, audit, TimeProvider.System);
        var settlements = new PartnerSettlementService(db, numbers, current, audit, TimeProvider.System);
        var exceptions = new TransportExceptionService(db, current, audit, TimeProvider.System);
        var changeLogs = new ChangeLogService(db, current, TimeProvider.System);

        var cityVm = new CityWorkspaceViewModel(cityService, current, prompt, gate, toast);
        var customerVm = new CustomerWorkspaceViewModel(customers, catalog, current, prompt, printer, documentInteraction, gate, toast);
        var customerAliasVm = new CustomerAliasWorkspaceViewModel(customerAliases, customers, current, prompt, gate, toast);
        var departmentVm = new DepartmentWorkspaceViewModel(departmentService, current, prompt, gate, toast);
        var dispatchVm = new DispatchWorkspaceViewModel(
            dispatchQueries, dispatchEditor, documents, documentInteraction, catalog, customers, company, printer, current, prompt, gate, toast);
        var dispatchGridVm = new DispatchGridEditWorkspaceViewModel(dispatchQueries, grid, catalog, customers, current, prompt, gate, toast);
        var dispatchImportVm = new DispatchImportWorkspaceViewModel(
            importer, Substitute.For<IDispatchImportParser>(), current, prompt, gate, toast);
        var dispatchHubVm = new DispatchHubWorkspaceViewModel(
            dispatchVm, dispatchGridVm, dispatchImportVm, current, prompt, gate, toast);
        var driverVm = new DriverWorkspaceViewModel(driverService, catalog, dispatchQueries, current, prompt, printer, documentInteraction, gate, toast);
        var employeeVm = new EmployeeWorkspaceViewModel(employeeService, catalog, current, prompt, gate, toast);
        var jobTitleVm = new JobTitleWorkspaceViewModel(jobTitleService, current, prompt, gate, toast);
        var locationVm = new LocationWorkspaceViewModel(locationService, catalog, current, prompt, gate, toast);
        var routeVm = new RouteWorkspaceViewModel(routeService, catalog, current, prompt, gate, toast);
        var routeCatalogVm = new RouteCatalogWorkspaceViewModel(locationVm, routeVm, current, prompt, gate, toast);
        var locationAliasVm = new LocationAliasWorkspaceViewModel(locationAliases, catalog, current, prompt, gate, toast);
        var routeAliasVm = new RouteAliasWorkspaceViewModel(routeAliases, catalog, current, prompt, gate, toast);
        var partnerVm = new PartnerWorkspaceViewModel(partnerService, current, prompt, printer, documentInteraction, gate, toast);
        var priceVm = new PriceListWorkspaceViewModel(prices, catalog, customers, current, prompt, gate, toast);
        var userVm = new UserWorkspaceViewModel(users, catalog, current, prompt, gate, toast);
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
            new ThemeService(),
            gate,
            toast);

        var exceptionViewModel = new TransportExceptionWorkspaceViewModel(exceptions, current, prompt, gate, toast);
        exceptionViewModel.Codes.Add(new TransportExceptionCodeOption(1, "WAIT", "Chờ bốc hàng"));
        var customerOption = new CustomerSummary(
            1, "KH001", "Khách kiểm thử", null, null, null, null, null,
            null, null, null, null, false, null);
        dispatchVm.CustomerOptions.Add(customerOption);
        dispatchGridVm.CustomerOptions.Add(customerOption);
        customerAliasVm.Customers.Add(customerOption);

        return
        [
            new(new CashPaymentView(), new CashPaymentWorkspaceViewModel(cash, customers, catalog, company, printer, documentInteraction, prompt, gate, toast)),
            new(new CashReceiptView(), new CashReceiptWorkspaceViewModel(cash, customers, dispatchQueries, company, printer, documentInteraction, prompt, gate, toast)),
            new(new CityView(), cityVm),
            new(new CustomerAliasView(), customerAliasVm),
            new(new CustomerView(), customerVm),
            new(new DashboardView(), new DashboardWorkspaceViewModel(dashboard, current, gate, toast)),
            new(new DepartmentView(), departmentVm),
            new(new DispatchGridEditView(), dispatchGridVm),
            new(new DispatchHubView(), dispatchHubVm),
            new(new DispatchImportView(), dispatchImportVm),
            new(new DispatchView(), dispatchVm),
            new(new DriverView(), driverVm),
            new(new EmployeeView(), employeeVm),
            new(new InvoiceView(), new InvoiceWorkspaceViewModel(invoices, customers, company, printer, documentInteraction, prompt, gate, toast)),
            new(new JobTitleView(), jobTitleVm),
            new(new LocationAliasView(), locationAliasVm),
            new(new LocationView(), locationVm),
            new(new LookupView(), new LookupWorkspaceViewModel(dispatchQueries, current, navigator, gate, toast)),
            new(new PartnerRateView(), new PartnerRateWorkspaceViewModel(partnerRates, catalog, current, prompt, gate, toast)),
            new(new PartnerSettlementView(), new PartnerSettlementWorkspaceViewModel(settlements, catalog, current, gate, toast)),
            new(new PartnerView(), partnerVm),
            new(new PriceListView(), priceVm),
            new(new ReconcileView(), new ReconcileWorkspaceViewModel(dispatchQueries, reconciliation, changeLogs, current, navigator, gate, toast)),
            new(new ReportView(), new ReportWorkspaceViewModel(reports, dashboard, company, printer, documentInteraction, current, gate, toast)),
            new(new RouteAliasView(), routeAliasVm),
            new(new RouteCatalogView(), routeCatalogVm),
            new(new RouteView(), routeVm),
            new(new SettingsView(), settingsVm),
            new(new StatementView(), new StatementWorkspaceViewModel(statements, customers, company, printer, documentInteraction, current, gate, toast)),
            new(new TransportExceptionView(), exceptionViewModel),
            new(new UserView(), userVm),
            new(new VehicleView(), new VehicleWorkspaceViewModel(vehicleService, catalog, dispatchQueries, current, prompt, printer, documentInteraction, gate, toast))
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

    private static T FindDescendant<T>(DependencyObject parent) where T : DependencyObject =>
        FindDescendants<T>(parent).FirstOrDefault()
        ?? throw new InvalidOperationException($"Không tìm thấy control {typeof(T).Name}.");

    private static IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
                yield return match;
            foreach (var descendant in FindDescendants<T>(child))
                yield return descendant;
        }
    }

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
