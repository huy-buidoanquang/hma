using Hma.Domain.Models;

namespace Hma.Application.Features.Dispatching;

public sealed record DispatchFreightResult(DispatchOrderDetails Order, FreightQuote? Quote);
