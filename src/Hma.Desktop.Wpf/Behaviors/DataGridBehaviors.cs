using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Hma.Desktop.Wpf.Behaviors;

public static class DataGridBehaviors
{
    public static readonly DependencyProperty ApplyTextCellLayoutProperty =
        DependencyProperty.RegisterAttached(
            "ApplyTextCellLayout",
            typeof(bool),
            typeof(DataGridBehaviors),
            new PropertyMetadata(false, OnApplyTextCellLayoutChanged));

    public static void SetApplyTextCellLayout(DependencyObject element, bool value) =>
        element.SetValue(ApplyTextCellLayoutProperty, value);

    public static bool GetApplyTextCellLayout(DependencyObject element) =>
        (bool)element.GetValue(ApplyTextCellLayoutProperty);

    private static void OnApplyTextCellLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;
        grid.Loaded -= OnGridReadyForTextCells;
        if ((bool)e.NewValue)
        {
            grid.Loaded += OnGridReadyForTextCells;
            if (grid.IsLoaded)
                ApplyTextCellLayout(grid);
        }
    }

    private static void OnGridReadyForTextCells(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
            ApplyTextCellLayout(grid);
    }

    private static void ApplyTextCellLayout(DataGrid grid)
    {
        if (grid.TryFindResource("DataGridTextCell") is not Style style)
            return;

        foreach (var column in grid.Columns)
        {
            if (column is not DataGridTextColumn text)
                continue;
            if (text.ElementStyle is null || ReferenceEquals(text.ElementStyle, DataGridTextColumn.DefaultElementStyle))
                text.ElementStyle = style;
        }
    }

    public static readonly DependencyProperty SingleClickCheckBoxProperty =
        DependencyProperty.RegisterAttached(
            "SingleClickCheckBox",
            typeof(bool),
            typeof(DataGridBehaviors),
            new PropertyMetadata(false, OnSingleClickChanged));

    public static void SetSingleClickCheckBox(DependencyObject element, bool value) =>
        element.SetValue(SingleClickCheckBoxProperty, value);

    public static bool GetSingleClickCheckBox(DependencyObject element) =>
        (bool)element.GetValue(SingleClickCheckBoxProperty);

    private static void OnSingleClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;
        grid.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        if ((bool)e.NewValue)
            grid.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject origin)
            return;

        var cell = FindParent<DataGridCell>(origin);
        if (cell is null) return;

        if (origin is CheckBox || FindParent<CheckBox>(origin) is not null)
            return;

        if (cell.Column is not DataGridCheckBoxColumn || cell.IsReadOnly)
            return;

        grid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
        if (!cell.IsEditing)
            grid.BeginEdit();

        var box = FindVisualChild<CheckBox>(cell);
        if (box is null) return;
        box.IsChecked = box.IsChecked != true;
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }

        return null;
    }
}
