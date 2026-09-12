using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Pricing;
using Hma.Domain.Enums;

namespace Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;

public sealed partial class PriceListFluctuationEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int priceListId;
    [ObservableProperty] private PriceFluctuationType type = PriceFluctuationType.Percentage;
    [ObservableProperty] private decimal value;
    [ObservableProperty] private DateTime effectiveFrom = DateTime.Today;
    [ObservableProperty] private DateTime? effectiveTo;
    [ObservableProperty] private string reason = "";
    [ObservableProperty] private byte[] versionToken = [];

    public static PriceListFluctuationEditorModel Create(int priceListId) => new()
    {
        PriceListId = priceListId,
        EffectiveFrom = DateTime.Today,
    };

    public static PriceListFluctuationEditorModel From(PriceListFluctuationSummary item) => new()
    {
        Id = item.Id,
        PriceListId = item.PriceListId,
        Type = item.Type,
        Value = item.Value,
        EffectiveFrom = item.EffectiveFrom,
        EffectiveTo = item.EffectiveTo,
        Reason = item.Reason,
        VersionToken = item.VersionToken.ToArray(),
    };

    public SavePriceListFluctuationCommand ToCommand() => new(
        Id,
        PriceListId,
        Type,
        Value,
        EffectiveFrom,
        EffectiveTo,
        Reason,
        VersionToken.ToArray());
}
