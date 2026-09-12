using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

public sealed partial class EmployeeEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? mobile;
    [ObservableProperty] private DateTime? birthDate;
    [ObservableProperty] private string? identityNumber;
    [ObservableProperty] private string? vehiclePlate;
    [ObservableProperty] private int? departmentId;
    [ObservableProperty] private int? jobTitleId;

    public static EmployeeEditorModel From(EmployeeSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Address = item.Address,
        Phone = item.Phone,
        Mobile = item.Mobile,
        BirthDate = item.BirthDate,
        IdentityNumber = item.IdentityNumber,
        VehiclePlate = item.VehiclePlate,
        DepartmentId = item.DepartmentId,
        JobTitleId = item.JobTitleId,
    };

    public SaveEmployeeCommand ToCommand() => new(
        Id, Code, Name, Address, Phone, Mobile, BirthDate, IdentityNumber, VehiclePlate, DepartmentId, JobTitleId);
}
