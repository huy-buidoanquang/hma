using System.Globalization;
using System.Windows.Data;
using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Desktop.Wpf.Converters;

public sealed class LocationAliasKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LocationAliasKind kind ? LocationAliasKindLabels.For(kind) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
