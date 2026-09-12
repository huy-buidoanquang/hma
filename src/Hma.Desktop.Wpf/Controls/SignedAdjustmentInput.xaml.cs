using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Hma.Domain.Enums;
using Hma.Domain.Rules;

namespace Hma.Desktop.Wpf.Controls;

public partial class SignedAdjustmentInput : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(decimal),
        typeof(SignedAdjustmentInput),
        new FrameworkPropertyMetadata(
            0m,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged));

    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type),
        typeof(PriceFluctuationType),
        typeof(SignedAdjustmentInput),
        new PropertyMetadata(PriceFluctuationType.Percentage, OnTypeChanged));

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(SignedAdjustmentInput),
        new PropertyMetadata(false, OnIsReadOnlyChanged));

    private bool _isUpdating;
    private bool _isDecrease;
    private bool _signExplicitlySelected;

    public SignedAdjustmentInput()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshFromValue();
        IsEnabledChanged += (_, _) => ApplyReadOnlyState();
    }

    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PriceFluctuationType Type
    {
        get => (PriceFluctuationType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool IsDecreaseSelected => _isDecrease;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SignedAdjustmentInput input || input._isUpdating) return;
        input._isDecrease = (decimal)e.NewValue < 0;
        input._signExplicitlySelected = input._isDecrease;
        input.RefreshFromValue();
    }

    private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignedAdjustmentInput input)
            input.RefreshFromValue();
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignedAdjustmentInput input)
            input.ApplyReadOnlyState();
    }

    private void OnDecrease(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly) return;
        _isDecrease = true;
        _signExplicitlySelected = true;
        ApplySignedValue();
    }

    private void OnIncrease(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly) return;
        _isDecrease = false;
        _signExplicitlySelected = true;
        ApplySignedValue();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        var text = ValueBox.Text.Trim();
        if (text.StartsWith('-'))
        {
            _isDecrease = true;
            _signExplicitlySelected = true;
        }
        else if (text.StartsWith('+'))
        {
            _isDecrease = false;
            _signExplicitlySelected = true;
        }
        else if (!_signExplicitlySelected)
        {
            _isDecrease = false;
        }

        UpdateSignChrome();
        if (!TryParseMagnitude(text, out var magnitude)) return;
        SetSignedValue(magnitude);
    }

    private void OnInputFocus(object sender, RoutedEventArgs e) =>
        Chrome.SetResourceReference(Border.BorderBrushProperty, "BorderFocusBrush");

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        Chrome.SetResourceReference(Border.BorderBrushProperty, "BorderDefaultBrush");
        RefreshText();
    }

    private void ApplySignedValue()
    {
        if (!TryParseMagnitude(ValueBox.Text, out var magnitude))
            magnitude = Math.Abs(Value);
        SetSignedValue(magnitude);
        RefreshText();
        ValueBox.Focus();
        ValueBox.SelectAll();
    }

    private void SetSignedValue(decimal magnitude)
    {
        _isUpdating = true;
        SetCurrentValue(ValueProperty, _isDecrease ? -Math.Abs(magnitude) : Math.Abs(magnitude));
        _isUpdating = false;
        UpdateSignChrome();
    }

    private bool TryParseMagnitude(string text, out decimal magnitude)
    {
        var normalized = text.Trim().TrimStart('+', '-').Replace("%", "", StringComparison.Ordinal).Trim();
        if (Type == PriceFluctuationType.FixedAmount)
        {
            if (MoneyRules.TryParse(normalized, out var amount))
            {
                magnitude = Math.Abs(amount);
                return true;
            }
        }
        else if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var percent)
                 || decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out percent))
        {
            magnitude = Math.Abs(percent);
            return true;
        }

        magnitude = 0;
        return false;
    }

    private void RefreshFromValue()
    {
        if (Value < 0)
            _isDecrease = true;
        else if (!_signExplicitlySelected)
            _isDecrease = false;
        Suffix.Text = Type == PriceFluctuationType.Percentage ? "%" : "đ";
        RefreshText();
        UpdateSignChrome();
        ApplyReadOnlyState();
    }

    private void RefreshText()
    {
        if (ValueBox is null) return;
        _isUpdating = true;
        ValueBox.Text = Type == PriceFluctuationType.Percentage
            ? Math.Abs(Value).ToString("0.####", CultureInfo.GetCultureInfo("vi-VN"))
            : MoneyRules.Format(Math.Abs(Value));
        _isUpdating = false;
    }

    private void UpdateSignChrome()
    {
        if (IncreaseButton is null || DecreaseButton is null) return;
        ApplyButtonChrome(IncreaseButton, !_isDecrease, "SuccessBrush", "SuccessBgBrush");
        ApplyButtonChrome(DecreaseButton, _isDecrease, "ErrorBrush", "ErrorBgBrush");
    }

    private static void ApplyButtonChrome(Button button, bool active, string foreground, string background)
    {
        button.SetResourceReference(ForegroundProperty, active ? foreground : "InkMutedBrush");
        button.SetResourceReference(BackgroundProperty, active ? background : "PanelBrush");
    }

    private void ApplyReadOnlyState()
    {
        if (ValueBox is null) return;
        ValueBox.IsReadOnly = IsReadOnly;
        IncreaseButton.IsEnabled = IsEnabled && !IsReadOnly;
        DecreaseButton.IsEnabled = IsEnabled && !IsReadOnly;
        Chrome.SetResourceReference(Border.BackgroundProperty, IsEnabled && !IsReadOnly ? "PanelBrush" : "MutedBrush");
    }
}
