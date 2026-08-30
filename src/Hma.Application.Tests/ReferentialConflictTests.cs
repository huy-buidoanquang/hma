using Microsoft.EntityFrameworkCore;
using Hma.Application.Services;

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
}
