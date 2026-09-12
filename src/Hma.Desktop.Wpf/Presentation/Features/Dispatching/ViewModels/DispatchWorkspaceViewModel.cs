using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Customers;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Settings;
using Hma.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Hma.Reporting;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.ViewModels;

public partial class DispatchWorkspaceViewModel(
    DispatchOrderQueryService queries,
    DispatchOrderEditorService editorService,
    DispatchDocumentService documents,
    IDocumentInteractionService documentInteraction,
    CatalogOptionQueryService catalog,
    CustomerService customers,
    CompanyService company,
    IDocumentRenderer printer,
    ICurrentUser current,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterPlate;
    [ObservableProperty] private string? filterCustomerCode;
    [ObservableProperty] private int? filterCustomerId;
    [ObservableProperty] private int? filterStatus;
    [ObservableProperty] private int? filterRecon;
    [ObservableProperty] private int? filterVehicleTypeId;
    [ObservableProperty] private int? filterPickupLocationId;
    [ObservableProperty] private int? filterDeliveryLocationId;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private decimal? filterAmountFrom;
    [ObservableProperty] private decimal? filterAmountTo;
    [ObservableProperty] private decimal? filterTonnage;
    [ObservableProperty] private DispatchOrderEditorModel editor = new();
    [ObservableProperty] private DispatchOrderSummary? selected;
    [ObservableProperty] private DispatchDocumentModel? selectedDocument;
    [ObservableProperty] private DispatchOrderLineModel? selectedLine;
    [ObservableProperty] private int selectedDocKind = (int)DispatchDocumentKind.DeliveryNote;
    [ObservableProperty] private int pickupHour;
    [ObservableProperty] private int pickupMinute;
    [ObservableProperty] private DateTime? pickupDate = DateTime.Today;
    [ObservableProperty] private string? driverPhone;
    [ObservableProperty] private string? vehicleLookupText;
    [ObservableProperty] private string? driverLookupText;
    [ObservableProperty] private string createdByLabel = "";
    [ObservableProperty] private string? freightSourceLabel;
    [ObservableProperty] private bool freightMissing;
    [ObservableProperty] private bool freightMatched;
    [ObservableProperty] private string? matchedPriceListCode;
    private bool _suppressAutoFreight;
    private bool _suppressPickupComposition;
    private bool _suppressVehicleLookup;
    private bool _suppressDriverLookup;
    private int _vehicleLookupGeneration;
    private int _driverLookupGeneration;
    private byte[] _versionToken = [];

    public ObservableCollection<DispatchOrderSummary> Items { get; } = [];
    public ObservableCollection<CustomerSummary> CustomerOptions { get; } = [];
    public ObservableCollection<DriverOption> Drivers { get; } = [];
    public ObservableCollection<VehicleOption> Vehicles { get; } = [];
    public ObservableCollection<VehicleTypeOption> VehicleTypes { get; } = [];
    public ObservableCollection<LocationOption> Locations { get; } = [];
    public ObservableCollection<RouteOption> Routes { get; } = [];
    public ObservableCollection<PaymentMethodOption> PaymentMethods { get; } = [];
    public ObservableCollection<DispatchDocumentModel> DocumentItems { get; } = [];
    public ObservableCollection<DispatchOrderLineModel> LineItems { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Status, Editor.ReconciliationStatus,
        Editor.CustomerId, Editor.SenderCustomerId, Editor.SenderName, Editor.SenderPhone,
        Editor.SenderAddress, Editor.SenderTaxCode, Editor.ReceiverCustomerId, Editor.ReceiverName,
        Editor.ReceiverPhone, Editor.ReceiverAddress, Editor.ReceiverTaxCode, Editor.PickupAt,
        Editor.PickupAddress, Editor.DeliveryAddress, Editor.RouteId, Editor.VehicleId,
        Editor.DriverId, Editor.VehicleTypeId, Editor.EmployeeId, Editor.PaymentMethodId,
        Editor.BillingYear, Editor.BillingMonth, Editor.ArNumber, Editor.UnitPrice,
        Editor.Surcharge, Editor.ExtraCost, Editor.IsFreightManual, Editor.FreightOverrideReason,
        Editor.BuyUnitPrice, Editor.BuySurcharge, Editor.BuyExtraCost, Editor.IsBuyManual,
        Editor.BuyOverrideReason, Editor.Notes,
        string.Join(";", LineItems.Select(line =>
            $"{line.Id},{line.LineNumber},{line.GoodsName},{line.PackageCount},{line.Route},{line.Kilometers},{line.Notes}")),
        string.Join(";", DocumentItems.Select(document =>
            $"{document.Id},{document.Kind},{document.FileName},{document.StoredPath}")));
    public IReadOnlyList<int> Hours { get; } = Enumerable.Range(0, 24).ToList();
    public IReadOnlyList<int> Minutes { get; } = Enumerable.Range(0, 60).ToList();
    public IReadOnlyList<NamedInt> StatusOptions { get; } =
    [
        new() { Value = (int)DispatchStatus.Draft, Name = "Nháp" },
        new() { Value = (int)DispatchStatus.Issued, Name = "Đã phát hành" },
        new() { Value = (int)DispatchStatus.Completed, Name = "Hoàn thành" },
        new() { Value = (int)DispatchStatus.Cancelled, Name = "Đã hủy" }
    ];
    public IReadOnlyList<NamedInt> ReconOptions { get; } =
    [
        new() { Value = (int)ReconciliationStatus.Pending, Name = "Chưa đối soát" },
        new() { Value = (int)ReconciliationStatus.Submitted, Name = "Chờ duyệt" },
        new() { Value = (int)ReconciliationStatus.Reconciled, Name = "Đã đối soát" },
        new() { Value = (int)ReconciliationStatus.Rejected, Name = "Bị từ chối" }
    ];
    public IReadOnlyList<NamedInt> DocKindOptions { get; } =
    [
        new() { Value = (int)DispatchDocumentKind.DispatchOrder, Name = "Lệnh điều xe" },
        new() { Value = (int)DispatchDocumentKind.DeliveryNote, Name = "Biên bản giao hàng" },
        new() { Value = (int)DispatchDocumentKind.Invoice, Name = "Hóa đơn / chứng từ" },
        new() { Value = (int)DispatchDocumentKind.Other, Name = "Khác" }
    ];

    private int? EditorCustomerAccountantEmployeeId =>
        Editor.Customer?.AccountantEmployeeId
        ?? CustomerOptions.FirstOrDefault(c => c.Id == Editor.CustomerId)?.AccountantEmployeeId;

    public override bool IsEditorReadOnly =>
        IsViewMode || !Editor.CanEdit;

    public bool IsFreightReadOnly => IsEditorReadOnly;

    public override bool AreFieldsEnabled => !IsEditorReadOnly;

    public bool CanPersist => CanSave && !IsEditorReadOnly;

    public bool CanLock =>
        CanUpdate && Editor.Id != 0 && Editor.Status == DispatchStatus.Completed
        && Editor.ConfirmedAt is null
        && Editor.ReconciliationStatus != ReconciliationStatus.Reconciled
        && DispatchConfirmRules.CanConfirm(current.EmployeeId, current.IsManager, EditorCustomerAccountantEmployeeId);

    public bool CanUnlock =>
        CanUpdate && Editor.Id != 0 && Editor.ConfirmedAt is not null
        && Editor.ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected
        && DispatchConfirmRules.CanUnlock(current.EmployeeId, current.IsManager, EditorCustomerAccountantEmployeeId);

    public bool CanComplete =>
        CanUpdate && Editor.Id != 0 && Editor.Status == DispatchStatus.Issued && Editor.CanEdit;

    public bool CanAttachDocument =>
        CanUpdate && Editor.Id != 0 && CanMutateDocuments;

    public bool CanDeleteDocument =>
        CanDelete && SelectedDocument is not null && CanMutateDocuments;

    public bool CanOpenDocument =>
        SelectedDocument is not null && !string.IsNullOrWhiteSpace(SelectedDocument.StoredPath);

    private bool CanMutateDocuments =>
        !Editor.IsDeleted
        && Editor.Status != DispatchStatus.Cancelled
        && Editor.ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected;

    public string TonnageLabel
    {
        get
        {
            var vehicle = Vehicles.FirstOrDefault(x => x.Id == Editor.VehicleId);
            var tons = vehicle?.Tonnage
                       ?? vehicle?.VehicleType?.Tonnage
                       ?? VehicleTypes.FirstOrDefault(x => x.Id == Editor.VehicleTypeId)?.Tonnage
                       ?? Editor.Vehicle?.Tonnage
                       ?? Editor.Vehicle?.VehicleType?.Tonnage
                       ?? Editor.VehicleType?.Tonnage;
            return tons is null or 0 ? "" : $"{tons:0.##} tấn";
        }
    }

    public int EditorStatus
    {
        get => (int)Editor.Status;
        set
        {
            Editor.Status = (DispatchStatus)value;
            OnPropertyChanged();
            NotifyLockChrome();
        }
    }

    public int? EditorCustomerId
    {
        get => Editor.CustomerId;
        set
        {
            Editor.Customer = null;
            Editor.SenderCustomer = null;
            Editor.CustomerId = value;
            Editor.SenderCustomerId = value;
            ApplySenderSnapshot();
            OnPropertyChanged();
            NotifyLockChrome();
            _ = AutoFreightAsync();
        }
    }

    public int? EditorSenderId
    {
        get => Editor.SenderCustomerId;
        set
        {
            Editor.SenderCustomer = null;
            Editor.Customer = null;
            Editor.SenderCustomerId = value;
            Editor.CustomerId = value;
            ApplySenderSnapshot();
            OnPropertyChanged();
            _ = AutoFreightAsync();
        }
    }

    public int? EditorReceiverId
    {
        get => Editor.ReceiverCustomerId;
        set
        {
            Editor.ReceiverCustomer = null;
            Editor.ReceiverCustomerId = value;
            ApplyReceiverSnapshot();
            OnPropertyChanged();
        }
    }

    public int? EditorRouteId
    {
        get => Editor.RouteId;
        set
        {
            Editor.RouteId = value;
            OnPropertyChanged();
            _ = AutoFreightAsync();
        }
    }

    public int? EditorVehicleId
    {
        get => Editor.VehicleId;
        set
        {
            Editor.VehicleId = value;
            ApplyVehicleDefaults();
            OnPropertyChanged();
            OnPropertyChanged(nameof(TonnageLabel));
            _ = AutoFreightAsync();
        }
    }

    public int? EditorDriverId
    {
        get => Editor.DriverId;
        set
        {
            Editor.DriverId = value;
            ApplyDriverPhone();
            OnPropertyChanged();
        }
    }

    public int? EditorVehicleTypeId
    {
        get => Editor.VehicleTypeId;
        set
        {
            Editor.VehicleTypeId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TonnageLabel));
            _ = AutoFreightAsync();
        }
    }

    public int? EditorPaymentMethodId
    {
        get => Editor.PaymentMethodId;
        set
        {
            Editor.PaymentMethodId = value;
            OnPropertyChanged();
        }
    }

    public string CodeLabel => string.IsNullOrWhiteSpace(Editor.Code) ? "(cấp khi lưu)" : Editor.Code;

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.DispatchOrders);
        UsePrompt(prompt);
        NotifyLockChrome();
        await RunAsync(async () =>
        {
            await ReloadLookupsAsync();
            await SearchCore();
        });
    }

    private async Task ReloadLookupsAsync()
    {
        CustomerOptions.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            CustomerOptions.Add(c);
        Drivers.Clear();
        foreach (var d in await catalog.DriverOptionsAsync(null)) Drivers.Add(d);
        Vehicles.Clear();
        foreach (var v in await catalog.VehicleOptionsAsync(null)) Vehicles.Add(v);
        VehicleTypes.Clear();
        foreach (var v in await catalog.VehicleTypesAsync()) VehicleTypes.Add(v);
        Locations.Clear();
        foreach (var l in await catalog.LocationsAsync()) Locations.Add(l);
        Routes.Clear();
        foreach (var r in await catalog.RoutesAsync()) Routes.Add(r);
        PaymentMethods.Clear();
        foreach (var p in await catalog.PaymentMethodsAsync()) PaymentMethods.Add(p);
    }

    private Task<List<DispatchOrderSummary>> QueryAsync(DateTime? from, DateTime? to) =>
        queries.SearchAsync(
            FilterCode, from, to, FilterCustomerId, FilterPlate, FilterStatus, FilterRecon,
            null, null, FilterVehicleTypeId, FilterDeliveryLocationId, FilterAmountFrom, FilterAmountTo,
            pickupLocationId: FilterPickupLocationId, customerCode: FilterCustomerCode, tonnage: FilterTonnage);

    [RelayCommand]
    private async Task Search() => await RunAsync(SearchCore);

    private async Task SearchCore()
    {
        Items.Clear();
        foreach (var o in await QueryAsync(FilterFrom, FilterTo))
            Items.Add(o);
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterCode = FilterPlate = FilterCustomerCode = null;
        FilterCustomerId = FilterStatus = FilterRecon = FilterVehicleTypeId = FilterDeliveryLocationId = FilterPickupLocationId = null;
        FilterFrom = FilterTo = null;
        FilterAmountFrom = FilterAmountTo = FilterTonnage = null;
        await Search();
    }

    [RelayCommand]
    private async Task NewItem()
    {
        if (!CanCreate) return;
        _suppressAutoFreight = true;
        Editor = DispatchOrderEditorModel.From(await editorService.CreateNewAsync());
        _versionToken = [];
        BindEditorChrome();
        DocumentItems.Clear();
        LineItems.Clear();
        foreach (var line in Editor.Lines) LineItems.Add(line);
        _suppressAutoFreight = false;
        OnPropertyChanged(nameof(EditorStatus));
        NotifyEditorBindings();
        RefreshFreightChrome();
        EnterCreateState("Thêm lệnh điều xe", CaptureEditorState);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await OpenByIdAsync(Selected.Id);
    }

    public async Task OpenByIdAsync(int id)
    {
        try
        {
            await OpenAsync(id);
        }
        catch (Exception ex)
        {
            ShowToast(PersistenceGuard.Translate(ex).Message, isError: true);
        }
    }

    private async Task OpenAsync(int id)
    {
        await OperationGate.RunAsync(async () =>
        {
            var full = await queries.GetAsync(id);
            if (full is null) return;
            _suppressAutoFreight = true;
            EnsureCurrentLookupOptions(full);
            Editor = DispatchOrderEditorModel.From(full);
            _versionToken = full.VersionToken.ToArray();
            BindEditorChrome();
            OnPropertyChanged(nameof(EditorStatus));
            DocumentItems.Clear();
            foreach (var d in full.Documents) DocumentItems.Add(DispatchDocumentModel.From(d));
            LineItems.Clear();
            foreach (var line in full.Lines.OrderBy(l => l.LineNumber)) LineItems.Add(DispatchOrderLineModel.From(line));
            if (LineItems.Count == 0)
                LineItems.Add(new DispatchOrderLineModel { LineNumber = 1 });
            _suppressAutoFreight = false;
            NotifyEditorBindings();
            RefreshFreightChrome();
            var canEdit = CanEditExisting && !IsEditorReadOnly;
            EnterExistingState($"Xem lệnh — {Editor.Code}", $"Sửa lệnh — {Editor.Code}", canEdit, CaptureEditorState);
        });
    }

    internal void BindEditorChrome()
    {
        var pickup = PickupTimeSelection.From(Editor.PickupAt);
        _suppressPickupComposition = true;
        PickupDate = pickup.Date;
        PickupHour = pickup.Hour;
        PickupMinute = pickup.Minute;
        _suppressPickupComposition = false;
        _suppressVehicleLookup = true;
        VehicleLookupText = Vehicles.FirstOrDefault(x => x.Id == Editor.VehicleId)?.PlateNumber
                            ?? Editor.Vehicle?.PlateNumber;
        _suppressVehicleLookup = false;
        _suppressDriverLookup = true;
        DriverLookupText = Drivers.FirstOrDefault(x => x.Id == Editor.DriverId)?.Name;
        _suppressDriverLookup = false;
        CreatedByLabel = $"{Editor.CreatedAt:dd/MM/yyyy HH:mm} · {Editor.CreatedByUser?.DisplayName ?? Editor.CreatedByUser?.UserName ?? ""}";
        ApplyVehicleDefaults();
        ApplyDriverPhone();
    }

    private void EnsureCurrentLookupOptions(DispatchOrderDetails order)
    {
        if (order.Header.Vehicle is not null && Vehicles.All(x => x.Id != order.Header.Vehicle.Id))
            Vehicles.Insert(0, order.Header.Vehicle);
        if (order.Header.Driver is not null && Drivers.All(x => x.Id != order.Header.Driver.Id))
            Drivers.Insert(0, order.Header.Driver);
    }

    private void NotifyEditorBindings()
    {
        OnPropertyChanged(nameof(EditorCustomerId));
        OnPropertyChanged(nameof(EditorSenderId));
        OnPropertyChanged(nameof(EditorReceiverId));
        OnPropertyChanged(nameof(EditorRouteId));
        OnPropertyChanged(nameof(EditorVehicleId));
        OnPropertyChanged(nameof(EditorDriverId));
        OnPropertyChanged(nameof(EditorVehicleTypeId));
        OnPropertyChanged(nameof(EditorPaymentMethodId));
        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(CodeLabel));
        OnPropertyChanged(nameof(TonnageLabel));
        NotifyLockChrome();
    }

    private void NotifyLockChrome()
    {
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(IsFreightReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
        OnPropertyChanged(nameof(CanLock));
        OnPropertyChanged(nameof(CanUnlock));
        OnPropertyChanged(nameof(CanComplete));
        OnPropertyChanged(nameof(CanAttachDocument));
        OnPropertyChanged(nameof(CanDeleteDocument));
        OnPropertyChanged(nameof(CanOpenDocument));
        OnPropertyChanged(nameof(CanPersist));
        OnPropertyChanged(nameof(Editor));
    }

    protected override void OnWorkspaceModeChanged() => NotifyLockChrome();

    private void ApplySenderSnapshot()
    {
        var c = CustomerOptions.FirstOrDefault(x => x.Id == (Editor.CustomerId ?? Editor.SenderCustomerId));
        if (c is null) return;
        Editor.SenderName = c.Name;
        Editor.SenderPhone = c.Phone;
        Editor.SenderAddress = c.Address;
        Editor.SenderTaxCode = c.TaxCode;
        if (string.IsNullOrWhiteSpace(Editor.PickupAddress)) Editor.PickupAddress = c.Address;
        OnPropertyChanged(nameof(Editor));
    }

    private void ApplyReceiverSnapshot()
    {
        var c = CustomerOptions.FirstOrDefault(x => x.Id == Editor.ReceiverCustomerId);
        if (c is null) return;
        Editor.ReceiverName = c.Name;
        Editor.ReceiverPhone = c.Phone;
        Editor.ReceiverAddress = c.Address;
        Editor.ReceiverTaxCode = c.TaxCode;
        if (string.IsNullOrWhiteSpace(Editor.DeliveryAddress)) Editor.DeliveryAddress = c.Address;
        OnPropertyChanged(nameof(Editor));
    }

    private void ApplyVehicleDefaults()
    {
        var v = Vehicles.FirstOrDefault(x => x.Id == Editor.VehicleId);
        if (v is not null)
        {
            Editor.VehicleTypeId = v.VehicleTypeId;
            OnPropertyChanged(nameof(EditorVehicleTypeId));
        }
        OnPropertyChanged(nameof(TonnageLabel));
    }

    private void ApplyDriverPhone()
    {
        DriverPhone = Drivers.FirstOrDefault(x => x.Id == Editor.DriverId)?.Phone;
    }

    partial void OnVehicleLookupTextChanged(string? value)
    {
        if (!_suppressVehicleLookup)
            _ = RefreshVehicleOptionsAsync(value);
    }

    partial void OnDriverLookupTextChanged(string? value)
    {
        if (!_suppressDriverLookup)
            _ = RefreshDriverOptionsAsync(value);
    }

    private async Task RefreshVehicleOptionsAsync(string? text)
    {
        var generation = Interlocked.Increment(ref _vehicleLookupGeneration);
        await Task.Delay(250);
        if (generation != _vehicleLookupGeneration) return;

        try
        {
            var rows = await OperationGate.RunAsync(() => catalog.VehicleOptionsAsync(text));
            if (generation != _vehicleLookupGeneration) return;

            _suppressVehicleLookup = true;
            Vehicles.Clear();
            foreach (var row in rows) Vehicles.Add(row);

            var exact = rows.FirstOrDefault(row =>
                string.Equals(row.PlateNumber.Trim(), text?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                EditorVehicleId = exact.Id;
        }
        catch (Exception ex)
        {
            if (generation == _vehicleLookupGeneration)
                ShowToast(PersistenceGuard.Translate(ex).Message, isError: true);
        }
        finally
        {
            _suppressVehicleLookup = false;
        }
    }

    private async Task RefreshDriverOptionsAsync(string? text)
    {
        var generation = Interlocked.Increment(ref _driverLookupGeneration);
        await Task.Delay(250);
        if (generation != _driverLookupGeneration) return;

        try
        {
            var rows = await OperationGate.RunAsync(() => catalog.DriverOptionsAsync(text));
            if (generation != _driverLookupGeneration) return;

            _suppressDriverLookup = true;
            Drivers.Clear();
            foreach (var row in rows) Drivers.Add(row);

            var exact = rows.FirstOrDefault(row =>
                string.Equals(row.Name.Trim(), text?.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Code.Trim(), text?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                EditorDriverId = exact.Id;
        }
        catch (Exception ex)
        {
            if (generation == _driverLookupGeneration)
                ShowToast(PersistenceGuard.Translate(ex).Message, isError: true);
        }
        finally
        {
            _suppressDriverLookup = false;
        }
    }

    private void CombinePickupAt()
    {
        if (_suppressPickupComposition) return;
        Editor.PickupAt = new PickupTimeSelection(PickupDate ?? DateTime.Today, PickupHour, PickupMinute)
            .ToDateTime();
    }

    partial void OnPickupDateChanged(DateTime? value)
    {
        CombinePickupAt();
        if (Editor.Id == 0)
        {
            Editor.BillingYear = Editor.PickupAt.Year;
            Editor.BillingMonth = Editor.PickupAt.Month;
            OnPropertyChanged(nameof(Editor));
        }
        _ = AutoFreightAsync();
    }

    partial void OnPickupHourChanged(int value) => CombinePickupAt();
    partial void OnPickupMinuteChanged(int value) => CombinePickupAt();

    private bool HasFreightKeys =>
        Editor.CustomerId is not null
        && Editor.RouteId is not null
        && Editor.VehicleTypeId is not null;

    private void RefreshFreightChrome(FreightQuote? quote = null, bool ranLookup = false)
    {
        var state = ranLookup
            ? FreightMatchState.FromLookup(HasFreightKeys, quote)
            : FreightMatchState.FromSaved(
                HasFreightKeys,
                Editor.PriceListItemId,
                Editor.PriceListCode,
                Editor.PriceSourceSnapshot);
        FreightMatched = state.IsMatched;
        FreightMissing = state.IsMissing;
        MatchedPriceListCode = state.PriceListCode;
        FreightSourceLabel = state.SourceLabel;
    }

    private int _freightGeneration;

    private async Task AutoFreightAsync()
    {
        if (_suppressAutoFreight) return;
        var gen = Interlocked.Increment(ref _freightGeneration);
        try
        {
            await OperationGate.RunAsync(async () =>
            {
                if (gen != _freightGeneration) return;
                CombinePickupAt();
                var result = await editorService.ApplyFreightAsync(Editor.ToCommand(_versionToken));
                if (gen != _freightGeneration) return;
                Editor.ApplyCalculatedFields(result.Order);
                Editor.PriceListCode = result.Quote?.PriceListCode;
                RefreshFreightChrome(result.Quote, ranLookup: true);
                OnPropertyChanged(nameof(Editor));
            });
        }
        catch (Exception ex)
        {
            ShowToast(PersistenceGuard.Translate(ex).Message, isError: true);
        }
    }

    [RelayCommand]
    private async Task CalcFreight()
    {
        await AutoFreightAsync();
        if (FreightMissing)
            ShowToast("Chưa khớp bảng giá cho ngày chạy này.", isError: true);
    }

    [RelayCommand]
    private void AddLine()
    {
        LineItems.Add(new DispatchOrderLineModel { LineNumber = LineItems.Count + 1 });
    }

    [RelayCommand]
    private void RemoveLine(DispatchOrderLineModel? line)
    {
        if (!AreFieldsEnabled || line is null) return;
        LineItems.Remove(line);
        var lineNumber = 1;
        foreach (var item in LineItems)
            item.LineNumber = lineNumber++;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor.Id == 0 ? !CanCreate : !CanPersist) return;
        await RunAsync(async () =>
        {
            CombinePickupAt();
            Editor.Lines.Clear();
            foreach (var line in LineItems) Editor.Lines.Add(line);
            await editorService.SaveAsync(Editor.ToCommand(_versionToken));
            await SearchCore();
        }, "Đã lưu lệnh điều xe.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await editorService.DeleteAsync(Editor.Id);
            await SearchCore();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa lệnh.");
    }

    [RelayCommand]
    private async Task Complete()
    {
        if (!CanComplete || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await editorService.SetStatusAsync(Editor.Id, DispatchStatus.Completed);
            await OpenAsync(Editor.Id);
            await SearchCore();
        }, "Đã đánh dấu hoàn thành.");
    }

    [RelayCommand]
    private async Task Lock()
    {
        if (!CanLock || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await editorService.LockAsync(Editor.Id, Editor.ArNumber);
            await OpenAsync(Editor.Id);
        }, "Đã chốt lệnh.");
    }

    [RelayCommand]
    private async Task Unlock()
    {
        if (!CanUnlock || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await editorService.UnlockAsync(Editor.Id);
            await OpenAsync(Editor.Id);
        }, "Đã bỏ chốt.");
    }

    [RelayCommand]
    private async Task Attach()
    {
        if (!CanAttachDocument)
        {
            ShowToast(Editor.Id == 0
                ? "Lưu lệnh trước khi đính kèm chứng từ."
                : "Không thể thay đổi chứng từ ở trạng thái hiện tại.", isError: true);
            return;
        }
        var dlg = new OpenFileDialog { Title = "Chọn chứng từ" };
        if (dlg.ShowDialog() != true) return;
        await RunAsync(async () =>
        {
            await using var stream = File.OpenRead(dlg.FileName);
            await documents.AttachAsync(Editor.Id, (DispatchDocumentKind)SelectedDocKind, Path.GetFileName(dlg.FileName), stream);
            await OpenAsync(Editor.Id);
            ShowToast(await documents.IsUsingLocalFallbackAsync()
                ? "Đã đính kèm (lưu máy local — đặt đường dẫn mạng trong Tham số khi dùng nhiều máy)."
                : "Đã đính kèm chứng từ.");
        });
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        if (!CanOpenDocument || SelectedDocument is null) return;
        await RunAsync(async () =>
        {
            await using var content = await documents.OpenReadAsync(SelectedDocument.StoredPath);
            await documentInteraction.OpenAsync(SelectedDocument.FileName, content);
        });
    }

    [RelayCommand]
    private async Task DeleteDocument()
    {
        if (!CanDeleteDocument || SelectedDocument is null || !ConfirmDelete()) return;
        var id = SelectedDocument.Id;
        await RunAsync(async () =>
        {
            await documents.DeleteAsync(id);
            await OpenAsync(Editor.Id);
        }, "Đã xóa chứng từ.");
    }

    partial void OnSelectedDocumentChanged(DispatchDocumentModel? value)
    {
        OnPropertyChanged(nameof(CanDeleteDocument));
        OnPropertyChanged(nameof(CanOpenDocument));
    }

    [RelayCommand]
    private async Task Print()
    {
        if (!CanPrint || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            var full = await queries.GetPrintDetailsAsync(Editor.Id);
            if (full is null) return;
            var companyInfo = await company.GetAsync();
            await documentInteraction.OpenAsync(printer.PrintDispatch(full, companyInfo));
        });
    }

    [RelayCommand]
    private async Task PrintSummaryDay()
    {
        if (!CanPrint) return;
        await RunAsync(async () =>
        {
            var day = FilterFrom?.Date ?? DateTime.Today;
            var list = await QueryAsync(day, day);
            await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH ĐIỀU XE NGÀY {day:dd/MM/yyyy}");
        });
    }

    [RelayCommand]
    private async Task PrintSummaryMonth()
    {
        if (!CanPrint) return;
        await RunAsync(async () =>
        {
            var month = FilterFrom ?? DateTime.Today;
            var from = new DateTime(month.Year, month.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var list = await QueryAsync(from, to);
            await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH ĐIỀU XE THÁNG {month:MM/yyyy}");
        });
    }

    [RelayCommand]
    private async Task PrintSummaryCustomer()
    {
        if (!CanPrint) return;
        if (FilterCustomerId is null) { ShowToast("Chọn khách hàng trên bộ lọc để in theo khách.", isError: true); return; }
        await RunAsync(async () =>
        {
            var from = FilterFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = FilterTo ?? DateTime.Today;
            var list = await QueryAsync(from, to);
            var name = CustomerOptions.FirstOrDefault(c => c.Id == FilterCustomerId)?.Name ?? "";
            await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH THEO KHÁCH {name} ({from:dd/MM}–{to:dd/MM/yyyy})");
        });
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (!CanPrint) return;
        await RunAsync(
            () => documentInteraction.OpenAsync(printer.ExportDispatchExcel(Items.ToList())),
            "Đã xuất danh sách cước.");
    }

    private async Task OpenSummaryAsync(List<DispatchOrderSummary> list, string title)
    {
        var companyInfo = await company.GetAsync();
        await documentInteraction.OpenAsync(printer.PrintDispatchSummary(list, title, companyInfo));
        ShowToast($"Đã in {list.Count} lệnh.");
    }

}
