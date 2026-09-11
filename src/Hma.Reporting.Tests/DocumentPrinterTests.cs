using ClosedXML.Excel;
using Hma.Domain.Entities;

namespace Hma.Reporting.Tests;

public class DocumentPrinterTests
{
    [Fact]
    public void Temporary_report_paths_are_unique_and_keep_the_requested_extension()
    {
        var first = TemporaryReportFile.Create("BK-002.pdf");
        var second = TemporaryReportFile.Create("BK-002.pdf");

        Assert.NotEqual(first, second);
        Assert.Equal(".pdf", Path.GetExtension(first));
        Assert.StartsWith(Path.GetTempPath(), first, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_freight_statement_keeps_exception_revenue_visible_and_sizes_columns()
    {
        var statement = new FreightStatement
        {
            Code = "002",
            Year = 2026,
            Month = 10,
            Customer = new Customer { Name = "UI" },
            TripCount = 1,
            FreightTotal = 1_000_000,
            ExtraCostTotal = 50_000,
            GrandTotal = 1_050_000,
            VatRate = 10,
            VatAmount = 105_000,
            TotalWithVat = 1_155_000,
            Lines =
            [
                new FreightStatementLine
                {
                    TripDate = new DateTime(2026, 9, 12),
                    DispatchCode = "003",
                    Route = "an tảo TP hưng yên → nb",
                    PlateNumber = "99Z-99999",
                    Tonnage = "Xe 1.25 tấn",
                    DriverName = "Tài xế kiểm thử UI",
                    UnitPrice = 1_000_000,
                    ExtraCost = 50_000,
                    LineTotal = 1_050_000
                }
            ]
        };

        var path = Path.Combine(Path.GetTempPath(), $"hma-statement-{Guid.NewGuid():N}.xlsx");
        try
        {
            new DocumentPrinter().ExportFreightStatementExcel(statement, path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet("Bang ke");
            Assert.Equal("Phát sinh", sheet.Cell(4, 10).GetString());
            Assert.Equal(50_000m, sheet.Cell(5, 10).GetValue<decimal>());
            Assert.Equal("dd/MM/yyyy", sheet.Cell(5, 2).Style.DateFormat.Format);
            Assert.Equal("#,##0", sheet.Cell(7, 11).Style.NumberFormat.Format);
            Assert.Equal("#,##0", sheet.Cell(8, 11).Style.NumberFormat.Format);
            Assert.Equal("#,##0", sheet.Cell(9, 11).Style.NumberFormat.Format);
            Assert.True(sheet.Cell(7, 10).Style.Font.Bold);
            Assert.True(sheet.Cell(9, 11).Style.Font.Bold);
            Assert.True(sheet.Column(2).Width > 10);
            Assert.True(sheet.Column(4).Width > 20);
            Assert.Equal(4, sheet.SheetView.SplitRow);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_period_summary_includes_approved_exception_revenue_in_extra_cost()
    {
        var order = new DispatchOrder
        {
            Code = "001",
            PickupAt = new DateTime(2026, 9, 11, 20, 0, 0),
            UnitPrice = 1_000_000,
            ExtraCost = 20_000,
            ApprovedExceptionRevenue = 30_000,
            PartnerNameSnapshot = "Đối tác kiểm thử",
            PartnerOperatingFeePercent = 5,
            PartnerPayableAmount = 693_500
        };
        order.RecalculateTotal();

        var path = Path.Combine(Path.GetTempPath(), $"hma-report-{Guid.NewGuid():N}.xlsx");
        try
        {
            new DocumentPrinter().ExportPeriodSummaryExcel(
                [order],
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet("Tong hop");
            Assert.Equal(50_000m, sheet.Cell(3, 8).GetValue<decimal>());
            Assert.Equal(1_050_000m, sheet.Cell(3, 9).GetValue<decimal>());
            Assert.Equal("Đối tác kiểm thử", sheet.Cell(3, 6).GetString());
            Assert.Equal(693_500m, sheet.Cell(3, 11).GetValue<decimal>());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
