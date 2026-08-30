using System.Windows;
using System.Windows.Input;

namespace Hma.Desktop.Wpf.Behaviors;

public static class CommandBehaviors
{
    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(CommandBehaviors),
            new PropertyMetadata(null, OnDoubleClickChanged));

    public static void SetDoubleClickCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(DoubleClickCommandProperty, value);

    public static ICommand? GetDoubleClickCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(DoubleClickCommandProperty);

    private static void OnDoubleClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el) return;
        el.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
        if (e.NewValue is ICommand)
            el.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2 || sender is not DependencyObject d) return;
        var command = GetDoubleClickCommand(d);
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
