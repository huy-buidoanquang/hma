namespace Hma.Application.Services;

public sealed class DispatchImportRow
{
    public int ExcelRow { get; init; }
    public int StartColumn { get; init; }
    public string? SheetName { get; init; }
    public string? BlockLabel { get; init; }
    public string? Stt { get; init; }
    public string? Route { get; init; }
    public string? Code { get; init; }
    public string? PickupAt { get; init; }
    public string? CustomerCode { get; init; }
    public string? PickupCity { get; init; }
    public string? DeliveryCity { get; init; }
    public string? Plate { get; init; }
    public string? Tonnage { get; init; }
    public string? Freight { get; init; }
    public string? ExtraCost { get; init; }
    public string? Notes { get; init; }
    public string? PaymentMethod { get; init; }
    public string? DriverName { get; init; }
}
