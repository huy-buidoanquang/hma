using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

public sealed partial class VehicleEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string plateNumber = "";
    [ObservableProperty] private int partnerId;
    [ObservableProperty] private int? vehicleTypeId;
    [ObservableProperty] private decimal? tonnage;

    public static VehicleEditorModel From(VehicleSummary item) => new()
    {
        Id = item.Id,
        PlateNumber = item.PlateNumber,
        PartnerId = item.PartnerId,
        VehicleTypeId = item.VehicleTypeId,
        Tonnage = item.Tonnage,
    };

    public SaveVehicleCommand ToCommand() => new(Id, PlateNumber, PartnerId, VehicleTypeId, Tonnage);
}
