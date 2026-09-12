using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Customers;

namespace Hma.Desktop.Wpf.Presentation.Features.Customers.Models;

public sealed partial class CustomerAliasEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string alias = "";
    [ObservableProperty] private int customerId;

    public static CustomerAliasEditorModel From(CustomerAliasSummary item) => new()
    {
        Id = item.Id,
        Alias = item.Alias,
        CustomerId = item.CustomerId,
    };

    public SaveCustomerAliasCommand ToCommand() => new(Id, Alias, CustomerId);
}
