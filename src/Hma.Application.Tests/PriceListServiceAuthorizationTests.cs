using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Pricing;
using Hma.Domain.Enums;

namespace Hma.Application.Tests;

public class PriceListServiceAuthorizationTests
{
    [Fact]
    public async Task Non_manager_cannot_change_price_fluctuation()
    {
        var db = Substitute.For<IHmaDbContext>();
        var current = Substitute.For<ICurrentUser>();
        current.IsManager.Returns(false);
        var service = new PriceListService(db, current, TimeProvider.System);
        var command = new SavePriceListFluctuationCommand(
            0,
            1,
            PriceFluctuationType.Percentage,
            10,
            new DateTime(2026, 9, 12),
            null,
            "Giá dầu tăng",
            []);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFluctuationAsync(command));

        Assert.Equal("Chỉ quản lý được điều chỉnh biến động giá.", error.Message);
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
