using Hma.Application.Features.Dispatching.Import;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public partial class DispatchImportDraft : ObservableObject
{
    public int ExcelRow { get; init; }
    public int StartColumn { get; init; }
    public string? SheetName { get; init; }
    public string? BlockLabel { get; init; }

    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private string? stt;
    [ObservableProperty] private string? route;
    [ObservableProperty] private string? code;
    [ObservableProperty] private string? pickupAt;
    [ObservableProperty] private string? customerCode;
    [ObservableProperty] private string? pickupCity;
    [ObservableProperty] private string? deliveryCity;
    [ObservableProperty] private string? plate;
    [ObservableProperty] private string? tonnage;
    [ObservableProperty] private string? freight;
    [ObservableProperty] private string? extraCost;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private string? paymentMethod;
    [ObservableProperty] private string? driverName;
    [ObservableProperty] private string? errorText;

    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);

    public static DispatchImportDraft FromRow(DispatchImportRow row) => new()
    {
        ExcelRow = row.ExcelRow,
        StartColumn = row.StartColumn,
        SheetName = row.SheetName,
        BlockLabel = row.BlockLabel,
        Stt = row.Stt,
        Route = row.Route,
        Code = row.Code,
        PickupAt = row.PickupAt,
        CustomerCode = row.CustomerCode,
        PickupCity = row.PickupCity,
        DeliveryCity = row.DeliveryCity,
        Plate = row.Plate,
        Tonnage = row.Tonnage,
        Freight = row.Freight,
        ExtraCost = row.ExtraCost,
        Notes = row.Notes,
        PaymentMethod = row.PaymentMethod,
        DriverName = row.DriverName
    };

    public DispatchImportRow ToRow() => new()
    {
        ExcelRow = ExcelRow,
        StartColumn = StartColumn,
        SheetName = SheetName,
        BlockLabel = BlockLabel,
        Stt = Stt,
        Route = Route,
        Code = Code,
        PickupAt = PickupAt,
        CustomerCode = CustomerCode,
        PickupCity = PickupCity,
        DeliveryCity = DeliveryCity,
        Plate = Plate,
        Tonnage = Tonnage,
        Freight = Freight,
        ExtraCost = ExtraCost,
        Notes = Notes,
        PaymentMethod = PaymentMethod,
        DriverName = DriverName
    };

    public void ApplyErrors(IReadOnlyList<string> errors)
    {
        ErrorText = errors.Count == 0 ? null : string.Join(" ", errors);
        OnPropertyChanged(nameof(IsValid));
    }

    partial void OnErrorTextChanged(string? value) => OnPropertyChanged(nameof(IsValid));
}
