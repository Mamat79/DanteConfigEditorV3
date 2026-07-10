namespace DanteConfigEditor.Models;

public sealed record DeviceProfile(
    string Key,
    string Samplerate,
    string Encoding,
    string Latency,
    bool? IsRedundant,
    bool SetIpAutomatic);

public static class DeviceProfileCatalog
{
    public static IReadOnlyList<DeviceProfile> BuiltIn { get; } =
    [
        new("Profile.48k24b1msAuto", "48000", "24", "1000", null, true),
        new("Profile.48k24b2msAuto", "48000", "24", "2000", null, true),
        new("Profile.96k24b1msAuto", "96000", "24", "1000", null, true),
        new("Profile.96k24b2msAuto", "96000", "24", "2000", null, true),
        new("Profile.48k24b1msRedundant", "48000", "24", "1000", true, true),
        new("Profile.48k24b1msDaisychain", "48000", "24", "1000", false, true)
    ];
}
