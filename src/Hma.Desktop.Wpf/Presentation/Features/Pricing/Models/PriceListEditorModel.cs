using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Pricing;

namespace Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;

public sealed partial class PriceListEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? description;
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private DateTime? effectiveFrom = DateTime.Today;
    [ObservableProperty] private DateTime? effectiveTo;
    [ObservableProperty] private bool hasPriceFluctuation;
    [ObservableProperty] private bool isLocked;
    [ObservableProperty] private string? lockReason;

    public static PriceListEditorModel From(PriceListSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        CustomerId = item.CustomerId,
        EffectiveFrom = item.EffectiveFrom,
        EffectiveTo = item.EffectiveTo,
        HasPriceFluctuation = item.HasPriceFluctuation,
        IsLocked = item.IsLocked,
        LockReason = item.LockReason,
    };

    public SavePriceListCommand ToCommand(byte[] versionToken) => new(
        Id, Code, Name, Description, CustomerId, EffectiveFrom, EffectiveTo,
        HasPriceFluctuation, LockReason, versionToken.ToArray());
}
