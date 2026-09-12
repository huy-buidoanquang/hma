using Hma.Application.Features.Dispatching.Import;
using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Hma.Reporting;

internal static class DispatchOpsBoardParser
{
    private static readonly Regex CaptionDate = new(
        @"Ngày\s+(\d{1,2})\s+tháng\s+(\d{1,2})\s+năm\s+(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool LooksLike(XLWorkbook workbook) =>
        workbook.Worksheets.Any(DispatchOpsBoardLayout.HasSttHeader);

    public static IReadOnlyList<DispatchImportRow> Parse(XLWorkbook workbook)
    {
        var day = ReadWorkbookDate(workbook);
        var dayText = day?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var rows = new List<DispatchImportRow>();
        foreach (var ws in workbook.Worksheets.Where(s => s.Visibility == XLWorksheetVisibility.Visible))
        {
            var sheetTonnage = DispatchImportTonnage.TryParse(ws.Name, out _) ? ws.Name : null;
            foreach (var block in DispatchOpsBoardLayout.FindBlocks(ws))
            {
                var emptyStreak = 0;
                var last = ws.LastRowUsed()?.RowNumber() ?? block.HeaderRow;
                for (var r = block.HeaderRow + 1; r <= last; r++)
                {
                    if (block.NextHeaderRow is int next && r >= next)
                        break;
                    if (DispatchOpsBoardLayout.IsSttCell(ws.Cell(r, block.StartColumn)))
                        break;

                    var stt = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn));
                    var driver = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 1));
                    var plate = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 2));
                    var route = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 3));
                    var note = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 4));
                    var money = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 5));
                    var customer = DispatchExcelCells.Text(ws.Cell(r, block.StartColumn + 6));

                    if (AllBlank(stt, driver, plate, route, note, money, customer))
                    {
                        emptyStreak++;
                        if (emptyStreak >= DispatchOpsBoardLayout.EmptyStreakLimit) break;
                        continue;
                    }

                    emptyStreak = 0;
                    if (DispatchImportRoute.IsIdle(route) || DispatchImportRoute.IsDateCaption(route))
                        continue;
                    if (!DispatchOpsBoardLayout.Started(stt, driver, plate, route, customer))
                        continue;
                    if (string.IsNullOrWhiteSpace(route) || string.IsNullOrWhiteSpace(customer))
                        continue;

                    string? pickup = null;
                    string? delivery = null;
                    if (DispatchImportRoute.TrySplit(route, out var from, out var to))
                    {
                        pickup = from;
                        delivery = to;
                    }

                    string? tonnage = sheetTonnage;
                    string? notes = note;
                    if (DispatchImportTonnage.TryParse(note, out _))
                    {
                        tonnage = note;
                        notes = null;
                    }

                    rows.Add(new DispatchImportRow
                    {
                        ExcelRow = r,
                        StartColumn = block.StartColumn,
                        SheetName = ws.Name,
                        BlockLabel = block.Label,
                        Stt = stt,
                        Route = route,
                        PickupAt = dayText,
                        CustomerCode = customer,
                        PickupCity = pickup,
                        DeliveryCity = delivery,
                        Plate = plate,
                        Tonnage = tonnage,
                        Freight = money,
                        Notes = notes,
                        DriverName = driver
                    });
                }
            }
        }

        return rows;
    }

    public static DateTime? ReadWorkbookDate(XLWorkbook workbook)
    {
        foreach (var ws in workbook.Worksheets)
        {
            var lastCol = Math.Min(ws.LastColumnUsed()?.ColumnNumber() ?? 0, 16);
            for (var r = 1; r <= 4; r++)
            {
                for (var c = 1; c <= lastCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    var date = DispatchExcelCells.TryDate(cell);
                    if (date is not null)
                        return date;
                    var text = DispatchExcelCells.Text(cell);
                    var match = CaptionDate.Match(text);
                    if (match.Success
                        && int.TryParse(match.Groups[1].Value, out var d)
                        && int.TryParse(match.Groups[2].Value, out var m)
                        && int.TryParse(match.Groups[3].Value, out var y))
                        return new DateTime(y, m, d);
                }
            }
        }

        return null;
    }

    private static bool AllBlank(params string[] values) =>
        values.All(string.IsNullOrWhiteSpace);
}
