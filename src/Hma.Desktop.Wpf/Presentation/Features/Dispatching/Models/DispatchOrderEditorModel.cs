using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Dispatching;
using System.Collections.ObjectModel;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public sealed partial class DispatchOrderEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private DateTime createdAt = DateTime.Now;
    [ObservableProperty] private int? createdByUserId;
    [ObservableProperty] private UserOption? createdByUser;
    [ObservableProperty] private DispatchStatus status = DispatchStatus.Issued;
    [ObservableProperty] private ReconciliationStatus reconciliationStatus = ReconciliationStatus.Pending;
    [ObservableProperty] private DateTime? confirmedAt;
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private CustomerOption? customer;
    [ObservableProperty] private int? senderCustomerId;
    [ObservableProperty] private CustomerOption? senderCustomer;
    [ObservableProperty] private string? senderName;
    [ObservableProperty] private string? senderPhone;
    [ObservableProperty] private string? senderAddress;
    [ObservableProperty] private string? senderTaxCode;
    [ObservableProperty] private int? receiverCustomerId;
    [ObservableProperty] private CustomerOption? receiverCustomer;
    [ObservableProperty] private string? receiverName;
    [ObservableProperty] private string? receiverPhone;
    [ObservableProperty] private string? receiverAddress;
    [ObservableProperty] private string? receiverTaxCode;
    [ObservableProperty] private DateTime pickupAt = DateTime.Now;
    [ObservableProperty] private string? pickupAddress;
    [ObservableProperty] private string? deliveryAddress;
    [ObservableProperty] private int? routeId;
    [ObservableProperty] private int? vehicleId;
    [ObservableProperty] private VehicleOption? vehicle;
    [ObservableProperty] private int? driverId;
    [ObservableProperty] private int? vehicleTypeId;
    [ObservableProperty] private VehicleTypeOption? vehicleType;
    [ObservableProperty] private int? employeeId;
    [ObservableProperty] private int? paymentMethodId;
    [ObservableProperty] private int billingYear;
    [ObservableProperty] private int billingMonth;
    [ObservableProperty] private string? arNumber;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal surcharge;
    [ObservableProperty] private decimal extraCost;
    [ObservableProperty] private decimal approvedExceptionRevenue;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private int? priceListItemId;
    [ObservableProperty] private int? priceListFluctuationId;
    [ObservableProperty] private string? priceListCode;
    [ObservableProperty] private string? priceSourceSnapshot;
    [ObservableProperty] private bool isFreightManual;
    [ObservableProperty] private string? freightOverrideReason;
    [ObservableProperty] private int? partnerId;
    [ObservableProperty] private string? partnerNameSnapshot;
    [ObservableProperty] private int? partnerRateId;
    [ObservableProperty] private decimal buyUnitPrice;
    [ObservableProperty] private decimal buySurcharge;
    [ObservableProperty] private decimal buyExtraCost;
    [ObservableProperty] private decimal approvedExceptionCost;
    [ObservableProperty] private decimal buyTotal;
    [ObservableProperty] private decimal partnerOperatingFeePercent;
    [ObservableProperty] private decimal partnerPayableAmount;
    [ObservableProperty] private decimal grossMargin;
    [ObservableProperty] private string? buyRateSourceSnapshot;
    [ObservableProperty] private bool isBuyManual;
    [ObservableProperty] private string? buyOverrideReason;
    [ObservableProperty] private string? amountInWords;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isDeleted;

    public ObservableCollection<DispatchStopModel> Stops { get; } = [];
    public ObservableCollection<DispatchOrderLineModel> Lines { get; } = [];

    public bool CanEdit => !IsDeleted
        && Status is not DispatchStatus.Locked and not DispatchStatus.Cancelled
        && ConfirmedAt is null
        && ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected;

    public static DispatchOrderEditorModel From(DispatchOrderDetails item)
    {
        var header = item.Header;
        var model = new DispatchOrderEditorModel
        {
            Id = header.Id,
            Code = header.Code,
            CreatedAt = item.CreatedAt,
            CreatedByUserId = item.CreatedByUserId,
            CreatedByUser = header.CreatedByUser,
            Status = header.Status,
            ReconciliationStatus = header.ReconciliationStatus,
            ConfirmedAt = item.ConfirmedAt,
            CustomerId = item.CustomerId,
            Customer = header.Customer,
            SenderCustomerId = item.SenderCustomerId,
            SenderCustomer = item.SenderCustomer,
            SenderName = item.SenderName,
            SenderPhone = item.SenderPhone,
            SenderAddress = item.SenderAddress,
            SenderTaxCode = item.SenderTaxCode,
            ReceiverCustomerId = item.ReceiverCustomerId,
            ReceiverCustomer = item.ReceiverCustomer,
            ReceiverName = item.ReceiverName,
            ReceiverPhone = item.ReceiverPhone,
            ReceiverAddress = item.ReceiverAddress,
            ReceiverTaxCode = item.ReceiverTaxCode,
            PickupAt = header.PickupAt,
            PickupAddress = item.PickupAddress,
            DeliveryAddress = item.DeliveryAddress,
            RouteId = header.RouteId,
            VehicleId = header.VehicleId,
            Vehicle = header.Vehicle,
            DriverId = header.DriverId,
            VehicleTypeId = item.VehicleTypeId,
            VehicleType = header.VehicleType,
            EmployeeId = item.EmployeeId,
            PaymentMethodId = item.PaymentMethodId,
            BillingYear = header.BillingYear,
            BillingMonth = header.BillingMonth,
            ArNumber = header.ArNumber,
            UnitPrice = header.UnitPrice,
            Surcharge = header.Surcharge,
            ExtraCost = header.ExtraCost,
            ApprovedExceptionRevenue = header.ApprovedExceptionRevenue,
            TotalAmount = header.TotalAmount,
            PriceListItemId = item.PriceListItemId,
            PriceListFluctuationId = item.PriceListFluctuationId,
            PriceListCode = item.PriceListCode,
            PriceSourceSnapshot = item.PriceSourceSnapshot,
            IsFreightManual = item.IsFreightManual,
            FreightOverrideReason = item.FreightOverrideReason,
            PartnerId = item.PartnerId,
            PartnerNameSnapshot = item.PartnerNameSnapshot,
            PartnerRateId = item.PartnerRateId,
            BuyUnitPrice = item.BuyUnitPrice,
            BuySurcharge = item.BuySurcharge,
            BuyExtraCost = item.BuyExtraCost,
            ApprovedExceptionCost = item.ApprovedExceptionCost,
            BuyTotal = item.BuyTotal,
            PartnerOperatingFeePercent = item.PartnerOperatingFeePercent,
            PartnerPayableAmount = item.PartnerPayableAmount,
            GrossMargin = item.GrossMargin,
            BuyRateSourceSnapshot = item.BuyRateSourceSnapshot,
            IsBuyManual = item.IsBuyManual,
            BuyOverrideReason = item.BuyOverrideReason,
            AmountInWords = item.AmountInWords,
            Notes = header.Notes,
            IsDeleted = item.IsDeleted,
        };
        foreach (var stop in item.Stops.OrderBy(x => x.Sequence)) model.Stops.Add(DispatchStopModel.From(stop));
        foreach (var line in item.Lines.OrderBy(x => x.LineNumber)) model.Lines.Add(DispatchOrderLineModel.From(line));
        return model;
    }

    public SaveDispatchOrderCommand ToCommand(byte[] versionToken)
    {
        var header = new DispatchOrderSummary
        {
            Id = Id,
            Code = Code,
            PickupAt = PickupAt,
            Status = Status,
            ReconciliationStatus = ReconciliationStatus,
            Customer = Customer,
            RouteId = RouteId,
            DriverId = DriverId,
            VehicleId = VehicleId,
            Vehicle = Vehicle,
            VehicleType = VehicleType,
            UnitPrice = UnitPrice,
            Surcharge = Surcharge,
            ExtraCost = ExtraCost,
            ApprovedExceptionRevenue = ApprovedExceptionRevenue,
            TotalAmount = TotalAmount,
            BillingYear = BillingYear,
            BillingMonth = BillingMonth,
            Notes = Notes,
            SenderName = SenderName,
            PartnerNameSnapshot = PartnerNameSnapshot,
            PartnerOperatingFeePercent = PartnerOperatingFeePercent,
            PartnerPayableAmount = PartnerPayableAmount,
            ArNumber = ArNumber,
            CreatedByUser = CreatedByUser,
            CanEdit = CanEdit,
        };
        return new SaveDispatchOrderCommand(new DispatchOrderDetails(
            header, CreatedAt, CreatedByUserId, ConfirmedAt, CustomerId, SenderCustomerId,
            SenderCustomer, SenderName, SenderPhone, SenderAddress, SenderTaxCode,
            ReceiverCustomerId, ReceiverCustomer, ReceiverName, ReceiverPhone, ReceiverAddress,
            ReceiverTaxCode, PickupAddress, DeliveryAddress, VehicleTypeId, EmployeeId,
            PaymentMethodId, PriceListItemId, PriceListFluctuationId, PriceSourceSnapshot, IsFreightManual,
            FreightOverrideReason, PartnerId, PartnerNameSnapshot, PartnerRateId, BuyUnitPrice,
            BuySurcharge, BuyExtraCost, ApprovedExceptionCost, BuyTotal, PartnerOperatingFeePercent,
            PartnerPayableAmount, GrossMargin, BuyRateSourceSnapshot, IsBuyManual, BuyOverrideReason,
            AmountInWords, IsDeleted, versionToken.ToArray(), Stops.Select(x => x.ToDetails()).ToList(),
            Lines.Select(x => x.ToDetails()).ToList(), [])
        {
            PriceListCode = PriceListCode
        });
    }

    public void ApplyCalculatedFields(DispatchOrderDetails item)
    {
        UnitPrice = item.Header.UnitPrice;
        Surcharge = item.Header.Surcharge;
        ExtraCost = item.Header.ExtraCost;
        ApprovedExceptionRevenue = item.Header.ApprovedExceptionRevenue;
        TotalAmount = item.Header.TotalAmount;
        PriceListItemId = item.PriceListItemId;
        PriceListFluctuationId = item.PriceListFluctuationId;
        PriceListCode = item.PriceListCode;
        PriceSourceSnapshot = item.PriceSourceSnapshot;
        IsFreightManual = item.IsFreightManual;
        FreightOverrideReason = item.FreightOverrideReason;
        PartnerId = item.PartnerId;
        PartnerNameSnapshot = item.PartnerNameSnapshot;
        PartnerRateId = item.PartnerRateId;
        BuyUnitPrice = item.BuyUnitPrice;
        BuySurcharge = item.BuySurcharge;
        BuyExtraCost = item.BuyExtraCost;
        ApprovedExceptionCost = item.ApprovedExceptionCost;
        BuyTotal = item.BuyTotal;
        PartnerOperatingFeePercent = item.PartnerOperatingFeePercent;
        PartnerPayableAmount = item.PartnerPayableAmount;
        GrossMargin = item.GrossMargin;
        BuyRateSourceSnapshot = item.BuyRateSourceSnapshot;
        IsBuyManual = item.IsBuyManual;
        BuyOverrideReason = item.BuyOverrideReason;
        AmountInWords = item.AmountInWords;
    }

}
