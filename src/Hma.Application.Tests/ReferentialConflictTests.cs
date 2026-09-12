using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Hma.Domain.Entities;

namespace Hma.Application.Tests;

public class ReferentialConflictTests
{
    [Fact]
    public void IsConstraint_detects_sql_server_fk_message()
    {
        var inner = new InvalidOperationException(
            "The DELETE statement conflicted with the REFERENCE constraint \"FK_DispatchOrder_Customer\".");
        var ex = new DbUpdateException("failed", inner);
        Assert.True(ReferentialConflict.IsConstraint(ex));
    }

    [Fact]
    public void IsConstraint_ignores_unrelated_errors()
    {
        var ex = new DbUpdateException("timeout", new TimeoutException("expired"));
        Assert.False(ReferentialConflict.IsConstraint(ex));
    }

    [Fact]
    public async Task SaveAsync_reloads_deleted_entity_before_reporting_fk_conflict()
    {
        var db = Substitute.For<IHmaDbContext>();
        var entity = new Location { Id = 12, Code = "USED", Name = "Điểm đang dùng" };
        var failure = new DbUpdateException(
            "failed",
            new InvalidOperationException("The DELETE statement conflicted with the REFERENCE constraint."));
        db.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(failure));
        db.ReloadAsync(entity, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ReferentialConflict.SaveAsync(db, entity));

        Assert.Equal(ReferentialConflict.Message, error.Message);
        Assert.Same(failure, error.InnerException);
        await db.Received(1).ReloadAsync(entity, Arg.Any<CancellationToken>());
    }
}
