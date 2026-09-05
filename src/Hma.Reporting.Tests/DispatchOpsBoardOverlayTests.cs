using ClosedXML.Excel;
using Hma.Application.Services;
using Hma.Reporting;

namespace Hma.Reporting.Tests;

public class DispatchOpsBoardOverlayTests
{
    private static readonly string[] Headers =
        ["STT", "Tên lái xe", "Biển số", "Tuyến đường", "Ghi chú", "Thành tiền", "Khách hàng"];

    [Fact]
    public void WriteCheck_inserts_error_column_after_each_shift_and_reparses()
    {
        using var source = BuildTwoShiftWorkbook();
        var parser = new DispatchImportParser();
        source.Position = 0;
        var parsed = parser.Parse(source);
        Assert.Equal(2, parsed.Count);
        Assert.Equal("TEC", parsed[0].CustomerCode);
        Assert.Equal("", parsed[1].CustomerCode);
        Assert.Equal("ca trái", parsed[0].BlockLabel);
        Assert.Equal("ca phải", parsed[1].BlockLabel);
        Assert.Equal("nb", parsed[0].PickupCity);
        Assert.Equal("1.25", parsed[0].Tonnage);

        var checks = new List<DispatchImportCheck>
        {
            new() { Row = parsed[0], Errors = [] },
            new() { Row = parsed[1], Errors = ["Thiếu khách hàng."] }
        };

        using var output = new MemoryStream();
        source.Position = 0;
        parser.WriteCheck(source, checks, null, output);

        output.Position = 0;
        using var wb = new XLWorkbook(output);
        var ws = wb.Worksheet("1.25");
        Assert.Equal("Lỗi", ws.Cell(3, 8).GetString());
        Assert.Equal("STT", ws.Cell(3, 9).GetString());
        Assert.Equal("Lỗi", ws.Cell(3, 16).GetString());
        Assert.Equal("TEC", ws.Cell(4, 7).GetString());
        Assert.Equal("", ws.Cell(4, 8).GetString());
        Assert.Equal("Thiếu khách hàng.", ws.Cell(4, 16).GetString());

        output.Position = 0;
        var again = parser.Parse(output);
        Assert.Equal(2, again.Count);
        Assert.Equal("TEC", again[0].CustomerCode);
        Assert.True(string.IsNullOrWhiteSpace(again[1].CustomerCode));
        Assert.Equal(1, again[0].StartColumn);
        Assert.Equal(9, again[1].StartColumn);
    }

    [Fact]
    public void Parse_emits_started_row_with_blank_route_and_skips_idle()
    {
        using var source = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("8");
            ws.Cell(2, 4).Value = "Ngày 11 tháng 08 năm 2026";
            WriteHeaders(ws, 1);
            ws.Cell(4, 1).Value = 1;
            ws.Cell(4, 2).Value = "An";
            ws.Cell(4, 3).Value = "29C91076";
            ws.Cell(4, 7).Value = "TEC";
            ws.Cell(5, 1).Value = 2;
            ws.Cell(5, 2).Value = "Bình";
            ws.Cell(5, 3).Value = "29C11111";
            ws.Cell(5, 4).Value = "X";
            wb.SaveAs(source);
        }

