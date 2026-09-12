using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Domain.Entities;

namespace Hma.Application.Features.Dispatching;

internal static class DispatchOrderMapping
{
    public static DispatchOrderDetails ToDetails(DispatchOrder item) => new(
        DispatchOrderQueryService.ToSummary(item), item.CreatedAt, item.CreatedByUserId, item.ConfirmedAt,
        item.CustomerId, item.SenderCustomerId, ToCustomer(item.SenderCustomer), item.SenderName,
        item.SenderPhone, item.SenderAddress, item.SenderTaxCode, item.ReceiverCustomerId,
        ToCustomer(item.ReceiverCustomer), item.ReceiverName, item.ReceiverPhone, item.ReceiverAddress,
        item.ReceiverTaxCode, item.PickupAddress, item.DeliveryAddress, item.VehicleTypeId, item.EmployeeId,
        item.PaymentMethodId, item.PriceListItemId, item.PriceSourceSnapshot, item.IsFreightManual,
        item.FreightOverrideReason, item.PartnerId,
        item.PartnerNameSnapshot, item.PartnerRateId, item.BuyUnitPrice, item.BuySurcharge,
        item.BuyExtraCost, item.ApprovedExceptionCost, item.BuyTotal, item.PartnerOperatingFeePercent,
        item.PartnerPayableAmount, item.GrossMargin, item.BuyRateSourceSnapshot, item.IsBuyManual,
        item.BuyOverrideReason, item.AmountInWords, item.IsDeleted, item.RowVersion.ToArray(),
        item.Stops.OrderBy(x => x.Sequence)
            .Select(x => new DispatchStopDetails(x.Id, x.Sequence, x.LocationId, x.NameSnapshot)).ToList(),
        item.Lines.OrderBy(x => x.LineNumber)
            .Select(x => new DispatchOrderLineDetails(
                x.Id, x.LineNumber, x.GoodsName, x.PackageCount, x.Route, x.Kilometers, x.Notes)).ToList(),
        item.Documents.OrderByDescending(x => x.UploadedAt)
            .Select(x => new DispatchDocumentDetails(
                x.Id, x.DispatchOrderId, x.Kind, x.FileName, x.StoredPath, x.UploadedAt,
                x.UploadedByUser is null ? null : new UserOption(
                    x.UploadedByUser.Id, x.UploadedByUser.UserName, x.UploadedByUser.DisplayName))).ToList())
    {
        PriceListCode = item.PriceListItem?.PriceListRevision?.PriceList?.Code
    };

    public static DispatchOrder ToEntity(DispatchOrderDetails details)
    {
        var header = details.Header;
        return new DispatchOrder
        {
            Id = header.Id,
            Code = header.Code,
            CreatedAt = details.CreatedAt,
            CreatedByUserId = details.CreatedByUserId,
            Status = header.Status,
            ReconciliationStatus = header.ReconciliationStatus,
            ConfirmedAt = details.ConfirmedAt,
            CustomerId = details.CustomerId,
            SenderCustomerId = details.SenderCustomerId,
            SenderName = details.SenderName,
            SenderPhone = details.SenderPhone,
            SenderAddress = details.SenderAddress,
            SenderTaxCode = details.SenderTaxCode,
            ReceiverCustomerId = details.ReceiverCustomerId,
            ReceiverName = details.ReceiverName,
            ReceiverPhone = details.ReceiverPhone,
            ReceiverAddress = details.ReceiverAddress,
            ReceiverTaxCode = details.ReceiverTaxCode,
            PickupAt = header.PickupAt,
            PickupAddress = details.PickupAddress,
            DeliveryAddress = details.DeliveryAddress,
            RouteId = header.RouteId,
            VehicleId = header.VehicleId,
            DriverId = header.DriverId,
            VehicleTypeId = details.VehicleTypeId,
            EmployeeId = details.EmployeeId,
            PaymentMethodId = details.PaymentMethodId,
            BillingYear = header.BillingYear,
            BillingMonth = header.BillingMonth,
            ArNumber = header.ArNumber,
            UnitPrice = header.UnitPrice,
            Surcharge = header.Surcharge,
            ExtraCost = header.ExtraCost,
            ApprovedExceptionRevenue = header.ApprovedExceptionRevenue,
            TotalAmount = header.TotalAmount,
            PriceListItemId = details.PriceListItemId,
            PriceSourceSnapshot = details.PriceSourceSnapshot,
            IsFreightManual = details.IsFreightManual,
            FreightOverrideReason = details.FreightOverrideReason,
            PartnerId = details.PartnerId,
            PartnerNameSnapshot = details.PartnerNameSnapshot,
            PartnerRateId = details.PartnerRateId,
            BuyUnitPrice = details.BuyUnitPrice,
            BuySurcharge = details.BuySurcharge,
            BuyExtraCost = details.BuyExtraCost,
            ApprovedExceptionCost = details.ApprovedExceptionCost,
            BuyTotal = details.BuyTotal,
            PartnerOperatingFeePercent = details.PartnerOperatingFeePercent,
            PartnerPayableAmount = details.PartnerPayableAmount,
            GrossMargin = details.GrossMargin,
            BuyRateSourceSnapshot = details.BuyRateSourceSnapshot,
            IsBuyManual = details.IsBuyManual,
            BuyOverrideReason = details.BuyOverrideReason,
            AmountInWords = details.AmountInWords,
            Notes = header.Notes,
            IsDeleted = details.IsDeleted,
            RowVersion = details.VersionToken.ToArray(),
            Stops = details.Stops.Select(x => new DispatchOrderStop
            {
                Id = x.Id,
                Sequence = x.Sequence,
                LocationId = x.LocationId,
                NameSnapshot = x.NameSnapshot,
            }).ToList(),
            Lines = details.Lines.Select(x => new DispatchOrderLine
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                GoodsName = x.GoodsName,
                PackageCount = x.PackageCount,
                Route = x.Route,
                Kilometers = x.Kilometers,
                Notes = x.Notes,
            }).ToList(),
        };
    }

    private static CustomerOption? ToCustomer(Customer? item) => item is null ? null : new CustomerOption(
        item.Id, item.Code, item.Name, item.Address, item.Phone, item.TaxCode, item.IsWalkIn,
        item.AccountantEmployeeId);
}
