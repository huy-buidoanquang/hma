using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Abstractions.Storage;
using Hma.Application.Features.Dispatching;
using NSubstitute;

namespace Hma.Application.Tests;

public class DispatchDocumentServiceTests
{
    [Fact]
    public async Task Open_read_delegates_the_storage_key_to_the_stream_based_port()
    {
        var storage = Substitute.For<IFileStorage>();
        var expected = new MemoryStream([1, 2, 3]);
        storage.OpenReadAsync("12/delivery.pdf", Arg.Any<CancellationToken>()).Returns(expected);
        var service = new DispatchDocumentService(
            Substitute.For<IHmaDbContext>(),
            Substitute.For<ICurrentUser>(),
            storage,
            TimeProvider.System);

        var actual = await service.OpenReadAsync("12/delivery.pdf");

        Assert.Same(expected, actual);
        await storage.Received(1).OpenReadAsync("12/delivery.pdf", Arg.Any<CancellationToken>());
    }
}
