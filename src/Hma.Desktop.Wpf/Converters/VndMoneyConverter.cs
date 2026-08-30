using System.Globalization;
using System.Windows.Data;
using Hma.Domain.Services;

namespace Hma.Desktop.Wpf.Converters;

public sealed class VndMoneyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value is string s && string.IsNullOrWhiteSpace(s))
            return "";
        if (value is decimal amount)
            return MoneyRules.Format(amount);
        if (value is double d)
            return MoneyRules.Format((decimal)d);
        if (value is int i)
            return MoneyRules.Format(i);
        if (decimal.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return MoneyRules.Format(parsed);
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (targetType == typeof(decimal?) || Nullable.GetUnderlyingType(targetType) == typeof(decimal))
                return null;
            return 0m;
        }

        if (!MoneyRules.TryParse(text, out var amount))
            return Binding.DoNothing;

        if (targetType == typeof(decimal?) || Nullable.GetUnderlyingType(targetType) == typeof(decimal))
            return (decimal?)amount;
        return amount;
    }
}
