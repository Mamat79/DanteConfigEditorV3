namespace DanteConfigEditor.Models;

public sealed record DanteImportantWarning(
    string Key,
    string Message,
    IReadOnlyList<string> DeviceNames,
    string EnglishMessage = "")
{
    public int DeviceCount => DeviceNames.Count;

    public string LocalizedMessage(bool english)
    {
        return english && !string.IsNullOrWhiteSpace(EnglishMessage) ? EnglishMessage : Message;
    }

    public string DevicesDisplay => DeviceNames.Count == 0
        ? "Aucune machine ciblée"
        : string.Join(", ", DeviceNames.Take(8))
            + (DeviceNames.Count > 8 ? $", +{DeviceNames.Count - 8} autre(s)" : string.Empty);
}
