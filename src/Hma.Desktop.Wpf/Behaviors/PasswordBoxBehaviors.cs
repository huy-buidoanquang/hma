using System.Windows;
using System.Windows.Controls;

namespace Hma.Desktop.Wpf.Behaviors;

public static class PasswordBoxBehaviors
{
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxBehaviors),
            new PropertyMetadata(false, OnBindPasswordChanged));

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBehaviors),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBehaviors));

    public static void SetPassword(DependencyObject element, string? value) =>
        element.SetValue(PasswordProperty, value ?? "");

    public static string GetPassword(DependencyObject element) =>
        (string)element.GetValue(PasswordProperty);

    public static void SetBindPassword(DependencyObject element, bool value) =>
        element.SetValue(BindPasswordProperty, value);

    public static bool GetBindPassword(DependencyObject element) =>
        (bool)element.GetValue(BindPasswordProperty);

    private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            return;

        passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;
        if (e.NewValue is true)
            passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox || (bool)passwordBox.GetValue(IsUpdatingProperty))
            return;

        passwordBox.Password = e.NewValue as string ?? "";
    }

    private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        passwordBox.SetValue(IsUpdatingProperty, true);
        passwordBox.SetCurrentValue(PasswordProperty, passwordBox.Password);
        passwordBox.SetValue(IsUpdatingProperty, false);
    }
}
