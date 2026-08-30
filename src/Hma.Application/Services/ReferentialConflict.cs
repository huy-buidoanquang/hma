using Hma.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public static class ReferentialConflict
{
    public const string Message = "Không xóa được vì đang được lệnh điều xe hoặc danh mục khác sử dụng.";

    public static bool IsConstraint(DbUpdateException exception)
    {
        for (var inner = (Exception?)exception; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)
                || inner.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                || inner.Message.Contains("conflicted with the REFERENCE", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static async Task SaveAsync(IHmaDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsConstraint(ex))
        {
            throw new InvalidOperationException(Message, ex);
        }
    }
}
