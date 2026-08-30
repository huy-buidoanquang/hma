using Hma.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DocumentNumberService(IHmaDbContext db) : IDocumentNumberService
{
    private const int MaxAttempts = 3;

    public async Task<string> NextAsync(string key, CancellationToken cancellationToken = default)
    {
        var seq = await db.Sequences.FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
                  ?? throw new InvalidOperationException($"Thiếu dãy số '{key}'.");

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            seq.LastValue++;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return seq.LastValue.ToString("000");
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                await db.ReloadAsync(seq, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(ConcurrencyConflict.Message);
            }
        }

        throw new InvalidOperationException(ConcurrencyConflict.Message);
    }
}
