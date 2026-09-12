using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Dispatching;

public sealed record DispatchOrderPrintDetails(
    DispatchOrderSummary Summary,
    DateTime CreatedAt,
    string? CreatedByName,
    CustomerOption? SenderCustomer,
    string? SenderName,
    string? SenderPhone,
    string? SenderAddress,
    string? PickupAddress,
    CustomerOption? ReceiverCustomer,
    string? ReceiverName,
    string? ReceiverPhone,
    string? ReceiverAddress,
    string? DeliveryAddress,
    IReadOnlyList<DispatchOrderLineDetails> Lines);