        source.Position = 0;
        var rows = new DispatchImportParser().Parse(source);
        Assert.Single(rows);
        Assert.Equal("1", rows[0].Stt);
        Assert.True(string.IsNullOrWhiteSpace(rows[0].Route));
        Assert.Equal("8", rows[0].Tonnage);
    }

    [Fact]
    public void Parse_does_not_take_tonnage_from_range_sheet_name()
    {
        using var source = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("3.5 - 5");
            ws.Cell(2, 4).Value = "Ngày 11 tháng 08 năm 2026";
            WriteHeaders(ws, 1);
            ws.Cell(4, 1).Value = 1;
            ws.Cell(4, 2).Value = "An";
            ws.Cell(4, 3).Value = "29C91076";
            ws.Cell(4, 4).Value = "nb - hp";
            ws.Cell(4, 7).Value = "TEC";
            wb.SaveAs(source);
        }

        source.Position = 0;
        var row = Assert.Single(new DispatchImportParser().Parse(source));
        Assert.True(string.IsNullOrWhiteSpace(row.Tonnage));
    }

    [Fact]
    public void WriteTemplate_is_six_shift_ops_board()
    {
        using var stream = new MemoryStream();
        new DispatchImportParser().WriteTemplate(stream);
        stream.Position = 0;
        using var wb = new XLWorkbook(stream);
        Assert.Equal(["1.25", "1.5", "2.5", "3.5 - 5", "8", "10"], wb.Worksheets.Select(s => s.Name));
        var ws = wb.Worksheet("1.25");
        Assert.Equal("STT", ws.Cell(3, 1).GetString());
        Assert.Equal("Khách hàng", ws.Cell(3, 7).GetString());
        Assert.Equal("STT", ws.Cell(3, 8).GetString());
        stream.Position = 0;
        Assert.Empty(new DispatchImportParser().Parse(stream));
    }

    [Fact]
    public void Round_trips_sample_ops_board_when_present()
    {
        var path = FindSample("11_08_2026.xlsx");
        if (path is null)
            return;

        FileStream? input;
        try
        {
            input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (IOException)
        {
            return;
        }

        using (input)
        {
            var parser = new DispatchImportParser();
            var rows = parser.Parse(input);
            Assert.True(rows.Count > 100, $"Expected roster rows, got {rows.Count}");
            Assert.Contains(rows, r => string.IsNullOrWhiteSpace(r.Route));
            Assert.Contains(rows, r => r.SheetName == "8");
            Assert.Contains(rows, r => r.SheetName == "10");
            Assert.DoesNotContain(rows, r => string.Equals(r.Route, "X", StringComparison.OrdinalIgnoreCase));

            var checks = rows.Select((r, i) => new DispatchImportCheck
            {
                Row = r,
                Errors = i == 0 ? ["probe"] : []
            }).ToList();

            using var output = new MemoryStream();
            input.Position = 0;
            parser.WriteCheck(input, checks, null, output);

            output.Position = 0;
            var again = parser.Parse(output);
            Assert.Equal(rows.Count, again.Count);

            output.Position = 0;
            using var wb = new XLWorkbook(output);
            var first = wb.Worksheet(rows[0].SheetName);
            Assert.Equal("Lỗi", first.Cell(3, 8).GetString());
            Assert.Equal("STT", first.Cell(3, 9).GetString());
            Assert.Equal("probe", first.Cell(rows[0].ExcelRow, 8).GetString());
        }
    }

    private static MemoryStream BuildTwoShiftWorkbook()
    {
        var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("1.25");
        ws.Cell(1, 1).Value = new DateTime(2026, 8, 11);
        ws.Cell(2, 4).Value = "Ngày 11 tháng 08 năm 2026";
        WriteHeaders(ws, 1);
        WriteHeaders(ws, 8);
        WriteTrip(ws, 4, 1, "1", "An", "29C91076", "nb - hp", "TEC");
        WriteTrip(ws, 4, 8, "1", "An", "29C91076", "nb - hp", "");
        wb.SaveAs(ms);
        return ms;
    }

    private static void WriteHeaders(IXLWorksheet ws, int start)
    {
        for (var i = 0; i < Headers.Length; i++)
            ws.Cell(3, start + i).Value = Headers[i];
    }

    private static void WriteTrip(
        IXLWorksheet ws, int row, int start, string stt, string driver, string plate, string route, string customer)
    {
        ws.Cell(row, start).Value = stt;
        ws.Cell(row, start + 1).Value = driver;
        ws.Cell(row, start + 2).Value = plate;
        ws.Cell(row, start + 3).Value = route;
        ws.Cell(row, start + 6).Value = customer;
    }

    private static string? FindSample(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, fileName);
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
