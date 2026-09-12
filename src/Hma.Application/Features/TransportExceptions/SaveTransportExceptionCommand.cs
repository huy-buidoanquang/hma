namespace Hma.Application.Features.TransportExceptions;

public sealed record SaveTransportExceptionCommand(
    int Id,
    int DispatchOrderId,
    int TransportExceptionCodeId,
    DateTime OccurredAt,
    string Description,
    decimal CustomerCharge,
    decimal PartnerCost,
    byte[] VersionToken);
