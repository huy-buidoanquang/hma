namespace Hma.Application.Features.Dispatching;

public sealed record DispatchStopDetails(int Id, int Sequence, int LocationId, string NameSnapshot);
