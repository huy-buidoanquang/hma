using Hma.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Common.Persistence;

public static class PersistenceGuard
{
    public static async Task SaveAsync(IHmaDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw Translate(ex);
        }
    }

    public static Exception Translate(Exception exception)
    {
        if (IsConcurrentContext(exception))
            return new InvalidOperationException(
                "Hệ thống đang xử lý thao tác trước. Thử lại sau giây lát.", exception);

        if (exception is DbUpdateConcurrencyException)
            return new InvalidOperationException(ConcurrencyConflict.Message, exception);

        if (exception is DbUpdateException dbEx)
        {
            if (ReferentialConflict.IsConstraint(dbEx))
                return new InvalidOperationException(ReferentialConflict.Message, dbEx);
            if (IsUnique(dbEx))
                return new InvalidOperationException(
                    "Giá trị bị trùng (mã, biển số hoặc tên đăng nhập).", dbEx);
            var leaf = Innermost(dbEx);
            var text = leaf.Message;
            if (string.IsNullOrWhiteSpace(text)
                || text.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase)
                || text.Contains("An error occurred while saving", StringComparison.OrdinalIgnoreCase))
                text = "Không lưu được dữ liệu. Kiểm tra thông tin bắt buộc rồi thử lại.";
            return new InvalidOperationException(text, dbEx);
        }

        return exception;
    }

    public static bool IsUnique(Exception exception)
    {
        for (var inner = (Exception?)exception; inner is not null; inner = inner.InnerException)
        {
            var m = inner.Message;
            if (m.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || m.Contains("unique index", StringComparison.OrdinalIgnoreCase)
                || m.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || m.Contains("Cannot insert duplicate", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsConcurrentContext(Exception exception)
    {
        for (var inner = (Exception?)exception; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("second operation was started", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static Exception Innermost(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
            current = current.InnerException;
        return current;
    }
}
