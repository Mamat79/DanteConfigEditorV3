using System.Xml.Linq;

namespace DanteConfigEditor.Services;

public sealed class DanteXmlCompatibilityProfile
{
    public string RootName { get; init; } = string.Empty;

    public string? PresetVersion { get; init; }

    public string? DeclarationVersion { get; init; }

    public string? DeclarationEncoding { get; init; }

    public string? DeclarationStandalone { get; init; }

    public IReadOnlyList<DanteDeviceXmlSignature> Devices { get; init; } = [];
}

public sealed class DanteDeviceXmlSignature
{
    public int Position { get; init; }

    public string Name { get; init; } = string.Empty;

    public IReadOnlySet<string> TechnicalElements { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<DanteChannelXmlSignature> TxChannels { get; init; } = [];

    public IReadOnlyList<DanteChannelXmlSignature> RxChannels { get; init; } = [];
}

public sealed class DanteChannelXmlSignature
{
    public int Position { get; init; }

    public string? DanteId { get; init; }

    public string? MediaType { get; init; }
}

public static class DanteXmlCompatibilityService
{
    private static readonly string[] TechnicalDeviceElements =
    [
        "captureInfo",
        "instance_id",
        "device_id",
        "process_id",
        "manufacturer_id",
        "manufacturer_name",
        "model_id",
        "model_name",
        "model_version",
        "device_type",
        "device_type_string",
        "samplerate",
        "encoding",
        "unicast_latency",
        "clock",
        "clock_priority",
        "rtp",
        "interface"
    ];

    public static DanteXmlCompatibilityProfile CaptureProfile(XDocument document)
    {
        XElement? root = document.Root;
        return new DanteXmlCompatibilityProfile
        {
            RootName = root?.Name.LocalName ?? string.Empty,
            PresetVersion = root?.Attribute("version")?.Value,
            DeclarationVersion = document.Declaration?.Version,
            DeclarationEncoding = document.Declaration?.Encoding,
            DeclarationStandalone = document.Declaration?.Standalone,
            Devices = root?.Elements("device")
                .Select((device, index) => CaptureDevice(device, index + 1))
                .ToArray() ?? []
        };
    }

    public static DanteValidationResult ValidateCompatibility(XDocument document, DanteXmlCompatibilityProfile originalProfile)
    {
        DanteValidationResult result = new();
        XElement? root = document.Root;
        if (root is null)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, "Le document XML ne contient plus de racine.");
            return result;
        }

        if (!string.Equals(root.Name.LocalName, "preset", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, "La racine XML doit rester <preset> pour un preset Dante Controller.");
        }

