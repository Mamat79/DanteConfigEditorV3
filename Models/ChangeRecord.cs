namespace DanteConfigEditor.Models;

public sealed record ChangeRecord(DateTime Timestamp, string Action, string Details)
{
    public string Display => $"{Timestamp:HH:mm:ss} - {Action} - {Details}";
}
