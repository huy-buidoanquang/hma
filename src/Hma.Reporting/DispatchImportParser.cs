using Hma.Application.Abstractions.Import;
using Hma.Application.Features.Dispatching.Import;
using ClosedXML.Excel;

namespace Hma.Reporting;

public sealed class DispatchImportParser : IDispatchImportParser
{
    public const string ErrorHeader = "Lỗi";

    public static readonly string[] Headers =
    [
        "Số lệnh", "Ngày chạy", "Mã khách hàng", "Điểm đi", "Điểm đến",
        "Biển kiểm soát", "Trọng tải", "Cước", "Phát sinh", "Ghi chú",
        "Hình thức TT", "Tài xế"
    ];

    public static IReadOnlyList<string> OpsBoardSheetNames => DispatchOpsBoardLayout.SheetNames;

    public IReadOnlyList<DispatchImportRow> Parse(Stream stream)
    {
        using var wb = DispatchExcelWorkbook.Open(stream);
        if (LooksLikeTemplate(wb.Worksheets.First()))
            return ParseTemplate(wb.Worksheets.First());
        if (DispatchOpsBoardParser.LooksLike(wb))
            return DispatchOpsBoardParser.Parse(wb);
        return ParseTemplate(wb.Worksheets.First());
    }

    public void WriteTemplate(Stream stream)
    {
        using var wb = new XLWorkbook();
        var today = DateTime.Today;
        var caption = $"Ngày {today.Day:00} tháng {today.Month:00} năm {today.Year}";
        var weekday = today.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)today.DayOfWeek + 1;
        var first = true;
        foreach (var name in DispatchOpsBoardLayout.SheetNames)
        {
            var ws = wb.AddWorksheet(name);
            if (first)
            {
                ws.Cell(1, 1).Value = today;
                ws.Cell(1, 1).Style.DateFormat.Format = "dd/mm/yyyy";
                first = false;
            }

            WriteShiftCaption(ws, 1, weekday, caption);
            WriteShiftCaption(ws, 1 + DispatchOpsBoardLayout.DataColumns, weekday, caption);
            WriteShiftHeaders(ws, 1);
            WriteShiftHeaders(ws, 1 + DispatchOpsBoardLayout.DataColumns);
            ws.SheetView.FreezeRows(3);
            ws.Columns(1, 14).AdjustToContents();
        }

        wb.SaveAs(stream);
    }

    public void WriteCheck(Stream sourceWorkbook, IReadOnlyList<DispatchImportCheck> rows, string? fileError, Stream output)
    {
        using var wb = DispatchExcelWorkbook.Open(sourceWorkbook);
        if (LooksLikeTemplate(wb.Worksheets.First()))
        {
            WriteFlatCheck(rows, fileError, output);
            return;
        }

        if (DispatchOpsBoardParser.LooksLike(wb))
        {
            DispatchOpsBoardCheckWriter.Write(wb, rows, fileError);
            wb.SaveAs(output);
            return;
        }

        WriteFlatCheck(rows, fileError, output);
    }

    private static void WriteShiftCaption(IXLWorksheet ws, int startColumn, int weekday, string caption)
    {
        ws.Cell(2, startColumn).Value = "Thứ";
        ws.Cell(2, startColumn + 1).Value = weekday;
        ws.Cell(2, startColumn + 3).Value = caption;
    }

    private static void WriteShiftHeaders(IXLWorksheet ws, int startColumn)
    {
        for (var i = 0; i < DispatchOpsBoardLayout.Headers.Length; i++)
        {
            var cell = ws.Cell(3, startColumn + i);
            cell.Value = DispatchOpsBoardLayout.Headers[i];
            cell.Style.Font.Bold = true;
        }
    }

    private static void WriteFlatCheck(IReadOnlyList<DispatchImportCheck> rows, string? fileError, Stream stream)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("KiemTra");
        for (var i = 0; i < Headers.Length; i++)
            ws.Cell(1, i + 1).Value = Headers[i];
        ws.Cell(1, Headers.Length + 1).Value = ErrorHeader;
        ws.Row(1).Style.Font.Bold = true;

        if (!string.IsNullOrWhiteSpace(fileError) && rows.Count == 0)
        {
            ws.Cell(2, Headers.Length + 1).Value = fileError;
        }
        else
        {
            var r = 2;
            foreach (var item in rows)
            {
                var row = item.Row;
                ws.Cell(r, 1).Value = row.Code ?? "";
                ws.Cell(r, 2).Value = row.PickupAt ?? "";
                ws.Cell(r, 3).Value = row.CustomerCode ?? "";
                ws.Cell(r, 4).Value = row.PickupCity ?? "";
                ws.Cell(r, 5).Value = row.DeliveryCity ?? "";
                ws.Cell(r, 6).Value = row.Plate ?? "";
                ws.Cell(r, 7).Value = row.Tonnage ?? "";
                ws.Cell(r, 8).Value = row.Freight ?? "";
                ws.Cell(r, 9).Value = row.ExtraCost ?? "";
                ws.Cell(r, 10).Value = row.Notes ?? row.Route ?? "";
                ws.Cell(r, 11).Value = row.PaymentMethod ?? "";
                ws.Cell(r, 12).Value = row.DriverName ?? "";
                if (item.Errors.Count > 0)
                {
                    ws.Cell(r, 13).Value = string.Join(" ", item.Errors);
                    ws.Cell(r, 13).Style.Font.FontColor = XLColor.DarkRed;
                }
                r++;
            }
        }

        ws.SheetView.FreezeRows(1);
        var lastCol = Headers.Length + 1;
        var lastRow = Math.Max(ws.LastRowUsed()?.RowNumber() ?? 1, 1);
        ws.Range(1, 1, lastRow, lastCol).SetAutoFilter();
        ws.Columns().AdjustToContents();
        wb.SaveAs(stream);
    }

    private static bool LooksLikeTemplate(IXLWorksheet ws)
    {
        var titles = Enumerable.Range(1, 20)
            .Select(c => DispatchExcelCells.Text(ws.Cell(1, c)))
            .Where(t => t.Length > 0)
            .ToList();
        return titles.Any(t => t.Equals("Ngày chạy", StringComparison.OrdinalIgnoreCase))
               && titles.Any(t => t.Equals("Điểm đi", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<DispatchImportRow> ParseTemplate(IXLWorksheet ws)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = 1; c <= 30; c++)
        {
            var title = DispatchExcelCells.Text(ws.Cell(1, c));
            if (title.Length > 0 && !title.Equals(ErrorHeader, StringComparison.OrdinalIgnoreCase))
                map.TryAdd(title, c);
        }

        int Col(string header)
        {
            if (map.TryGetValue(header, out var c)) return c;
            var i = Array.IndexOf(Headers, header);
            return i >= 0 ? i + 1 : -1;
        }

        var rows = new List<DispatchImportRow>();
        var last = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= last; r++)
        {
            string Cell(string header)
            {
                var c = Col(header);
                return c < 1 ? "" : DispatchExcelCells.Text(ws.Cell(r, c));
            }

            var values = Headers.Select(Cell).ToArray();
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            rows.Add(new DispatchImportRow
            {
                ExcelRow = r,
                Code = values[0],
                PickupAt = values[1],
                CustomerCode = values[2],
                PickupCity = values[3],
                DeliveryCity = values[4],
                Plate = values[5],
                Tonnage = values[6],
                Freight = values[7],
                ExtraCost = values[8],
                Notes = values[9],
                PaymentMethod = values[10],
                DriverName = values[11]
            });
        }

        return rows;
    }
}
