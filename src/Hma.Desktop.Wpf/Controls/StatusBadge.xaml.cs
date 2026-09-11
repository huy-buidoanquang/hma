using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Controls;

public partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty OrderStatusProperty =
        DependencyProperty.Register(
            nameof(OrderStatus),
            typeof(object),
            typeof(StatusBadge),
            new PropertyMetadata(null, OnKindChanged));

    public static readonly DependencyProperty ReconStatusProperty =
        DependencyProperty.Register(
            nameof(ReconStatus),
            typeof(object),
            typeof(StatusBadge),
            new PropertyMetadata(null, OnKindChanged));

    public StatusBadge()
    {
        InitializeComponent();
        Loaded += (_, _) => Apply();
    }

    public object? OrderStatus
    {
        get => GetValue(OrderStatusProperty);
        set => SetValue(OrderStatusProperty, value);
    }

    public object? ReconStatus
    {
        get => GetValue(ReconStatusProperty);
        set => SetValue(ReconStatusProperty, value);
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge) badge.Apply();
    }

    private void Apply()
    {
        if (Chrome is null || Caption is null) return;

        if (OrderStatus is DispatchStatus order)
        {
            Visibility = Visibility.Visible;
            (Caption.Text, Chrome.Background, Caption.Foreground) = order switch
            {
                DispatchStatus.Draft => ("Nháp", ThemeBrush("MutedBrush"), ThemeBrush("InkMutedBrush")),
                DispatchStatus.Issued => ("Đã phát hành", ThemeBrush("AccentHoverBrush"), ThemeBrush("AccentDeepBrush")),
                DispatchStatus.Completed => ("Hoàn thành", ThemeBrush("SuccessBgBrush"), ThemeBrush("SuccessBrush")),
                DispatchStatus.Locked => ("Đã khóa", ThemeBrush("ErrorBgBrush"), ThemeBrush("ErrorBrush")),
                DispatchStatus.Cancelled => ("Đã hủy", ThemeBrush("ErrorBgBrush"), ThemeBrush("ErrorBrush")),
                _ => ("", ThemeBrush("MutedBrush"), ThemeBrush("InkMutedBrush"))
            };
            return;
        }

        if (ReconStatus is ReconciliationStatus recon)
        {
            Visibility = Visibility.Visible;
            (Caption.Text, Chrome.Background, Caption.Foreground) = recon switch
            {
                ReconciliationStatus.Pending => ("Chưa đối soát", ThemeBrush("WarningBgBrush"), ThemeBrush("WarningBrush")),
                ReconciliationStatus.Reconciled => ("Đã đối soát", ThemeBrush("SuccessBgBrush"), ThemeBrush("SuccessBrush")),
                ReconciliationStatus.Submitted => ("Chờ duyệt", ThemeBrush("AccentHoverBrush"), ThemeBrush("AccentDeepBrush")),
                ReconciliationStatus.Rejected => ("Bị từ chối", ThemeBrush("ErrorBgBrush"), ThemeBrush("ErrorBrush")),
                _ => ("", ThemeBrush("MutedBrush"), ThemeBrush("InkMutedBrush"))
            };
            return;
        }

        Visibility = Visibility.Collapsed;
    }

    private Brush ThemeBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Transparent;
}
