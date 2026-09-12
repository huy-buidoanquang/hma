using Hma.Application.Features.Dispatching.Import;
using ClosedXML.Excel;

namespace Hma.Reporting;

internal static class DispatchOpsBoardCheckWriter
{
    public static void Write(XLWorkbook workbook, IReadOnlyList<DispatchImportCheck> rows, string? fileError)
    {
        foreach (var ws in workbook.Worksheets.Where(s => s.Visibility == XLWorksheetVisibility.Visible))
            EnsureErrorColumns(ws);

        if (!string.IsNullOrWhiteSpace(fileError) && rows.Count == 0)
        {
            WriteFileError(workbook, fileError);
            return;
        }

        var bySheet = rows.ToLookup(c => c.Row.SheetName ?? "");
        foreach (var ws in workbook.Worksheets.Where(s => s.Visibility == XLWorksheetVisibility.Visible))
        {
            var blocks = DispatchOpsBoardLayout.FindBlocks(ws);
            foreach (var item in bySheet[ws.Name])
            {
                var block = MatchBlock(blocks, item.Row);
                if (block is null) continue;
                var cell = ws.Cell(item.Row.ExcelRow, DispatchOpsBoardLayout.ErrorColumn(block.StartColumn));
                if (item.Errors.Count == 0)
                {
                    cell.Value = "";
                    cell.Style.Font.FontColor = XLColor.Black;
                    continue;
                }

                cell.Value = string.Join(" ", item.Errors);
                cell.Style.Font.FontColor = XLColor.DarkRed;
            }
        }
    }

    private static void EnsureErrorColumns(IXLWorksheet ws)
    {
        var starts = DispatchOpsBoardLayout.FindBlocks(ws)
            .Select(b => b.StartColumn)
            .Distinct()
            .OrderByDescending(c => c)
            .ToList();
        foreach (var start in starts)
        {
            var headerRows = DispatchOpsBoardLayout.FindBlocks(ws)
                .Where(b => b.StartColumn == start)
                .Select(b => b.HeaderRow)
                .Distinct()
                .ToList();
            if (headerRows.Count == 0) continue;
            var sampleRow = headerRows[0];
            if (DispatchOpsBoardLayout.IsErrorHeaderCell(ws.Cell(sampleRow, DispatchOpsBoardLayout.ErrorColumn(start))))
                continue;
            ws.Column(DispatchOpsBoardLayout.ErrorColumn(start)).InsertColumnsBefore(1);
            foreach (var headerRow in headerRows)
            {
                var cell = ws.Cell(headerRow, DispatchOpsBoardLayout.ErrorColumn(start));
                cell.Value = DispatchImportParser.ErrorHeader;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        foreach (var start in DispatchOpsBoardLayout.FindBlocks(ws).Select(b => b.StartColumn).Distinct())
            ws.Column(DispatchOpsBoardLayout.ErrorColumn(start)).Width = 48;
    }

    private static void WriteFileError(XLWorkbook workbook, string fileError)
    {
        var ws = workbook.Worksheets.FirstOrDefault(s => s.Visibility == XLWorksheetVisibility.Visible)
                 ?? workbook.Worksheets.First();
        var block = DispatchOpsBoardLayout.FindBlocks(ws).FirstOrDefault();
        var row = block?.HeaderRow + 1 ?? 4;
        var col = block is null ? 1 : DispatchOpsBoardLayout.ErrorColumn(block.StartColumn);
        var cell = ws.Cell(row, col);
        cell.Value = fileError;
        cell.Style.Font.FontColor = XLColor.DarkRed;
    }

    private static DispatchOpsBoardBlock? MatchBlock(IReadOnlyList<DispatchOpsBoardBlock> blocks, DispatchImportRow row)
    {
        return blocks.FirstOrDefault(b =>
            b.Label == (row.BlockLabel ?? b.Label)
            && row.ExcelRow > b.HeaderRow
            && (b.NextHeaderRow is null || row.ExcelRow < b.NextHeaderRow));
    }
}
