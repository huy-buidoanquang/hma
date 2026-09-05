using Hma.Domain.Entities;

namespace Hma.Reporting;

public interface IDocumentPrinter
{
    string PrintDispatch(DispatchOrder order, Company company);
    string PrintCashReceipt(CashReceipt receipt, Company company);
    string PrintCashPayment(CashPayment payment, Company company);
    string PrintVatInvoice(VatInvoice invoice, Company company);
    string PrintDailyDispatch(IReadOnlyList<DispatchOrder> orders, DateTime day, Company company);
    string PrintDispatchSummary(IReadOnlyList<DispatchOrder> orders, string title, Company company);
    string ExportVatExcel(IReadOnlyList<VatInvoice> invoices, string path);
    string PrintFreightStatement(FreightStatement statement, Company company);
    string ExportFreightStatementExcel(FreightStatement statement, string path);
    string ExportCustomersExcel(IReadOnlyList<Customer> customers, string path);
    string ExportDispatchExcel(IReadOnlyList<DispatchOrder> orders, string path);
    string ExportVehiclesExcel(IReadOnlyList<Vehicle> vehicles, string path);
    string ExportPartnersExcel(IReadOnlyList<Partner> partners, string path);
    string ExportDriversExcel(IReadOnlyList<Driver> drivers, string path);
    string ExportPeriodSummaryExcel(IReadOnlyList<DispatchOrder> orders, DateTime from, DateTime to, string path);
}
