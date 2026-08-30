namespace Hma.Domain.Entities;

public class DocumentSequence
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public int LastValue { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
