using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public partial class DispatchImportSheetModel : ObservableObject
{
    [ObservableProperty] private bool? allSelected;
    [ObservableProperty] private DispatchImportDraft? selected;

    private bool _suppressAllSelected;

    public DispatchImportSheetModel(string title) => Title = title;

    public string Title { get; }

    public ObservableCollection<DispatchImportDraft> Items { get; } = [];

    public void Replace(IEnumerable<DispatchImportDraft> rows)
    {
        Clear();
        foreach (var row in rows)
        {
            row.PropertyChanged += OnRowPropertyChanged;
            Items.Add(row);
        }

        RefreshAllSelected();
    }

    public void Clear()
    {
        foreach (var row in Items)
            row.PropertyChanged -= OnRowPropertyChanged;
        Items.Clear();
        Selected = null;
        AllSelected = false;
    }

    public void RefreshAllSelected()
    {
        _suppressAllSelected = true;
        if (Items.Count == 0 || Items.All(r => !r.IsSelected))
            AllSelected = false;
        else if (Items.All(r => r.IsSelected))
            AllSelected = true;
        else
            AllSelected = null;
        _suppressAllSelected = false;
    }

    partial void OnAllSelectedChanged(bool? value)
    {
        if (_suppressAllSelected) return;
        var select = value == true;
        foreach (var row in Items)
            row.IsSelected = select;
        if (value is null)
            RefreshAllSelected();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DispatchImportDraft.IsSelected))
            RefreshAllSelected();
    }
}
