using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Settings;
using Hma.Application.Features.Statements;
using Hma.Domain.Entities;

namespace Hma.Reporting;

public sealed class ReportDocumentRenderer(DocumentPrinter printer) : IDocumentRenderer
{
    private const string Pdf = "application/pdf";
    private const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public GeneratedDocument PrintDispatch(DispatchOrderPrintDetails order, CompanyDetails company) =>
        Capture(() => printer.PrintDispatch(ToEntity(order), ToEntity(company)), $"LDX-{order.Summary.Code}.pdf", Pdf);

    public GeneratedDocument PrintCashReceipt(CashReceiptDetails receipt, CompanyDetails company) =>
        Capture(() => printer.PrintCashReceipt(ToEntity(receipt), ToEntity(company)), $"PT-{receipt.Code}.pdf", Pdf);

    public GeneratedDocument PrintCashPayment(CashPaymentDetails payment, CompanyDetails company) =>
        Capture(() => printer.PrintCashPayment(ToEntity(payment), ToEntity(company)), $"PC-{payment.Code}.pdf", Pdf);

    public GeneratedDocument PrintVatInvoice(VatInvoiceDetails invoice, CompanyDetails company) =>
        Capture(() => printer.PrintVatInvoice(ToEntity(invoice), ToEntity(company)), $"HD-{invoice.Code}.pdf", Pdf);

    public GeneratedDocument PrintDailyDispatch(IReadOnlyList<DispatchOrderSummary> orders, DateTime day, CompanyDetails company) =>
        Capture(() => printer.PrintDailyDispatch(orders.Select(ToEntity).ToList(), day, ToEntity(company)), $"BC-LDX-{day:yyyyMMdd}.pdf", Pdf);

    public GeneratedDocument PrintDispatchSummary(IReadOnlyList<DispatchOrderSummary> orders, string title, CompanyDetails company) =>
        Capture(() => printer.PrintDispatchSummary(orders.Select(ToEntity).ToList(), title, ToEntity(company)), "BC-LDX.pdf", Pdf);

    public GeneratedDocument PrintFreightStatement(FreightStatementDetails statement, CompanyDetails company) =>
        Capture(() => printer.PrintFreightStatement(ToEntity(statement), ToEntity(company)), $"BK-{statement.Code}.pdf", Pdf);

    public GeneratedDocument ExportVatExcel(IReadOnlyList<VatInvoiceDetails> invoices) =>
        Capture(path => printer.ExportVatExcel(invoices.Select(ToEntity).ToList(), path), "HDGTGT.xlsx", Excel);

    public GeneratedDocument ExportFreightStatementExcel(FreightStatementDetails statement) =>
        Capture(path => printer.ExportFreightStatementExcel(ToEntity(statement), path), $"BK-{statement.Code}.xlsx", Excel);

    public GeneratedDocument ExportCustomersExcel(IReadOnlyList<CustomerSummary> customers) =>
        Capture(path => printer.ExportCustomersExcel(customers.Select(ToEntity).ToList(), path), "KH.xlsx", Excel);

    public GeneratedDocument ExportDispatchExcel(IReadOnlyList<DispatchOrderSummary> orders) =>
        Capture(path => printer.ExportDispatchExcel(orders.Select(ToEntity).ToList(), path), "DS-CUOC.xlsx", Excel);

    public GeneratedDocument ExportVehiclesExcel(IReadOnlyList<VehicleSummary> vehicles) =>
        Capture(path => printer.ExportVehiclesExcel(vehicles.Select(ToEntity).ToList(), path), "DS-XE.xlsx", Excel);

    public GeneratedDocument ExportPartnersExcel(IReadOnlyList<PartnerSummary> partners) =>
        Capture(path => printer.ExportPartnersExcel(partners.Select(ToEntity).ToList(), path), "DS-DOI-TAC.xlsx", Excel);

    public GeneratedDocument ExportDriversExcel(IReadOnlyList<DriverSummary> drivers) =>
        Capture(path => printer.ExportDriversExcel(drivers.Select(ToEntity).ToList(), path), "DS-TAI-XE.xlsx", Excel);

    public GeneratedDocument ExportPeriodSummaryExcel(IReadOnlyList<DispatchOrderSummary> orders, DateTime from, DateTime to) =>
        Capture(
            path => printer.ExportPeriodSummaryExcel(orders.Select(ToEntity).ToList(), from, to, path),
            $"TONG-KY-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx",
            Excel);

    private static GeneratedDocument Capture(Func<string> render, string fileName, string contentType)
    {
        var path = render();
        return ReadAndDelete(path, fileName, contentType);
    }

