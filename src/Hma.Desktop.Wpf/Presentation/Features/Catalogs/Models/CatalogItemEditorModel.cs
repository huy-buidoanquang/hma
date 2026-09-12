using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

public sealed partial class CatalogItemEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? description;

    public static CatalogItemEditorModel From(CatalogItemSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
    };

    public SaveCatalogItemCommand ToCommand() => new(Id, Code, Name, Description);
}
