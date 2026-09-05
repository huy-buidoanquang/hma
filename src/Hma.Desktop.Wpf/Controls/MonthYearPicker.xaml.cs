using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hma.Desktop.Wpf.Controls;

public partial class MonthYearPicker : UserControl
{
    public static readonly DependencyProperty MonthProperty =
        DependencyProperty.Register(
            nameof(Month),
            typeof(int),
            typeof(MonthYearPicker),
            new FrameworkPropertyMetadata(
                DateTime.Today.Month,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPeriodChanged));

    public static readonly DependencyProperty YearProperty =
        DependencyProperty.Register(
            nameof(Year),
            typeof(int),
            typeof(MonthYearPicker),
            new FrameworkPropertyMetadata(
                DateTime.Today.Year,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPeriodChanged));

    private bool _suppressMode;

    public MonthYearPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshDisplay();
        IsEnabledChanged += (_, _) => ApplyEnabledChrome();
        DropDown.Opened += OnDropDownOpened;
    }

    public int Month
    {
        get => (int)GetValue(MonthProperty);
        set => SetValue(MonthProperty, value);
    }

    public int Year
    {
        get => (int)GetValue(YearProperty);
        set => SetValue(YearProperty, value);
    }

    private static void OnPeriodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MonthYearPicker picker) picker.RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (Caption is null) return;
        var month = Month is >= 1 and <= 12 ? Month : DateTime.Today.Month;
        var year = Year is >= 1900 and <= 2100 ? Year : DateTime.Today.Year;
        Caption.Text = $"{month:00}/{year}";
        ApplyEnabledChrome();
    }

    private void ApplyEnabledChrome()
    {
        if (Chrome is null) return;
        Chrome.Background = TryFindResource(IsEnabled ? "PanelBrush" : "MutedBrush") as Brush;
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;
        if (DropDown.IsOpen)
        {
            DropDown.IsOpen = false;
            return;
        }

        _suppressMode = true;
        Cal.DisplayDate = new DateTime(ClampedYear(), ClampedMonth(), 1);
        Cal.DisplayMode = CalendarMode.Month;
        _suppressMode = false;
        DropDown.IsOpen = true;
    }

    private void OnDropDownOpened(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!DropDown.IsOpen) return;
            _suppressMode = true;
            Cal.DisplayDate = new DateTime(ClampedYear(), ClampedMonth(), 1);
            Cal.DisplayMode = CalendarMode.Year;
            Cal.InvalidateMeasure();
            Cal.UpdateLayout();
            _suppressMode = false;
        }, DispatcherPriority.Loaded);
    }

    private void OnDisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
    {
        if (_suppressMode || sender is not Calendar calendar) return;
        if (calendar.DisplayMode != CalendarMode.Month) return;

        var picked = calendar.DisplayDate;
        SetCurrentValue(YearProperty, picked.Year);
        SetCurrentValue(MonthProperty, picked.Month);
        _suppressMode = true;
        calendar.DisplayMode = CalendarMode.Year;
        _suppressMode = false;
        DropDown.IsOpen = false;
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (Chrome is not null)
            Chrome.BorderBrush = TryFindResource("BorderFocusBrush") as Brush;
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (Chrome is not null)
            Chrome.BorderBrush = TryFindResource("BorderDefaultBrush") as Brush;
    }

    private int ClampedMonth() => Month is >= 1 and <= 12 ? Month : DateTime.Today.Month;

    private int ClampedYear() => Year is >= 1900 and <= 2100 ? Year : DateTime.Today.Year;
}
