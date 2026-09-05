using System.Globalization;
using ClosedXML.Excel;

namespace Hma.Reporting;

internal static class DispatchExcelCells
{
    public static string Text(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date) && date.Year >= 2000)
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        if (cell.DataType == XLDataType.Number && cell.TryGetValue(out double number)
            && Math.Abs(number - Math.Truncate(number)) < 0.0000001
            && Math.Abs(number) < 1_000_000_000_000d
            && Math.Abs(number) >= 1)
            return ((long)Math.Truncate(number)).ToString(CultureInfo.InvariantCulture);
        return cell.GetFormattedString().Trim();
    }

    public static DateTime? TryDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date) && date.Year >= 2000)
            return date.Date;
        return null;
    }
}
