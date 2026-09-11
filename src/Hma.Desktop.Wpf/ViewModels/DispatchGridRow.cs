using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DispatchGridRow : ObservableObject
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public DateTime PickupAt { get; init; }
    public string CustomerLabel { get; init; } = "";
    public DispatchStatus Status { get; init; }
    public ReconciliationStatus ReconciliationStatus { get; init; }
    public bool CanEditRow { get; init; }

    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private int? routeId;
    [ObservableProperty] private int? vehicleId;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal extraCost;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private int billingYear;
    [ObservableProperty] private int billingMonth;

    public static DispatchGridRow FromOrder(DispatchOrder order) => new()
    {
        Id = order.Id,
        Code = order.Code,
        PickupAt = order.PickupAt,
        CustomerLabel = order.CustomerCodeName,
        Status = order.Status,
        ReconciliationStatus = order.ReconciliationStatus,
        RouteId = order.RouteId,
        VehicleId = order.VehicleId,
        UnitPrice = order.UnitPrice,
        ExtraCost = order.ExtraCost,
        Notes = order.Notes,
        BillingYear = order.BillingYear == 0 ? order.PickupAt.Year : order.BillingYear,
        BillingMonth = order.BillingMonth == 0 ? order.PickupAt.Month : order.BillingMonth,
        CanEditRow = order.CanEdit
    };
}
