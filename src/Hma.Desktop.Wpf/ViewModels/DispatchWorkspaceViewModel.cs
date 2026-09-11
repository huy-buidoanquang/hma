using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Hma.Reporting;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DispatchWorkspaceViewModel(
    DispatchOrderService orders,
    DispatchDocumentService documents,
    CatalogService catalog,
    CustomerService customers,
    CompanyService company,
    IDocumentPrinter printer,
    ICurrentUser current,
    IUserPrompt prompt) : WorkspaceBase
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
    [ObservableProperty] private DispatchOrder editor = new();
    [ObservableProperty] private DispatchOrder? selected;
    [ObservableProperty] private DispatchDocument? selectedDocument;
    [ObservableProperty] private DispatchOrderLine? selectedLine;
    [ObservableProperty] private int selectedDocKind = (int)DispatchDocumentKind.DeliveryNote;
    [ObservableProperty] private int pickupHour;
    [ObservableProperty] private int pickupMinute;
    [ObservableProperty] private DateTime? pickupDate = DateTime.Today;
    [ObservableProperty] private string? driverPhone;
    [ObservableProperty] private string? vehiclePlate;
    [ObservableProperty] private string? vehicleLookupText;
    [ObservableProperty] private string? driverLookupText;
    [ObservableProperty] private string? newPartyName;
    [ObservableProperty] private string? newPartyPhone;
    [ObservableProperty] private string? newPartyAddress;
    [ObservableProperty] private string? newPartyTaxCode;
    [ObservableProperty] private int? newPartyCityId;
    [ObservableProperty] private string createdByLabel = "";
    [ObservableProperty] private string? freightSourceLabel;
    [ObservableProperty] private bool freightMissing;
    private bool _suppressAutoFreight;
    private bool _suppressVehicleLookup;
    private bool _suppressDriverLookup;
    private int _vehicleLookupGeneration;
    private int _driverLookupGeneration;

    public ObservableCollection<DispatchOrder> Items { get; } = [];
    public ObservableCollection<Customer> CustomerOptions { get; } = [];
    public ObservableCollection<Driver> Drivers { get; } = [];
    public ObservableCollection<Vehicle> Vehicles { get; } = [];
    public ObservableCollection<VehicleType> VehicleTypes { get; } = [];
    public ObservableCollection<Location> Locations { get; } = [];
    public ObservableCollection<City> Cities { get; } = [];
    public ObservableCollection<Route> Routes { get; } = [];
    public ObservableCollection<PaymentMethod> PaymentMethods { get; } = [];
    public ObservableCollection<DispatchDocument> DocumentItems { get; } = [];
    public ObservableCollection<DispatchOrderLine> LineItems { get; } = [];
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

    private Customer? EditorCustomer =>
        Editor.Customer ?? CustomerOptions.FirstOrDefault(c => c.Id == Editor.CustomerId);

    public override bool IsEditorReadOnly =>
        IsViewMode || !Editor.CanEdit;

    public bool IsFreightReadOnly => IsEditorReadOnly;

    public override bool AreFieldsEnabled => !IsEditorReadOnly;

    public bool CanPersist => CanSave && !IsEditorReadOnly;

    public bool CanLock =>
        CanUpdate && Editor.Id != 0 && Editor.Status == DispatchStatus.Completed
        && Editor.ConfirmedAt is null
        && Editor.ReconciliationStatus != ReconciliationStatus.Reconciled
        && DispatchConfirmRules.CanConfirm(current.User, EditorCustomer);

    public bool CanUnlock =>
        CanUpdate && Editor.Id != 0 && Editor.ConfirmedAt is not null
        && Editor.ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected
        && DispatchConfirmRules.CanUnlock(current.User, EditorCustomer);

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
            var vehicle = Vehicles.FirstOrDefault(x => x.Id == Editor.VehicleId) ?? Editor.Vehicle;
            var tons = vehicle?.Tonnage
                       ?? vehicle?.VehicleType?.Tonnage
                       ?? VehicleTypes.FirstOrDefault(x => x.Id == Editor.VehicleTypeId)?.Tonnage
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
        Cities.Clear();
        foreach (var c in await catalog.CitiesAsync()) Cities.Add(c);
        Routes.Clear();
        foreach (var r in await catalog.RoutesAsync()) Routes.Add(r);
        PaymentMethods.Clear();
        foreach (var p in await catalog.PaymentMethodsAsync()) PaymentMethods.Add(p);
    }

    private Task<List<DispatchOrder>> QueryAsync(DateTime? from, DateTime? to) =>
        orders.SearchAsync(
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
        Status = $"{Items.Count} lệnh điều xe";
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
        Editor = await orders.CreateNewAsync();
        BindEditorChrome();
        DocumentItems.Clear();
        LineItems.Clear();
        foreach (var line in Editor.Lines) LineItems.Add(line);
        _suppressAutoFreight = false;
        OnPropertyChanged(nameof(EditorStatus));
        NotifyEditorBindings();
        RefreshFreightChrome();
        EnterCreate("Thêm lệnh điều xe", Editor, LineItems, DocumentItems);
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
        await SessionDbGate.RunAsync(async () =>
        {
            var full = await orders.GetAsync(id);
            if (full is null) return;
            _suppressAutoFreight = true;
            EnsureCurrentLookupOptions(full);
            Editor = full;
            BindEditorChrome();
            OnPropertyChanged(nameof(EditorStatus));
            DocumentItems.Clear();
            foreach (var d in full.Documents) DocumentItems.Add(d);
            LineItems.Clear();
            foreach (var line in full.Lines.OrderBy(l => l.LineNumber)) LineItems.Add(line);
            if (LineItems.Count == 0)
                LineItems.Add(new DispatchOrderLine { LineNumber = 1 });
            _suppressAutoFreight = false;
            NotifyEditorBindings();
            RefreshFreightChrome();
            var canEdit = CanEditExisting && !IsEditorReadOnly;
            EnterExisting($"Xem lệnh — {Editor.Code}", $"Sửa lệnh — {Editor.Code}", canEdit, Editor, LineItems, DocumentItems);
        });
    }

    private void BindEditorChrome()
    {
        PickupDate = Editor.PickupAt.Date;
        PickupHour = Editor.PickupAt.Hour;
        PickupMinute = Editor.PickupAt.Minute;
        CreatedByLabel = $"{Editor.CreatedAt:dd/MM/yyyy HH:mm} · {Editor.CreatedByUser?.DisplayName ?? Editor.CreatedByUser?.UserName ?? ""}";
        ApplyVehicleDefaults();
        ApplyDriverPhone();
    }

    private void EnsureCurrentLookupOptions(DispatchOrder order)
    {
        if (order.Vehicle is not null && Vehicles.All(x => x.Id != order.Vehicle.Id))
            Vehicles.Insert(0, order.Vehicle);
        if (order.Driver is not null && Drivers.All(x => x.Id != order.Driver.Id))
            Drivers.Insert(0, order.Driver);
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
        VehiclePlate = v?.PlateNumber;
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
            var rows = await SessionDbGate.RunAsync(() => catalog.VehicleOptionsAsync(text));
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
            var rows = await SessionDbGate.RunAsync(() => catalog.DriverOptionsAsync(text));
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
        var date = PickupDate ?? DateTime.Today;
        Editor.PickupAt = date.Date.AddHours(PickupHour).AddMinutes(PickupMinute);
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
        if (quote is not null)
        {
            FreightMissing = false;
            FreightSourceLabel = quote.SourceLabel;
            return;
        }
        FreightSourceLabel = Editor.PriceSourceSnapshot ?? "";
        FreightMissing = ranLookup ? HasFreightKeys : HasFreightKeys && Editor.UnitPrice == 0 && Editor.Surcharge == 0;
    }

    private int _freightGeneration;

    private async Task AutoFreightAsync()
    {
        if (_suppressAutoFreight) return;
        var gen = Interlocked.Increment(ref _freightGeneration);
        try
        {
            await SessionDbGate.RunAsync(async () =>
            {
                if (gen != _freightGeneration) return;
                CombinePickupAt();
                var quote = await orders.ApplyFreightAsync(Editor);
                if (gen != _freightGeneration) return;
                RefreshFreightChrome(quote, ranLookup: true);
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
        LineItems.Add(new DispatchOrderLine { LineNumber = LineItems.Count + 1 });
    }

    [RelayCommand]
    private void RemoveLine(DispatchOrderLine? line)
    {
        if (!AreFieldsEnabled || line is null) return;
        LineItems.Remove(line);
        var lineNumber = 1;
        foreach (var item in LineItems)
            item.LineNumber = lineNumber++;
    }

    [RelayCommand]
    private async Task AddSenderWalkIn()
    {
        if (string.IsNullOrWhiteSpace(NewPartyName)) { ShowToast("Nhập tên khách mới.", isError: true); return; }
        await RunAsync(async () =>
        {
            var created = await orders.CreateWalkInAsync(NewPartyName, NewPartyAddress, NewPartyPhone, NewPartyTaxCode, NewPartyCityId);
            CustomerOptions.Add(created);
            EditorSenderId = created.Id;
            ClearNewParty();
            ShowToast($"Đã thêm khách {created.Code}");
        });
    }

    [RelayCommand]
    private async Task AddReceiverWalkIn()
    {
        if (string.IsNullOrWhiteSpace(NewPartyName)) { ShowToast("Nhập tên khách mới.", isError: true); return; }
        await RunAsync(async () =>
        {
            var created = await orders.CreateWalkInAsync(NewPartyName, NewPartyAddress, NewPartyPhone, NewPartyTaxCode, NewPartyCityId);
            CustomerOptions.Add(created);
            EditorReceiverId = created.Id;
            ClearNewParty();
            ShowToast($"Đã thêm khách {created.Code}");
        });
    }

    private void ClearNewParty()
    {
        NewPartyName = NewPartyPhone = NewPartyAddress = NewPartyTaxCode = null;
        NewPartyCityId = null;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor.Id == 0 ? !CanCreate : !CanPersist) return;
        await RunAsync(async () =>
        {
            CombinePickupAt();
            Editor.Lines = LineItems.ToList();
            await orders.SaveAsync(Editor);
            await SearchCore();
        }, "Đã lưu lệnh điều xe.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await orders.DeleteAsync(Editor.Id);
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
            await orders.SetStatusAsync(Editor.Id, DispatchStatus.Completed);
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
            await orders.LockAsync(Editor.Id, Editor.ArNumber);
            await OpenAsync(Editor.Id);
        }, "Đã chốt lệnh.");
    }

    [RelayCommand]
    private async Task Unlock()
    {
        if (!CanUnlock || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await orders.UnlockAsync(Editor.Id);
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
            if (await documents.IsUsingLocalFallbackAsync())
                Status = "Đã đính kèm (lưu máy local — đặt đường dẫn mạng trong Tham số khi dùng nhiều máy).";
        }, "Đã đính kèm chứng từ.");
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        if (!CanOpenDocument || SelectedDocument is null) return;
        await RunAsync(async () =>
        {
            var path = await documents.ResolvePhysicalPathAsync(SelectedDocument.StoredPath);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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

    partial void OnSelectedDocumentChanged(DispatchDocument? value)
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
            var full = await orders.GetAsync(Editor.Id) ?? Editor;
            var companyInfo = await company.GetAsync();
            var path = printer.PrintDispatch(full, companyInfo);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
    private void ExportExcel()
    {
        if (!CanPrint) return;
        var path = TemporaryReportFile.Create("DS-CUOC.xlsx");
        printer.ExportDispatchExcel(Items.ToList(), path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = "Đã xuất danh sách cước.";
    }

    private async Task OpenSummaryAsync(List<DispatchOrder> list, string title)
    {
        var companyInfo = await company.GetAsync();
        var path = printer.PrintDispatchSummary(list, title, companyInfo);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"Đã in {list.Count} lệnh.";
    }
}
