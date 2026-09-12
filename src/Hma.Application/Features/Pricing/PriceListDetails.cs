namespace Hma.Application.Features.Pricing;

public sealed record PriceListDetails(PriceListSummary Header, int? CurrentRevisionId, IReadOnlyList<PriceListItemSummary> Items);
