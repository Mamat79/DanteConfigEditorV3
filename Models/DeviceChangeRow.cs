namespace DanteConfigEditor.Models;

public sealed record DeviceChangeRow(
    string DeviceName,
    string Parameter,
    string Before,
    string After,
    string Status,
    bool HasCurrentDevice);
