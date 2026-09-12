using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hma.Desktop.Wpf.Behaviors;

public static class AutoCompleteComboBoxBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AutoCompleteComboBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox) return;
        comboBox.PreviewTextInput -= OnPreviewTextInput;
        comboBox.PreviewKeyDown -= OnPreviewKeyDown;
        if ((bool)e.NewValue)
        {
            comboBox.PreviewTextInput += OnPreviewTextInput;
            comboBox.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e) =>
        Open(sender as ComboBox);

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Back or Key.Delete)
            Open(sender as ComboBox);
    }

    private static void Open(ComboBox? comboBox)
    {
        if (comboBox is { IsEditable: true, IsEnabled: true })
            comboBox.IsDropDownOpen = true;
    }
}
