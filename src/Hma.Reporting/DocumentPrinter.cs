using ClosedXML.Excel;
using Hma.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hma.Reporting;

public sealed class DocumentPrinter : IDocumentPrinter
{
    public DocumentPrinter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string PrintDispatch(DispatchOrder order, Company company)
    {
        var path = Path.Combine(Path.GetTempPath(), $"LDX-{order.Code}.pdf");
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.Header().Column(c =>
                {
                    c.Item().Text(company.Name).FontSize(11).SemiBold();
                    c.Item().AlignCenter().Text("LỆNH ĐIỀU XE").FontSize(16).Bold();
                    c.Item().AlignCenter().Text($"Số {order.Code}  ·  Ngày tạo {order.CreatedAt:dd/MM/yyyy HH:mm}  ·  {order.CreatedByUser?.DisplayName ?? order.CreatedByUser?.UserName}").FontSize(10);
                });
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Text($"Ngày lấy hàng: {order.PickupAt:dd/MM/yyyy HH:mm}").FontSize(10);
                    col.Item().PaddingTop(6).Text($"Người gửi: {order.SenderName ?? order.SenderCustomer?.Name ?? order.Customer?.Name}  ·  ĐT {order.SenderPhone ?? order.SenderCustomer?.Phone}  ·  {order.SenderAddress ?? order.PickupAddress}").FontSize(10);
                    col.Item().Text($"Người nhận: {order.ReceiverName ?? order.ReceiverCustomer?.Name}  ·  ĐT {order.ReceiverPhone ?? order.ReceiverCustomer?.Phone}  ·  {order.ReceiverAddress ?? order.DeliveryAddress}").FontSize(10);
                    col.Item().Text($"Tuyến: {order.RouteLabel}  ·  Xe {order.Vehicle?.PlateNumber}  ·  {order.VehicleType?.Name}  ·  TX {order.Driver?.Name} {order.Driver?.Phone}").FontSize(10);
                    if (order.Lines.Count > 0)
                    {
                        col.Item().PaddingTop(10).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(28);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.5f);
                            });
                            t.Header(h =>
                            {
                                foreach (var title in new[] { "STT", "Tên hàng", "Số kiện", "Hành trình", "Km", "Ghi chú" })
                                    h.Cell().Text(title).SemiBold().FontSize(9);
                            });
                            foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
                            {
                                t.Cell().Text(line.LineNumber.ToString()).FontSize(9);
                                t.Cell().Text(line.GoodsName ?? "").FontSize(9);
                                t.Cell().Text(line.PackageCount?.ToString() ?? "").FontSize(9);
                                t.Cell().Text(line.Route ?? "").FontSize(9);
                                t.Cell().Text(line.Kilometers?.ToString("N1") ?? "").FontSize(9);
                                t.Cell().Text(line.Notes ?? "").FontSize(9);
                            }
                        });
                    }
                    col.Item().PaddingTop(10).Text($"Cước {order.UnitPrice:N0}  ·  Phụ phí {order.Surcharge:N0}  ·  Phát sinh {order.ExtraCost:N0}  ·  Tổng {order.TotalAmount:N0}").FontSize(11).SemiBold();
                    col.Item().Text($"Bằng chữ: {order.AmountInWords}").FontSize(10);
                    if (!string.IsNullOrWhiteSpace(order.Notes))
                        col.Item().Text($"Ghi chú: {order.Notes}").FontSize(10);
                });
            });
        }).GeneratePdf(path);
        return path;
    }

    public string PrintCashReceipt(CashReceipt receipt, Company company) =>
        Render($"PT-{receipt.Code}.pdf", company, "PHIẾU THU", [
            ("Số", receipt.Code),
            ("Ngày", receipt.DocumentDate.ToString("dd/MM/yyyy")),
            ("Người nộp", receipt.PayerName ?? receipt.Customer?.Name ?? ""),
            ("Địa chỉ", receipt.Address ?? ""),
            ("Số tiền", receipt.Amount.ToString("N0")),
            ("Bằng chữ", receipt.AmountInWords ?? ""),
            ("Lý do", receipt.Reason ?? "")
        ]);

    public string PrintCashPayment(CashPayment payment, Company company) =>
        Render($"PC-{payment.Code}.pdf", company, "PHIẾU CHI", [
            ("Số", payment.Code),
            ("Ngày", payment.DocumentDate.ToString("dd/MM/yyyy")),
            ("Người nhận", payment.PayeeName ?? payment.Customer?.Name ?? payment.DriverEmployee?.Name ?? ""),
            ("Địa chỉ", payment.Address ?? ""),
            ("Số tiền", payment.Amount.ToString("N0")),
            ("Bằng chữ", payment.AmountInWords ?? ""),
            ("Lý do", payment.Reason ?? "")
        ]);

    public string PrintVatInvoice(VatInvoice invoice, Company company) =>
        Render($"HD-{invoice.Code}.pdf", company, "HÓA ĐƠN GTGT", [
            ("Số", invoice.Code),
            ("Ngày", invoice.InvoiceDate.ToString("dd/MM/yyyy")),
            ("Khách hàng", invoice.Customer?.Name ?? ""),
            ("MST", invoice.Customer?.TaxCode ?? ""),
            ("Tên hàng", invoice.GoodsName ?? ""),
            ("Thành tiền", invoice.Amount.ToString("N0")),
            ("VAT %", invoice.VatRate.ToString("N0")),
            ("Tiền VAT", invoice.VatAmount.ToString("N0")),
            ("Tổng cộng", invoice.TotalAmount.ToString("N0")),
            ("Bằng chữ", invoice.AmountInWords ?? "")
        ]);

    public string PrintDailyDispatch(IReadOnlyList<DispatchOrder> orders, DateTime day, Company company) =>
        PrintDispatchSummary(orders, $"BÁO CÁO LỆNH ĐIỀU XE NGÀY {day:dd/MM/yyyy}", company);

    public string PrintDispatchSummary(IReadOnlyList<DispatchOrder> orders, string title, Company company)
    {
        var path = Path.Combine(Path.GetTempPath(), $"BC-LDX-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.Header().Column(c =>
                {
                    c.Item().Text(company.Name).FontSize(11).SemiBold();
                    c.Item().AlignCenter().Text(title).FontSize(14).Bold();
                });
                page.Content().PaddingTop(12).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(1.8f);
                        c.RelativeColumn(1.6f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.9f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(1);
                    });
                    t.Header(h =>
                    {
                        foreach (var header in new[] { "Số LDX", "Ngày", "Khách", "Tuyến", "Biển", "Tài xế", "Cước", "Phụ phí", "Phát sinh", "Tổng" })
                            h.Cell().Text(header).SemiBold().FontSize(8);
                    });
                    foreach (var o in orders)
                    {
                        t.Cell().Text(o.Code).FontSize(8);
                        t.Cell().Text(o.PickupAt.ToString("dd/MM")).FontSize(8);
                        t.Cell().Text(o.Customer?.Name ?? o.SenderName ?? "").FontSize(8);
                        t.Cell().Text(o.RouteLabel).FontSize(8);
                        t.Cell().Text(o.Vehicle?.PlateNumber ?? "").FontSize(8);
                        t.Cell().Text(o.Driver?.Name ?? "").FontSize(8);
                        t.Cell().AlignRight().Text(o.UnitPrice.ToString("N0")).FontSize(8);
                        t.Cell().AlignRight().Text(o.Surcharge.ToString("N0")).FontSize(8);
                        t.Cell().AlignRight().Text(o.ExtraCost.ToString("N0")).FontSize(8);
                        t.Cell().AlignRight().Text(o.TotalAmount.ToString("N0")).FontSize(8);
                    }
                });
                page.Footer().AlignRight().Text($"Số chuyến: {orders.Count}  ·  Tổng cước: {orders.Sum(o => o.TotalAmount):N0}");
            });
        }).GeneratePdf(path);
        return path;
    }

    public string ExportCustomersExcel(IReadOnlyList<Customer> customers, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Khach hang");
        var headers = new[] { "Mã", "Tên", "MST", "Địa chỉ", "Người liên hệ", "Điện thoại", "Email", "Thành phố", "Kế toán" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        var row = 2;
        foreach (var customer in customers)
        {
            ws.Cell(row, 1).Value = customer.Code;
            ws.Cell(row, 2).Value = customer.Name;
            ws.Cell(row, 3).Value = customer.TaxCode;
            ws.Cell(row, 4).Value = customer.Address;
            ws.Cell(row, 5).Value = customer.ContactName;
            ws.Cell(row, 6).Value = customer.Phone;
            ws.Cell(row, 7).Value = customer.Email;
            ws.Cell(row, 8).Value = customer.City?.Name;
            ws.Cell(row, 9).Value = customer.AccountantEmployee?.Name;
            row++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
        return path;
    }

    public string ExportDispatchExcel(IReadOnlyList<DispatchOrder> orders, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Cuoc");
        var headers = new[] { "Ngày", "Mã", "Tuyến", "Biển", "Tải", "Tài xế", "Cước", "Phụ phí", "Phát sinh", "Tổng", "Đối soát" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        var row = 2;
        foreach (var o in orders)
        {
            ws.Cell(row, 1).Value = o.PickupAt;
            ws.Cell(row, 2).Value = o.Code;
            ws.Cell(row, 3).Value = o.RouteLabel;
            ws.Cell(row, 4).Value = o.Vehicle?.PlateNumber;
            ws.Cell(row, 5).Value = o.VehicleType?.Name ?? o.Vehicle?.Tonnage?.ToString();
            ws.Cell(row, 6).Value = o.Driver?.Name;
            ws.Cell(row, 7).Value = o.UnitPrice;
            ws.Cell(row, 8).Value = o.Surcharge;
            ws.Cell(row, 9).Value = o.ExtraCost;
            ws.Cell(row, 10).Value = o.TotalAmount;
            ws.Cell(row, 11).Value = o.ReconciliationStatus.ToString();
            row++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
        return path;
    }

    public string PrintFreightStatement(FreightStatement statement, Company company)
    {
        var path = Path.Combine(Path.GetTempPath(), $"BK-{statement.Code}.pdf");
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.Header().Column(c =>
                {
                    c.Item().Text(company.Name).FontSize(11).SemiBold();
                    c.Item().AlignCenter().Text($"BẢNG KÊ CƯỚC VẬN CHUYỂN THÁNG {statement.Month:00}/{statement.Year}").FontSize(16).Bold();
                    c.Item().AlignCenter().Text($"Khách hàng: {statement.Customer?.Name}").FontSize(12);
                });
                page.Content().PaddingTop(12).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(28);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.6f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    t.Header(h =>
                    {
                        foreach (var title in new[] { "STT", "Ngày", "Mã lệnh", "Tuyến", "Biển số", "Trọng tải", "Tài xế", "Cước", "Phụ phí", "Tổng tiền" })
                            h.Cell().Text(title).SemiBold().FontSize(9);
                    });
                    var i = 1;
                    foreach (var line in statement.Lines)
                    {
                        t.Cell().Text(i++.ToString()).FontSize(9);
                        t.Cell().Text(line.TripDate.ToString("dd/MM")).FontSize(9);
                        t.Cell().Text(line.DispatchCode).FontSize(9);
                        t.Cell().Text(line.Route ?? "").FontSize(9);
                        t.Cell().Text(line.PlateNumber ?? "").FontSize(9);
                        t.Cell().Text(line.Tonnage ?? "").FontSize(9);
                        t.Cell().Text(line.DriverName ?? "").FontSize(9);
                        t.Cell().AlignRight().Text(line.UnitPrice.ToString("N0")).FontSize(9);
                        t.Cell().AlignRight().Text(line.Surcharge.ToString("N0")).FontSize(9);
                        t.Cell().AlignRight().Text(line.LineTotal.ToString("N0")).FontSize(9);
                    }
                });
                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"Số chuyến: {statement.TripCount}  |  Cước: {statement.FreightTotal:N0}  |  Phụ phí: {statement.SurchargeTotal:N0}  |  Phát sinh: {statement.ExtraCostTotal:N0}");
                    r.RelativeItem().AlignRight().Text($"Tổng: {statement.GrandTotal:N0}  |  VAT {statement.VatRate:N0}%: {statement.VatAmount:N0}  |  Sau VAT: {statement.TotalWithVat:N0}");
                });
            });
        }).GeneratePdf(path);
        return path;
    }

    public string ExportFreightStatementExcel(FreightStatement statement, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Bang ke");
        ws.Cell(1, 1).Value = $"BẢNG KÊ CƯỚC VẬN CHUYỂN THÁNG {statement.Month:00}/{statement.Year}";
        ws.Cell(2, 1).Value = $"Khách hàng: {statement.Customer?.Name}";
        var headers = new[] { "STT", "Ngày", "Mã lệnh", "Tuyến", "Biển số", "Trọng tải", "Tài xế", "Cước", "Phụ phí", "Phát sinh", "Tổng tiền", "Ghi chú" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(4, c + 1).Value = headers[c];
        var row = 5;
        var stt = 1;
        foreach (var line in statement.Lines)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = line.TripDate;
            ws.Cell(row, 3).Value = line.DispatchCode;
            ws.Cell(row, 4).Value = line.Route;
            ws.Cell(row, 5).Value = line.PlateNumber;
            ws.Cell(row, 6).Value = line.Tonnage;
            ws.Cell(row, 7).Value = line.DriverName;
            ws.Cell(row, 8).Value = line.UnitPrice;
            ws.Cell(row, 9).Value = line.Surcharge;
            ws.Cell(row, 10).Value = line.ExtraCost;
            ws.Cell(row, 11).Value = line.LineTotal;
            ws.Cell(row, 12).Value = line.Notes;
            row++;
        }
        ws.Cell(row + 1, 10).Value = "Tổng cước";
        ws.Cell(row + 1, 11).Value = statement.GrandTotal;
        ws.Cell(row + 2, 10).Value = $"VAT {statement.VatRate:N0}%";
        ws.Cell(row + 2, 11).Value = statement.VatAmount;
        ws.Cell(row + 3, 10).Value = "Tổng sau VAT";
        ws.Cell(row + 3, 11).Value = statement.TotalWithVat;
        wb.SaveAs(path);
        return path;
    }

    public string ExportVatExcel(IReadOnlyList<VatInvoice> invoices, string path)
    {
        using var wb = new XLWorkbook();
        void Sheet(string name, decimal rate)
        {
            var ws = wb.AddWorksheet(name);
            ws.Cell(1, 1).Value = "Số HĐ";
            ws.Cell(1, 2).Value = "Ngày";
            ws.Cell(1, 3).Value = "Khách hàng";
            ws.Cell(1, 4).Value = "MST";
            ws.Cell(1, 5).Value = "Thành tiền";
            ws.Cell(1, 6).Value = "VAT";
            ws.Cell(1, 7).Value = "Tổng";
            var row = 2;
            foreach (var i in invoices.Where(x => x.VatRate == rate))
            {
                ws.Cell(row, 1).Value = i.Code;
                ws.Cell(row, 2).Value = i.InvoiceDate;
                ws.Cell(row, 3).Value = i.Customer?.Name;
                ws.Cell(row, 4).Value = i.Customer?.TaxCode;
                ws.Cell(row, 5).Value = i.Amount;
                ws.Cell(row, 6).Value = i.VatAmount;
                ws.Cell(row, 7).Value = i.TotalAmount;
                row++;
            }
        }
        Sheet("VAT 0", 0);
        Sheet("VAT 5", 5);
        Sheet("VAT 10", 10);
        wb.SaveAs(path);
        return path;
    }

    private static string Render(string fileName, Company company, string title, (string Label, string Value)[] rows)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName);
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(36);
                page.Header().Column(c =>
                {
                    c.Item().Text(company.Name).FontSize(11).SemiBold();
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        c.Item().Text(company.Address!).FontSize(9);
                    c.Item().PaddingTop(8).AlignCenter().Text(title).FontSize(16).Bold();
                });
                page.Content().PaddingTop(16).Column(col =>
                {
                    foreach (var (label, value) in rows)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(1).Text(label).FontSize(10);
                            r.RelativeItem(2).Text(value).FontSize(10);
                        });
                    }
                    col.Item().PaddingTop(28).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Text("Người lập");
                        r.RelativeItem().AlignCenter().Text("Điều phối");
                    });
                });
            });
        }).GeneratePdf(path);
        return path;
    }
}