        if (!string.IsNullOrWhiteSpace(originalProfile.PresetVersion)
            && !string.Equals(root.Attribute("version")?.Value, originalProfile.PresetVersion, StringComparison.OrdinalIgnoreCase))
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, $"La version du preset doit rester {originalProfile.PresetVersion}.");
        }

        ValidateDeclaration(document, originalProfile, result);

        XElement[] currentDevices = root.Elements("device").ToArray();
        if (currentDevices.Length == 0)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, "Aucun device Dante n'est présent dans le XML.");
            return result;
        }

        if (currentDevices.Length != originalProfile.Devices.Count)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, $"Le nombre de devices a changé : {originalProfile.Devices.Count} attendu(s), {currentDevices.Length} trouvé(s).");
        }

        int maxDevices = Math.Min(currentDevices.Length, originalProfile.Devices.Count);
        for (int index = 0; index < maxDevices; index++)
        {
            ValidateDevice(currentDevices[index], originalProfile.Devices[index], result);
        }

        return result;
    }

    private static DanteDeviceXmlSignature CaptureDevice(XElement device, int position)
    {
        return new DanteDeviceXmlSignature
        {
            Position = position,
            Name = device.Element("name")?.Value.Trim() ?? $"Device {position}",
            TechnicalElements = device.Elements()
                .Select(element => element.Name.LocalName)
                .Where(name => TechnicalDeviceElements.Contains(name, StringComparer.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            TxChannels = device.Elements("txchannel")
                .Select((channel, index) => CaptureChannel(channel, index + 1))
                .ToArray(),
            RxChannels = device.Elements("rxchannel")
                .Select((channel, index) => CaptureChannel(channel, index + 1))
                .ToArray()
        };
    }

    private static DanteChannelXmlSignature CaptureChannel(XElement channel, int position)
    {
        return new DanteChannelXmlSignature
        {
            Position = position,
            DanteId = channel.Attribute("danteId")?.Value,
            MediaType = channel.Attribute("mediaType")?.Value
        };
    }

    private static void ValidateDeclaration(XDocument document, DanteXmlCompatibilityProfile originalProfile, DanteValidationResult result)
    {
        if (document.Declaration is null && (!string.IsNullOrWhiteSpace(originalProfile.DeclarationVersion)
            || !string.IsNullOrWhiteSpace(originalProfile.DeclarationEncoding)
            || !string.IsNullOrWhiteSpace(originalProfile.DeclarationStandalone)))
        {
            result.AddWarning(DanteIssueCategory.XmlCompatibility, "La déclaration XML d'origine n'est plus présente.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(originalProfile.DeclarationVersion)
            && !string.Equals(document.Declaration?.Version, originalProfile.DeclarationVersion, StringComparison.OrdinalIgnoreCase))
        {
            result.AddWarning(DanteIssueCategory.XmlCompatibility, $"La version de déclaration XML d'origine ({originalProfile.DeclarationVersion}) n'est pas conservée.");
        }

        if (!string.IsNullOrWhiteSpace(originalProfile.DeclarationEncoding)
            && !string.Equals(document.Declaration?.Encoding, originalProfile.DeclarationEncoding, StringComparison.OrdinalIgnoreCase))
        {
            result.AddWarning(DanteIssueCategory.XmlCompatibility, $"L'encodage XML d'origine ({originalProfile.DeclarationEncoding}) n'est pas conservé.");
        }

        if (!string.IsNullOrWhiteSpace(originalProfile.DeclarationStandalone)
            && !string.Equals(document.Declaration?.Standalone, originalProfile.DeclarationStandalone, StringComparison.OrdinalIgnoreCase))
        {
            result.AddWarning(DanteIssueCategory.XmlCompatibility, $"Le standalone XML d'origine ({originalProfile.DeclarationStandalone}) n'est pas conservé.");
        }
    }

    private static void ValidateDevice(XElement currentDevice, DanteDeviceXmlSignature originalDevice, DanteValidationResult result)
    {
        string currentName = currentDevice.Element("name")?.Value.Trim() ?? originalDevice.Name;
        foreach (string technicalElement in originalDevice.TechnicalElements)
        {
            if (currentDevice.Element(technicalElement) is null)
            {
                result.AddError(DanteIssueCategory.XmlCompatibility, $"Balise technique supprimée : <{technicalElement}>.", currentName);
            }
        }

        ValidateChannels(currentDevice, "txchannel", originalDevice.TxChannels, currentName, "TX", result);
        ValidateChannels(currentDevice, "rxchannel", originalDevice.RxChannels, currentName, "RX", result);
        ValidatePatchPairs(currentDevice, currentName, result);
    }

    private static void ValidateChannels(
        XElement device,
        string elementName,
        IReadOnlyList<DanteChannelXmlSignature> originalChannels,
        string deviceName,
        string channelKind,
        DanteValidationResult result)
    {
        XElement[] currentChannels = device.Elements(elementName).ToArray();
        if (currentChannels.Length != originalChannels.Count)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, $"{deviceName} : nombre de canaux {channelKind} modifié ({originalChannels.Count} attendu(s), {currentChannels.Length} trouvé(s)).", deviceName);
        }

        HashSet<string> seenDanteIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (XElement channel in currentChannels)
        {
            string? danteId = channel.Attribute("danteId")?.Value;
            if (!string.IsNullOrWhiteSpace(danteId) && !seenDanteIds.Add(danteId))
            {
                result.AddError(DanteIssueCategory.Channel, $"{deviceName} : Dante Id {danteId} en doublon sur les canaux {channelKind}.", deviceName, danteId: ParseDanteId(danteId));
            }
        }

        int maxChannels = Math.Min(currentChannels.Length, originalChannels.Count);
        for (int index = 0; index < maxChannels; index++)
        {
            XElement currentChannel = currentChannels[index];
            DanteChannelXmlSignature originalChannel = originalChannels[index];
            string? currentDanteId = currentChannel.Attribute("danteId")?.Value;
            string? currentMediaType = currentChannel.Attribute("mediaType")?.Value;

            if (!string.IsNullOrWhiteSpace(originalChannel.DanteId)
                && !string.Equals(currentDanteId, originalChannel.DanteId, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError(DanteIssueCategory.XmlCompatibility, $"{deviceName} : Dante Id supprimé ou modifié sur {channelKind} position {originalChannel.Position}.", deviceName, danteId: ParseDanteId(originalChannel.DanteId));
            }

            if (!string.IsNullOrWhiteSpace(originalChannel.MediaType)
                && !string.Equals(currentMediaType, originalChannel.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError(DanteIssueCategory.XmlCompatibility, $"{deviceName} : mediaType supprimé ou modifié sur {channelKind} {Blank(originalChannel.DanteId)}.", deviceName, danteId: ParseDanteId(originalChannel.DanteId));
            }
        }
    }

    private static void ValidatePatchPairs(XElement device, string deviceName, DanteValidationResult result)
    {
        foreach (XElement rxChannel in device.Elements("rxchannel"))
        {
            XElement? subscribedChannel = rxChannel.Element("subscribed_channel");
            XElement? subscribedDevice = rxChannel.Element("subscribed_device");
            bool hasChannel = !string.IsNullOrWhiteSpace(subscribedChannel?.Value);
            bool hasDevice = !string.IsNullOrWhiteSpace(subscribedDevice?.Value);
            int? danteId = ParseDanteId(rxChannel.Attribute("danteId")?.Value);

            if (hasChannel && !hasDevice)
            {
                result.AddError(DanteIssueCategory.Patch, $"{deviceName} RX Dante Id {danteId?.ToString() ?? "?"} : subscribed_channel renseigné sans subscribed_device.", deviceName, danteId: danteId);
            }

            if (hasDevice && !hasChannel)
            {
                result.AddError(DanteIssueCategory.Patch, $"{deviceName} RX Dante Id {danteId?.ToString() ?? "?"} : subscribed_device renseigné sans subscribed_channel.", deviceName, danteId: danteId);
            }
        }
    }

    private static int? ParseDanteId(string? value)
    {
        return int.TryParse(value, out int danteId) ? danteId : null;
    }

    private static string Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "sans Dante Id" : value;
    }
}
