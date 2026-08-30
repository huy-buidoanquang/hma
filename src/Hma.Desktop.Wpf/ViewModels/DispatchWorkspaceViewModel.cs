using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;
using Microsoft.Win32;

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
    [ObservableProperty] private int? filterCustomerId;
    [ObservableProperty] private int? filterStatus;
    [ObservableProperty] private int? filterRecon;
    [ObservableProperty] private int? filterVehicleTypeId;
    [ObservableProperty] private int? filterDeliveryCityId;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private decimal? filterAmountFrom;
    [ObservableProperty] private decimal? filterAmountTo;
    [ObservableProperty] private DispatchOrder editor = new();
    [ObservableProperty] private DispatchOrder? selected;
    [ObservableProperty] private DispatchDocument? selectedDocument;
    [ObservableProperty] private int selectedDocKind = (int)DispatchDocumentKind.DeliveryNote;
    [ObservableProperty] private int pickupHour;
    [ObservableProperty] private int pickupMinute;
    [ObservableProperty] private DateTime? pickupDate = DateTime.Today;
    [ObservableProperty] private string? driverPhone;
    [ObservableProperty] private string? vehiclePlate;
    [ObservableProperty] private string? newPartyName;
    [ObservableProperty] private string? newPartyPhone;
    [ObservableProperty] private string? newPartyAddress;
    [ObservableProperty] private string? newPartyTaxCode;
    [ObservableProperty] private int? newPartyCityId;
    [ObservableProperty] private string createdByLabel = "";
    private bool _suppressAutoFreight;

    public ObservableCollection<DispatchOrder> Items { get; } = [];
    public ObservableCollection<Customer> CustomerOptions { get; } = [];
    public ObservableCollection<Driver> Drivers { get; } = [];
    public ObservableCollection<Vehicle> Vehicles { get; } = [];
    public ObservableCollection<VehicleType> VehicleTypes { get; } = [];
    public ObservableCollection<City> Cities { get; } = [];
    public ObservableCollection<DispatchDocument> DocumentItems { get; } = [];
    public ObservableCollection<DispatchOrderLine> LineItems { get; } = [];
    public IReadOnlyList<int> Hours { get; } = Enumerable.Range(0, 24).ToList();
    public IReadOnlyList<int> Minutes { get; } = Enumerable.Range(0, 60).ToList();
    public IReadOnlyList<NamedInt> StatusOptions { get; } =
    [
        new() { Value = (int)DispatchStatus.Draft, Name = "Nháp" },
        new() { Value = (int)DispatchStatus.Issued, Name = "Đã phát hành" },
        new() { Value = (int)DispatchStatus.Completed, Name = "Hoàn thành" },
        new() { Value = (int)DispatchStatus.Locked, Name = "Đã khóa" }
    ];
    public IReadOnlyList<NamedInt> ReconOptions { get; } =
    [
        new() { Value = (int)ReconciliationStatus.Pending, Name = "Chưa đối soát" },
        new() { Value = (int)ReconciliationStatus.Reconciled, Name = "Đã đối soát" }
    ];
    public IReadOnlyList<NamedInt> DocKindOptions { get; } =
    [
        new() { Value = (int)DispatchDocumentKind.DispatchOrder, Name = "Lệnh điều xe" },
        new() { Value = (int)DispatchDocumentKind.DeliveryNote, Name = "Biên bản giao hàng" },
        new() { Value = (int)DispatchDocumentKind.Invoice, Name = "Hóa đơn / chứng từ" },
        new() { Value = (int)DispatchDocumentKind.Other, Name = "Khác" }
    ];

    public bool IsEditorReadOnly =>
        Editor.Status == DispatchStatus.Locked && current.User?.IsManager != true;

    public bool IsFreightReadOnly =>
        IsEditorReadOnly || Editor.ReconciliationStatus == ReconciliationStatus.Reconciled;

    public bool AreFieldsEnabled => !IsEditorReadOnly;

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

    public int? EditorSenderId
    {
        get => Editor.SenderCustomerId;
        set
        {
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
            Editor.ReceiverCustomerId = value;
            ApplyReceiverSnapshot();
            OnPropertyChanged();
        }
    }

    public int? EditorPickupCityId
    {
        get => Editor.PickupCityId;
        set
        {
            Editor.PickupCityId = value;
            OnPropertyChanged();
            _ = AutoFreightAsync();
        }
    }

    public int? EditorDeliveryCityId
    {
        get => Editor.DeliveryCityId;
        set
        {
            Editor.DeliveryCityId = value;
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
            _ = AutoFreightAsync();
        }
    }

    public string CodeLabel => string.IsNullOrWhiteSpace(Editor.Code) ? "(cấp khi lưu)" : Editor.Code;

    public bool CanLock => current.User?.IsManager == true && CanUpdate;

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.DispatchOrders);
        UsePrompt(prompt);
        OnPropertyChanged(nameof(CanLock));
        await RunAsync(async () =>
        {
            await ReloadLookupsAsync();
            await Search();
        });
    }

    private async Task ReloadLookupsAsync()
    {
        CustomerOptions.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            CustomerOptions.Add(c);
        Drivers.Clear();
        foreach (var d in await catalog.DriversAsync()) Drivers.Add(d);
        Vehicles.Clear();
        foreach (var v in await catalog.VehiclesAsync()) Vehicles.Add(v);
        VehicleTypes.Clear();
        foreach (var v in await catalog.VehicleTypesAsync()) VehicleTypes.Add(v);
        Cities.Clear();
        foreach (var c in await catalog.CitiesAsync()) Cities.Add(c);
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var o in await orders.SearchAsync(
                     FilterCode, FilterFrom, FilterTo, FilterCustomerId, FilterPlate, FilterStatus, FilterRecon,
                     null, null, FilterVehicleTypeId, FilterDeliveryCityId, FilterAmountFrom, FilterAmountTo))
            Items.Add(o);
        Status = $"{Items.Count} lệnh điều xe";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterCode = FilterPlate = null;
        FilterCustomerId = FilterStatus = FilterRecon = FilterVehicleTypeId = FilterDeliveryCityId = null;
        FilterFrom = FilterTo = null;
        FilterAmountFrom = FilterAmountTo = null;
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
        EnterCreate("Thêm lệnh điều xe");
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await OpenAsync(Selected.Id);
    }

    public async Task OpenByIdAsync(int id) => await OpenAsync(id);

    private async Task OpenAsync(int id)
    {
        var full = await orders.GetAsync(id);
        if (full is null) return;
        _suppressAutoFreight = true;
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
        EnterEdit($"Sửa lệnh — {Editor.Code}");
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

    private void NotifyEditorBindings()
    {
        OnPropertyChanged(nameof(EditorSenderId));
        OnPropertyChanged(nameof(EditorReceiverId));
        OnPropertyChanged(nameof(EditorPickupCityId));
        OnPropertyChanged(nameof(EditorDeliveryCityId));
        OnPropertyChanged(nameof(EditorVehicleId));
        OnPropertyChanged(nameof(EditorDriverId));
        OnPropertyChanged(nameof(EditorVehicleTypeId));
        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(CodeLabel));
        NotifyLockChrome();
    }

    private void NotifyLockChrome()
    {
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(IsFreightReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
    }

    private void ApplySenderSnapshot()
    {
        var c = CustomerOptions.FirstOrDefault(x => x.Id == Editor.SenderCustomerId);
        if (c is null) return;
        Editor.SenderName = c.Name;
        Editor.SenderPhone = c.Phone;
        Editor.SenderAddress = c.Address;
        Editor.SenderTaxCode = c.TaxCode;
        if (string.IsNullOrWhiteSpace(Editor.PickupAddress)) Editor.PickupAddress = c.Address;
        if (Editor.PickupCityId is null) Editor.PickupCityId = c.CityId;
        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(EditorPickupCityId));
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
        if (Editor.DeliveryCityId is null) Editor.DeliveryCityId = c.CityId;
        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(EditorDeliveryCityId));
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
    }

    private void ApplyDriverPhone()
    {
        DriverPhone = Drivers.FirstOrDefault(x => x.Id == Editor.DriverId)?.Phone;
    }

    private void CombinePickupAt()
    {
        var date = PickupDate ?? DateTime.Today;
        Editor.PickupAt = date.Date.AddHours(PickupHour).AddMinutes(PickupMinute);
    }

    partial void OnPickupDateChanged(DateTime? value) => CombinePickupAt();
    partial void OnPickupHourChanged(int value) => CombinePickupAt();
    partial void OnPickupMinuteChanged(int value) => CombinePickupAt();

    private async Task AutoFreightAsync()
    {
        if (_suppressAutoFreight) return;
        await orders.ApplyFreightAsync(Editor);
        OnPropertyChanged(nameof(Editor));
    }

    [RelayCommand]
    private async Task CalcFreight()
    {
        CombinePickupAt();
        await orders.ApplyFreightAsync(Editor);
        OnPropertyChanged(nameof(Editor));
    }

    [RelayCommand]
    private void AddLine()
    {
        LineItems.Add(new DispatchOrderLine { LineNumber = LineItems.Count + 1 });
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
        if (Editor.Id == 0 ? !CanCreate : !CanUpdate) return;
        await RunAsync(async () =>
        {
            CombinePickupAt();
            Editor.Lines = LineItems.ToList();
            await orders.SaveAsync(Editor);
            await Search();
        }, "Đã lưu lệnh điều xe.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await orders.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa lệnh.");
    }

    [RelayCommand]
    private async Task Complete()
    {
        if (!CanUpdate || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await orders.SetStatusAsync(Editor.Id, DispatchStatus.Completed);
            await OpenAsync(Editor.Id);
            await Search();
        }, "Đã đánh dấu hoàn thành.");
    }

    [RelayCommand]
    private async Task Lock()
    {
        if (!CanLock || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            await orders.LockAsync(Editor.Id);
            await OpenAsync(Editor.Id);
        }, "Đã khóa lệnh.");
    }

    [RelayCommand]
    private async Task Attach()
    {
        if (Editor.Id == 0)
        {
            ShowToast("Lưu lệnh trước khi đính kèm chứng từ.", isError: true);
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
        if (SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.StoredPath)) return;
        await RunAsync(async () =>
        {
            var path = await documents.ResolvePhysicalPathAsync(SelectedDocument.StoredPath);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        });
    }

    [RelayCommand]
    private async Task Print()
    {
        if (!CanPrint || Editor.Id == 0) return;
        var full = await orders.GetAsync(Editor.Id) ?? Editor;
        var companyInfo = await company.GetAsync();
        var path = printer.PrintDispatch(full, companyInfo);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task PrintSummaryDay()
    {
        if (!CanPrint) return;
        var day = FilterFrom?.Date ?? DateTime.Today;
        var list = await orders.SearchAsync(null, day, day, FilterCustomerId, FilterPlate, FilterStatus, FilterRecon, null, null,
            FilterVehicleTypeId, FilterDeliveryCityId, FilterAmountFrom, FilterAmountTo);
        await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH ĐIỀU XE NGÀY {day:dd/MM/yyyy}");
    }

    [RelayCommand]
    private async Task PrintSummaryMonth()
    {
        if (!CanPrint) return;
        var month = FilterFrom ?? DateTime.Today;
        var from = new DateTime(month.Year, month.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var list = await orders.SearchAsync(null, from, to, FilterCustomerId, FilterPlate, FilterStatus, FilterRecon, null, null,
            FilterVehicleTypeId, FilterDeliveryCityId, FilterAmountFrom, FilterAmountTo);
        await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH ĐIỀU XE THÁNG {month:MM/yyyy}");
    }

    [RelayCommand]
    private async Task PrintSummaryCustomer()
    {
        if (!CanPrint) return;
        if (FilterCustomerId is null) { ShowToast("Chọn khách hàng trên bộ lọc để in theo khách.", isError: true); return; }
        var from = FilterFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = FilterTo ?? DateTime.Today;
        var list = await orders.SearchAsync(null, from, to, FilterCustomerId, FilterPlate, FilterStatus, FilterRecon, null, null,
            FilterVehicleTypeId, FilterDeliveryCityId, FilterAmountFrom, FilterAmountTo);
        var name = CustomerOptions.FirstOrDefault(c => c.Id == FilterCustomerId)?.Name ?? "";
        await OpenSummaryAsync(list, $"TỔNG HỢP LỆNH THEO KHÁCH {name} ({from:dd/MM}–{to:dd/MM/yyyy})");
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!CanPrint) return;
        var path = Path.Combine(Path.GetTempPath(), "DS-CUOC.xlsx");
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
