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
    public bool IsLocked => Status == DispatchStatus.Locked;
    public bool CanEditRow => !IsLocked;

    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private int? routeId;
    [ObservableProperty] private int? vehicleId;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal extraCost;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private int billingYear;
    [ObservableProperty] private int billingMonth;
}
