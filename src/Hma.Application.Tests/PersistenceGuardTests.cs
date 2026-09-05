using Hma.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Tests;

public class PersistenceGuardTests
{
    [Fact]
    public void Translate_concurrent_context_to_vietnamese()
    {
        var ex = new InvalidOperationException(
            "A second operation was started on this context instance before a previous operation completed.");
        var translated = PersistenceGuard.Translate(ex);
        Assert.Contains("thao tác trước", translated.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Translate_unique_constraint_to_vietnamese()
    {
        var inner = new InvalidOperationException(
            "Cannot insert duplicate key row in object 'dbo.City' with unique index 'UQ_City_Code'.");
        var ex = new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", inner);
        var translated = PersistenceGuard.Translate(ex);
        Assert.Contains("trùng", translated.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Translate_fk_to_referential_message()
    {
        var inner = new InvalidOperationException(
            "The DELETE statement conflicted with the REFERENCE constraint \"FK_DispatchOrder_Customer\".");
        var ex = new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", inner);
        var translated = PersistenceGuard.Translate(ex);
        Assert.Equal(ReferentialConflict.Message, translated.Message);
    }

    [Fact]
    public void Translate_generic_save_uses_inner_sql_text()
    {
        var inner = new InvalidOperationException("String or binary data would be truncated.");
        var ex = new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", inner);
        var translated = PersistenceGuard.Translate(ex);
        Assert.Contains("truncated", translated.Message, StringComparison.OrdinalIgnoreCase);
    }
}
