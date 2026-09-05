using System.Windows;
using System.Windows.Controls;
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

    public static readonly DependencyProperty EnterCommandProperty =
        DependencyProperty.RegisterAttached(
            "EnterCommand",
            typeof(ICommand),
            typeof(CommandBehaviors),
            new PropertyMetadata(null, OnEnterCommandChanged));

    public static void SetEnterCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(EnterCommandProperty, value);

    public static ICommand? GetEnterCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(EnterCommandProperty);

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

    private static void OnEnterCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el) return;
        el.PreviewKeyDown -= OnPreviewKeyDown;
        if (e.NewValue is ICommand)
            el.PreviewKeyDown += OnPreviewKeyDown;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DependencyObject d) return;
        if (sender is TextBox textBox)
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        var command = GetEnterCommand(d);
        if (command?.CanExecute(null) != true) return;
        command.Execute(null);
        e.Handled = true;
    }
}
