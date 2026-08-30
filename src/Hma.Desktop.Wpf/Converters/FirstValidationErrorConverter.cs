using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Hma.Desktop.Wpf.Converters;

public sealed class FirstValidationErrorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is ValidationError error)
                    return error.ErrorContent;
            }
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
