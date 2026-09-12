using Hma.Application.Features.Accounting;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Pricing;
using Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;
using Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Tests;

public class PresentationModelMappingTests
{
    [Fact]
    public void Catalog_editor_round_trips_to_save_command()
    {
        var editor = CatalogItemEditorModel.From(new CatalogItemSummary(12, " PB01 ", "Điều hành", "Ghi chú"));

        var command = editor.ToCommand();

        Assert.Equal(12, command.Id);
        Assert.Equal(" PB01 ", command.Code);
        Assert.Equal("Điều hành", command.Name);
        Assert.Equal("Ghi chú", command.Description);
    }

    [Fact]
    public void Price_list_editor_keeps_concurrency_token_out_of_bindable_state()
    {
        var token = new byte[] { 1, 2, 3 };
        var summary = new PriceListSummary(
            7, "BG-01", "Bảng giá chuẩn", null, 4, null,
            new DateTime(2026, 1, 1), null, true, false, null, token);

        var command = PriceListEditorModel.From(summary).ToCommand(token);
        token[0] = 9;

        Assert.Equal(new byte[] { 1, 2, 3 }, command.VersionToken);
        Assert.Equal(7, command.Id);
        Assert.True(command.HasPriceFluctuation);
    }

    [Fact]
    public void Dispatch_editor_round_trip_preserves_snapshot_and_row_version()
    {
        var entity = new DispatchOrder
        {
            Id = 9,
            Code = "LDX-009",
            SenderName = "Kho A",
            ReceiverName = "Kho B",
            UnitPrice = 1_000_000,
            Surcharge = 100_000,
            TotalAmount = 1_100_000,
            RowVersion = [4, 5, 6],
            Lines = [new DispatchOrderLine { LineNumber = 1, GoodsName = "Thép" }],
        };

        var details = CreateDetails(entity);
        var mapped = DispatchOrderEditorModel.From(details).ToCommand(entity.RowVersion).Order;

        Assert.Equal(entity.Code, mapped.Header.Code);
        Assert.Equal(entity.SenderName, mapped.SenderName);
        Assert.Equal(entity.ReceiverName, mapped.ReceiverName);
        Assert.Equal(entity.TotalAmount, mapped.Header.TotalAmount);
        Assert.Equal("Thép", Assert.Single(mapped.Lines).GoodsName);
        Assert.Equal(entity.RowVersion, mapped.VersionToken);
        Assert.NotSame(entity.RowVersion, mapped.VersionToken);
    }

    [Fact]
    public void Accounting_editor_maps_to_application_command_without_domain_entity()
    {
        var details = new CashReceiptDetails(
            3, "PT-003", new DateTime(2026, 9, 12), CashReceiptKind.Customer,
            null, null, 5, new CustomerOption(5, "KH05", "Khách", null, null, null, false),
            500_000, "Năm trăm nghìn đồng", "Người nộp", null, "Thu cước", 2);

        var command = CashReceiptModel.From(details).ToCommand();

        Assert.Equal(details.Id, command.Id);
        Assert.Equal(details.CustomerId, command.CustomerId);
        Assert.Equal(details.Amount, command.Amount);
        Assert.Equal(details.Reason, command.Reason);
    }

    private static DispatchOrderDetails CreateDetails(DispatchOrder entity)
    {
        var header = new DispatchOrderSummary
        {
            Id = entity.Id,
            Code = entity.Code,
            PickupAt = entity.PickupAt,
            Status = entity.Status,
            ReconciliationStatus = entity.ReconciliationStatus,
            UnitPrice = entity.UnitPrice,
            Surcharge = entity.Surcharge,
            TotalAmount = entity.TotalAmount,
            BillingYear = entity.BillingYear,
            BillingMonth = entity.BillingMonth,
            CanEdit = entity.CanEdit,
        };
        return new DispatchOrderDetails(
            header, entity.CreatedAt, entity.CreatedByUserId, entity.ConfirmedAt, entity.CustomerId,
            entity.SenderCustomerId, null, entity.SenderName, entity.SenderPhone, entity.SenderAddress,
            entity.SenderTaxCode, entity.ReceiverCustomerId, null, entity.ReceiverName,
            entity.ReceiverPhone, entity.ReceiverAddress, entity.ReceiverTaxCode, entity.PickupAddress,
            entity.DeliveryAddress, entity.VehicleTypeId, entity.EmployeeId, entity.PaymentMethodId,
            entity.PriceListItemId, entity.PriceSourceSnapshot, entity.IsFreightManual,
            entity.FreightOverrideReason, entity.PartnerId, entity.PartnerNameSnapshot, entity.PartnerRateId,
            entity.BuyUnitPrice, entity.BuySurcharge, entity.BuyExtraCost, entity.ApprovedExceptionCost,
            entity.BuyTotal, entity.PartnerOperatingFeePercent, entity.PartnerPayableAmount,
            entity.GrossMargin, entity.BuyRateSourceSnapshot, entity.IsBuyManual, entity.BuyOverrideReason,
            entity.AmountInWords, entity.IsDeleted, entity.RowVersion,
            [], entity.Lines.Select(x => new DispatchOrderLineDetails(
                x.Id, x.LineNumber, x.GoodsName, x.PackageCount, x.Route, x.Kilometers, x.Notes)).ToList(), []);
    }
}
