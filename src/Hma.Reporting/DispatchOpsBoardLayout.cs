using ClosedXML.Excel;

namespace Hma.Reporting;

internal static class DispatchOpsBoardLayout
{
    public const int DataColumns = 7;
    public const int HeaderScanRows = 80;
    public const int HeaderScanColumns = 32;
    public const int EmptyStreakLimit = 15;

    public static readonly string[] SheetNames =
        ["1.25", "1.5", "2.5", "3.5 - 5", "8", "10"];

    public static readonly string[] Headers =
        ["STT", "Tên lái xe", "Biển số", "Tuyến đường", "Ghi chú", "Thành tiền", "Khách hàng"];

    public static bool IsStt(string? text) =>
        (text ?? "").Equals("STT", StringComparison.OrdinalIgnoreCase);

    public static bool IsErrorHeader(string? text) =>
        (text ?? "").Equals(DispatchImportParser.ErrorHeader, StringComparison.OrdinalIgnoreCase);

    public static bool IsSttCell(IXLCell cell) =>
        IsStt(DispatchExcelCells.Text(cell));

    public static bool IsErrorHeaderCell(IXLCell cell) =>
        IsErrorHeader(DispatchExcelCells.Text(cell));

    public static int ErrorColumn(int startColumn) => startColumn + DataColumns;

    public static List<DispatchOpsBoardBlock> FindBlocks(IXLWorksheet ws)
    {
        var found = new List<(int Row, int Col)>();
        var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 0, HeaderScanRows);
        var lastCol = Math.Min(ws.LastColumnUsed()?.ColumnNumber() ?? 0, HeaderScanColumns);
        for (var r = 1; r <= lastRow; r++)
        {
            for (var c = 1; c <= lastCol; c++)
            {
                if (IsSttCell(ws.Cell(r, c)))
                    found.Add((r, c));
            }
        }

        var blocks = new List<DispatchOpsBoardBlock>();
        foreach (var group in found.GroupBy(x => x.Row))
        {
            var cols = group.Select(x => x.Col).Distinct().OrderBy(x => x).ToList();
            for (var i = 0; i < cols.Count; i++)
            {
                var headerRow = group.Key;
                var nextSameCol = found
                    .Where(x => x.Col == cols[i] && x.Row > headerRow)
                    .Select(x => (int?)x.Row)
                    .Min();
                blocks.Add(new DispatchOpsBoardBlock(
                    headerRow,
                    cols[i],
                    cols.Count == 1 ? "ca" : (i == 0 ? "1" : "2"),
                    nextSameCol));
            }
        }

        return blocks;
    }

    public static bool HasSttHeader(IXLWorksheet ws) =>
        FindBlocks(ws).Count > 0;

    public static bool Started(string stt, string driver, string plate, string route, string customer) =>
        !string.IsNullOrWhiteSpace(stt)
        || !string.IsNullOrWhiteSpace(driver)
        || !string.IsNullOrWhiteSpace(plate)
        || !string.IsNullOrWhiteSpace(route)
        || !string.IsNullOrWhiteSpace(customer);
}