    private static GeneratedDocument Capture(Func<string, string> render, string fileName, string contentType)
    {
        var path = TemporaryReportFile.Create(fileName);
        render(path);
        return ReadAndDelete(path, fileName, contentType);
    }

    private static GeneratedDocument ReadAndDelete(string path, string fileName, string contentType)
    {
        try
        {
            return new GeneratedDocument(Path.GetFileName(fileName), contentType, File.ReadAllBytes(path));
        }
        finally
        {
            try { File.Delete(path); }
            catch { /* A generated temp file can be reclaimed by the OS later. */ }
        }
    }

    private static Company ToEntity(CompanyDetails item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Address = item.Address,
        Phone = item.Phone,
        TaxCode = item.TaxCode,
        Bank = item.Bank,
        Website = item.Website,
        Email = item.Email,
    };

    private static DispatchOrder ToEntity(DispatchOrderSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        PickupAt = item.PickupAt,
        Status = item.Status,
        ReconciliationStatus = item.ReconciliationStatus,
        Customer = ToEntity(item.Customer),
        Driver = item.Driver is null ? null : new Driver
        {
            Id = item.Driver.Id,
            Code = item.Driver.Code,
            Name = item.Driver.Name,
            Phone = item.Driver.Phone,
        },
        Vehicle = item.Vehicle is null ? null : new Vehicle
        {
            Id = item.Vehicle.Id,
            PlateNumber = item.Vehicle.PlateNumber,
            Tonnage = item.Vehicle.Tonnage,
        },
        VehicleType = item.VehicleType is null ? null : new VehicleType
        {
            Id = item.VehicleType.Id,
            Code = item.VehicleType.Code,
            Name = item.VehicleType.Name,
            Tonnage = item.VehicleType.Tonnage,
        },
        PaymentMethod = item.PaymentMethod is null ? null : new PaymentMethod
        {
            Id = item.PaymentMethod.Id,
            Code = item.PaymentMethod.Code,
            Name = item.PaymentMethod.Name,
        },
        Route = new Route { Name = item.RouteLabel },
        UnitPrice = item.UnitPrice,
        Surcharge = item.Surcharge,
        ExtraCost = item.ExtraCost,
        ApprovedExceptionRevenue = item.ApprovedExceptionRevenue,
        TotalAmount = item.TotalAmount,
        BillingYear = item.BillingYear,
        BillingMonth = item.BillingMonth,
        Notes = item.Notes,
        SenderName = item.SenderName,
        PartnerNameSnapshot = item.PartnerNameSnapshot,
        PartnerOperatingFeePercent = item.PartnerOperatingFeePercent,
        PartnerPayableAmount = item.PartnerPayableAmount,
        ArNumber = item.ArNumber,
    };

    private static DispatchOrder ToEntity(DispatchOrderPrintDetails item)
    {
        var order = ToEntity(item.Summary);
        order.CreatedAt = item.CreatedAt;
        order.CreatedByUser = string.IsNullOrWhiteSpace(item.CreatedByName)
            ? null
            : new AppUser { DisplayName = item.CreatedByName, UserName = item.CreatedByName };
        order.SenderCustomer = ToEntity(item.SenderCustomer);
        order.SenderName = item.SenderName;
        order.SenderPhone = item.SenderPhone;
        order.SenderAddress = item.SenderAddress;
        order.PickupAddress = item.PickupAddress;
        order.ReceiverCustomer = ToEntity(item.ReceiverCustomer);
        order.ReceiverName = item.ReceiverName;
        order.ReceiverPhone = item.ReceiverPhone;
        order.ReceiverAddress = item.ReceiverAddress;
        order.DeliveryAddress = item.DeliveryAddress;
        order.Lines = item.Lines.Select(line => new DispatchOrderLine
        {
            LineNumber = line.LineNumber,
            GoodsName = line.GoodsName,
            PackageCount = line.PackageCount,
            Route = line.Route,
            Kilometers = line.Kilometers,
            Notes = line.Notes,
        }).ToList();
        return order;
    }

    private static CashReceipt ToEntity(CashReceiptDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        DocumentDate = item.DocumentDate,
        Kind = item.Kind,
        CustomerId = item.CustomerId,
        Customer = ToEntity(item.Customer),
        Amount = item.Amount,
        AmountInWords = item.AmountInWords,
        PayerName = item.PayerName,
        Address = item.Address,
        Reason = item.Reason,
    };

