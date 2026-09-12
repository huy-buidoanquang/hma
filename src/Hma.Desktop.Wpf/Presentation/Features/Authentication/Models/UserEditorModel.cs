using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Authentication;

namespace Hma.Desktop.Wpf.Presentation.Features.Authentication.Models;

public sealed partial class UserEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string userName = "";
    [ObservableProperty] private string? displayName;
    [ObservableProperty] private int? employeeId;
    [ObservableProperty] private bool isManager;
    [ObservableProperty] private bool isSpecial;

    public static UserEditorModel From(UserSummary item) => new()
    {
        Id = item.Id,
        UserName = item.UserName,
        DisplayName = item.DisplayName,
        EmployeeId = item.EmployeeId,
        IsManager = item.IsManager,
        IsSpecial = item.IsSpecial,
    };
}
