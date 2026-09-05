using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class SettingsService(IHmaDbContext db, ICurrentUser current)
{
    public async Task<SettingsModel> LoadAsync(CancellationToken ct = default)
    {
        var parameters = await db.Parameters.AsNoTracking().ToListAsync(ct);
        var sequences = await db.Sequences.AsNoTracking().ToListAsync(ct);
        string Value(string key, string fallback) =>
            parameters.FirstOrDefault(p => p.Key == key)?.Value ?? fallback;
        int Seq(string key) => sequences.FirstOrDefault(s => s.Key == key)?.LastValue ?? 0;

        return new SettingsModel
        {
            VatRate = Value("VatRate", "10"),
            DocumentStorePath = Value("DocumentStorePath", ""),
            DispatchOrderLastValue = Seq("dispatch-order"),
            FreightStatementLastValue = Seq("freight-statement")
        };
    }

    public async Task SaveAsync(SettingsModel model, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Update);
        if (!decimal.TryParse(model.VatRate, out var vat) || vat < 0 || vat > 100)
            throw new InvalidOperationException("VAT phải là số từ 0 đến 100.");
        if (model.DispatchOrderLastValue < 0 || model.FreightStatementLastValue < 0)
            throw new InvalidOperationException("Số đếm chứng từ không được âm.");

        await UpsertParameterAsync("VatRate", vat.ToString("0.##"), ct);
        await UpsertParameterAsync("DocumentStorePath", model.DocumentStorePath?.Trim() ?? "", ct);
        await UpsertSequenceAsync("dispatch-order", model.DispatchOrderLastValue, ct);
        await UpsertSequenceAsync("freight-statement", model.FreightStatementLastValue, ct);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    private async Task UpsertParameterAsync(string key, string value, CancellationToken ct)
    {
        var row = await db.Parameters.FirstOrDefaultAsync(p => p.Key == key, ct);
        if (row is null)
        {
            db.Add(new SystemParameter { Key = key, Value = value });
            return;
        }
        row.Value = value;
        db.Update(row);
    }

    private async Task UpsertSequenceAsync(string key, int lastValue, CancellationToken ct)
    {
        var row = await db.Sequences.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null)
        {
            db.Add(new DocumentSequence { Key = key, LastValue = lastValue });
            return;
        }
        row.LastValue = lastValue;
        db.Update(row);
    }
}
