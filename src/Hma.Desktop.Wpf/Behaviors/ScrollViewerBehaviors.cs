using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Hma.Desktop.Wpf.Behaviors;

public static class ScrollViewerBehaviors
{
    public static readonly DependencyProperty FormScrollProperty =
        DependencyProperty.RegisterAttached(
            "FormScroll",
            typeof(bool),
            typeof(ScrollViewerBehaviors),
            new PropertyMetadata(false, OnFormScrollChanged));

    public static void SetFormScroll(DependencyObject element, bool value) =>
        element.SetValue(FormScrollProperty, value);

    public static bool GetFormScroll(DependencyObject element) =>
        (bool)element.GetValue(FormScrollProperty);

    private static void OnFormScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer) return;
        viewer.Loaded -= OnLoaded;
        viewer.SizeChanged -= OnSizeChanged;
        viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        if ((bool)e.NewValue)
        {
            viewer.Loaded += OnLoaded;
            viewer.SizeChanged += OnSizeChanged;
            viewer.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e) => ConstrainWidth(sender as ScrollViewer);

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => ConstrainWidth(sender as ScrollViewer);

    private static void ConstrainWidth(ScrollViewer? viewer)
    {
        if (viewer?.Content is not FrameworkElement content) return;
        var width = viewer.ViewportWidth;
        if (width <= 0) width = viewer.ActualWidth;
        if (width > 0)
            content.MaxWidth = width;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.Delta == 0) return;
        if (ShouldDeferWheel(e.OriginalSource as DependencyObject, viewer)
            || ShouldDeferWheel(Mouse.DirectlyOver as DependencyObject, viewer))
            return;
        var next = viewer.VerticalOffset - Math.Sign(e.Delta) * 48;
        next = Math.Clamp(next, 0, viewer.ScrollableHeight);
        viewer.ScrollToVerticalOffset(next);
        e.Handled = true;
    }

    private static bool ShouldDeferWheel(DependencyObject? origin, ScrollViewer outer)
    {
        for (var current = origin; current is not null; current = GetParent(current))
        {
            if (ReferenceEquals(current, outer))
                break;
            if (current is ComboBox { IsDropDownOpen: true })
                return true;
            if (current is Popup { IsOpen: true })
                return true;
            if (current is ScrollViewer inner
                && !ReferenceEquals(inner, outer)
                && inner.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                return true;
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        if (current is Visual or Visual3D)
        {
            var visual = VisualTreeHelper.GetParent(current);
            if (visual is not null) return visual;
        }

        return LogicalTreeHelper.GetParent(current);
    }
}
