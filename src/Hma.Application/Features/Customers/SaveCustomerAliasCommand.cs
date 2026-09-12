namespace Hma.Application.Features.Customers;

public sealed record SaveCustomerAliasCommand(int Id, string Alias, int CustomerId);
