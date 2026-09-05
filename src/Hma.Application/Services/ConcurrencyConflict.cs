using System.Diagnostics.CodeAnalysis;
using Hma.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public static class ConcurrencyConflict
{
    public const string Message = "Dữ liệu đã bị người khác thay đổi. Tải lại rồi lưu lại.";

    [DoesNotReturn]
    public static void Throw() => throw new InvalidOperationException(Message);

    public static async Task SaveAsync(IHmaDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            Throw();
        }
        catch (Exception ex)
        {
            throw PersistenceGuard.Translate(ex);
        }
    }
}