    private static CashPayment ToEntity(CashPaymentDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        DocumentDate = item.DocumentDate,
        Kind = item.Kind,
        CustomerId = item.CustomerId,
        Customer = ToEntity(item.Customer),
        DriverEmployeeId = item.DriverEmployeeId,
        DriverEmployee = item.DriverEmployee is null ? null : new Employee
        {
            Id = item.DriverEmployee.Id,
            Code = item.DriverEmployee.Code,
            Name = item.DriverEmployee.Name,
        },
        Amount = item.Amount,
        AmountInWords = item.AmountInWords,
        PayeeName = item.PayeeName,
        Address = item.Address,
        Reason = item.Reason,
    };

    private static VatInvoice ToEntity(VatInvoiceDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        InvoiceDate = item.InvoiceDate,
        CustomerId = item.CustomerId,
        Customer = ToEntity(item.Customer),
        GoodsName = item.GoodsName,
        Amount = item.Amount,
        VatRate = item.VatRate,
        VatAmount = item.VatAmount,
        TotalAmount = item.TotalAmount,
        AmountInWords = item.AmountInWords,
    };

    private static FreightStatement ToEntity(FreightStatementDetails item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        CustomerId = item.CustomerId,
        Customer = ToEntity(item.Customer),
        Year = item.Year,
        Month = item.Month,
        Status = item.Status,
        TripCount = item.TripCount,
        FreightTotal = item.FreightTotal,
        SurchargeTotal = item.SurchargeTotal,
        ExtraCostTotal = item.ExtraCostTotal,
        GrandTotal = item.GrandTotal,
        VatRate = item.VatRate,
        VatAmount = item.VatAmount,
        TotalWithVat = item.TotalWithVat,
        Notes = item.Notes,
        Lines = item.Lines.Select(line => new FreightStatementLine
        {
            Id = line.Id,
            TripDate = line.TripDate,
            DispatchCode = line.DispatchCode,
            Route = line.Route,
            PlateNumber = line.PlateNumber,
            Tonnage = line.Tonnage,
            DriverName = line.DriverName,
            UnitPrice = line.UnitPrice,
            Surcharge = line.Surcharge,
            ExtraCost = line.ExtraCost,
            LineTotal = line.LineTotal,
            Notes = line.Notes,
        }).ToList(),
    };

    private static Customer? ToEntity(CustomerOption? item) => item is null ? null : new Customer
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Address = item.Address,
        Phone = item.Phone,
        TaxCode = item.TaxCode,
        IsWalkIn = item.IsWalkIn,
    };

    private static Partner ToEntity(PartnerSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        TaxCode = item.TaxCode,
        Address = item.Address,
        ContactName = item.ContactName,
        Phone = item.Phone,
        Email = item.Email,
        OperatingFeePercent = item.OperatingFeePercent,
    };

    private static Customer ToEntity(CustomerSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Address = item.Address,
        Phone = item.Phone,
        TaxCode = item.TaxCode,
        ContactName = item.ContactName,
        Email = item.Email,
        CityId = item.CityId,
        City = item.City is null ? null : new City { Id = item.City.Id, Code = item.City.Code, Name = item.City.Name },
        AccountantEmployeeId = item.AccountantEmployeeId,
        AccountantEmployee = item.AccountantEmployee is null ? null : new Employee
        {
            Id = item.AccountantEmployee.Id,
            Code = item.AccountantEmployee.Code,
            Name = item.AccountantEmployee.Name,
        },
        IsWalkIn = item.IsWalkIn,
        UpdatedAt = item.UpdatedAt,
    };

    private static Driver ToEntity(DriverSummary item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Phone = item.Phone,
        BirthDate = item.BirthDate,
        IdentityNumber = item.IdentityNumber,
        PartnerId = item.PartnerId,
        Partner = item.Partner is null ? null : new Partner
        {
            Id = item.Partner.Id,
            Code = item.Partner.Code,
            Name = item.Partner.Name,
            OperatingFeePercent = item.Partner.OperatingFeePercent,
        },
    };

    private static Vehicle ToEntity(VehicleSummary item) => new()
    {
        Id = item.Id,
        PlateNumber = item.PlateNumber,
        PartnerId = item.PartnerId,
        Partner = item.Partner is null ? null : new Partner
        {
            Id = item.Partner.Id,
            Code = item.Partner.Code,
            Name = item.Partner.Name,
            OperatingFeePercent = item.Partner.OperatingFeePercent,
        },
        VehicleTypeId = item.VehicleTypeId,
        VehicleType = item.VehicleType is null ? null : new VehicleType
        {
            Id = item.VehicleType.Id,
            Code = item.VehicleType.Code,
            Name = item.VehicleType.Name,
            Tonnage = item.VehicleType.Tonnage,
        },
        Tonnage = item.Tonnage,
    };
}
