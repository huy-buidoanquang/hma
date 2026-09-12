using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Pricing;

namespace Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;

public sealed partial class PartnerRateEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int partnerId;
    [ObservableProperty] private int routeId;
    [ObservableProperty] private int vehicleTypeId;
    [ObservableProperty] private DateTime effectiveFrom = DateTime.Today;
    [ObservableProperty] private DateTime? effectiveTo;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal surcharge;

    public static PartnerRateEditorModel From(PartnerRateSummary item) => new()
    {
        Id = item.Id,
        PartnerId = item.PartnerId,
        RouteId = item.RouteId,
        VehicleTypeId = item.VehicleTypeId,
        EffectiveFrom = item.EffectiveFrom,
        EffectiveTo = item.EffectiveTo,
        UnitPrice = item.UnitPrice,
        Surcharge = item.Surcharge,
    };
}
