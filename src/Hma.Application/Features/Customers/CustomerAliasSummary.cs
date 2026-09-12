namespace Hma.Application.Features.Customers;

public sealed record CustomerAliasSummary(int Id, string Alias, int CustomerId, CustomerOption? Customer);
