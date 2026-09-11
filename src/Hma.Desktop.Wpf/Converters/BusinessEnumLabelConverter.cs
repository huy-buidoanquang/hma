using System.Globalization;
using System.Windows.Data;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Converters;

public sealed class BusinessEnumLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FinancialDocumentStatus.Draft => "Nháp",
        FinancialDocumentStatus.Submitted => "Chờ duyệt",
        FinancialDocumentStatus.Finalized => "Đã chốt",
        FinancialDocumentStatus.Voided => "Đã hủy",
        DispatchDocumentKind.DispatchOrder => "Lệnh điều xe",
        DispatchDocumentKind.DeliveryNote => "Biên bản giao hàng",
        DispatchDocumentKind.Invoice => "Hóa đơn / chứng từ",
        DispatchDocumentKind.Other => "Khác",
        null => "",
        _ => value.ToString() ?? ""
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
