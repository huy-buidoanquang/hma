using Hma.Application.Features.Accounting;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Settings;
using Hma.Application.Features.Statements;

namespace Hma.Application.Abstractions.Reporting;

public interface IDocumentRenderer
{
    GeneratedDocument PrintDispatch(DispatchOrderPrintDetails order, CompanyDetails company);
    GeneratedDocument PrintCashReceipt(CashReceiptDetails receipt, CompanyDetails company);
    GeneratedDocument PrintCashPayment(CashPaymentDetails payment, CompanyDetails company);
    GeneratedDocument PrintVatInvoice(VatInvoiceDetails invoice, CompanyDetails company);
    GeneratedDocument PrintDailyDispatch(IReadOnlyList<DispatchOrderSummary> orders, DateTime day, CompanyDetails company);
    GeneratedDocument PrintDispatchSummary(IReadOnlyList<DispatchOrderSummary> orders, string title, CompanyDetails company);
    GeneratedDocument PrintFreightStatement(FreightStatementDetails statement, CompanyDetails company);
    GeneratedDocument ExportVatExcel(IReadOnlyList<VatInvoiceDetails> invoices);
    GeneratedDocument ExportFreightStatementExcel(FreightStatementDetails statement);
    GeneratedDocument ExportCustomersExcel(IReadOnlyList<CustomerSummary> customers);
    GeneratedDocument ExportDispatchExcel(IReadOnlyList<DispatchOrderSummary> orders);
    GeneratedDocument ExportVehiclesExcel(IReadOnlyList<VehicleSummary> vehicles);
    GeneratedDocument ExportPartnersExcel(IReadOnlyList<PartnerSummary> partners);
    GeneratedDocument ExportDriversExcel(IReadOnlyList<DriverSummary> drivers);
    GeneratedDocument ExportPeriodSummaryExcel(IReadOnlyList<DispatchOrderSummary> orders, DateTime from, DateTime to);
}
