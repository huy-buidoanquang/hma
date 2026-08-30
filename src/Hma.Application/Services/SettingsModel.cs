namespace Hma.Application.Services;

public sealed class SettingsModel
{
    public string VatRate { get; set; } = "10";
    public string DocumentStorePath { get; set; } = "";
    public int DispatchOrderLastValue { get; set; }
    public int FreightStatementLastValue { get; set; }
}
