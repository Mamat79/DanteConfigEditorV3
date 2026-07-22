using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed partial class DanteProject
{
    public void SetAllNetworkModes(bool redundant)
    {
        foreach (DanteDevice device in Devices)
        {
            SetBooleanElementAttribute(device.Element, "redundancy", "value", redundant, afterElementName: "friendly_name");
        }

        RegisterChange("Mode réseau global", redundant ? "Tous redondants" : "Tous en daisychain");
    }

    public void SetAllLatencies(string latency)
    {
        ValidateLatency(latency);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "unicast_latency", latency);
        }

        RegisterChange("Latence globale", $"Tous -> {DanteLatencyFormatter.FormatLatencyWithXmlValue(latency)}");
    }

    public void SetAllSamplerates(string samplerate)
    {
        string cleanSamplerate = ValidateSamplerate(samplerate);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "samplerate", cleanSamplerate);
        }

        RegisterChange("Sample rate globale", $"Tous -> {FormatSamplerateForDisplay(cleanSamplerate)}");
    }

    public void SetAllEncodings(string encoding)
    {
        string cleanEncoding = ValidateEncoding(encoding);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "encoding", cleanEncoding);
        }

        RegisterChange("Bits par échantillon globaux", $"Tous -> {FormatEncodingForDisplay(cleanEncoding)}");
    }

    public int SetAllIpAddressesDynamic()
    {
        int changedDevices = 0;
        foreach (DanteDevice device in Devices)
        {
            if (SetDeviceIpAddressesDynamic(device))
            {
                changedDevices++;
            }
        }

        RegisterChange("IP automatique globale", $"{changedDevices} machine(s) passée(s) en dynamique");
        return changedDevices;
    }

    public void SetAllIpAddressesStaticSequential(string prefix, int startHost, string netmask, string gateway)
    {
        string cleanPrefix = ValidateIpv4Prefix(prefix);
        string cleanNetmask = ValidateIpv4Address(string.IsNullOrWhiteSpace(netmask) ? "255.255.255.0" : netmask, "masque");
        string cleanGateway = ValidateIpv4Address(string.IsNullOrWhiteSpace(gateway) ? "0.0.0.0" : gateway, "passerelle");

        if (startHost < 1 || startHost > 254)
        {
            throw new InvalidOperationException("Le premier numéro IP doit être compris entre 1 et 254.");
        }

        DanteDevice[] configurableDevices = Devices.Where(DeviceSupportsIpConfiguration).ToArray();
        if (configurableDevices.Length == 0)
        {
            throw new InvalidOperationException("Aucune machine du XML ne contient d'interface IPv4 modifiable.");
        }

        if (startHost + configurableDevices.Length - 1 > 254)
        {
            throw new InvalidOperationException("La plage IP dépasse 254. Choisissez un numéro de départ plus bas.");
        }

        int host = startHost;
        foreach (DanteDevice device in configurableDevices)
        {
            SetDeviceIpAddressStatic(device, $"{cleanPrefix}.{host}", cleanNetmask, cleanGateway);
            host++;
        }

        int skipped = Devices.Count - configurableDevices.Length;
        RegisterChange("IP fixes globales", $"{configurableDevices.Length} machine(s) depuis {cleanPrefix}.{startHost}, {skipped} ignorée(s) sans interface IPv4");
    }

    public void ResetAllChannels()
    {
        foreach (DanteDevice device in Devices)
        {
            ResetDeviceChannels(device);
        }

        RegisterChange("Canaux global", "Réinitialisation des canaux de tous les devices");
    }

    public void SetExclusivePreferredMaster(string deviceName)
    {
        DanteDevice selected = FindDevice(deviceName)
            ?? throw new InvalidOperationException("La machine choisie est introuvable.");

        foreach (DanteDevice device in Devices)
        {
            SetBooleanElementAttribute(
                device.Element,
                "preferred_master",
                "value",
                ReferenceEquals(device, selected),
                afterElementName: "redundancy");
        }

        RegisterChange("Preferred master exclusif", $"{selected.Name} est le seul Preferred Master");
    }

    public int ApplyDeviceProfile(IEnumerable<string> deviceNames, DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidateLatency(profile.Latency);
        string samplerate = ValidateSamplerate(profile.Samplerate);
        string encoding = ValidateEncoding(profile.Encoding);
        HashSet<string> requestedNames = deviceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        DanteDevice[] targets = Devices.Where(device => requestedNames.Contains(device.Name)).ToArray();
        if (targets.Length == 0)
        {
            throw new InvalidOperationException("Aucune machine ne correspond à la cible du profil.");
        }

        int changedDevices = 0;
        foreach (DanteDevice device in targets)
        {
            bool changed = false;
            if (!string.Equals(device.Samplerate, samplerate, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "samplerate", samplerate);
                changed = true;
            }

            if (!string.Equals(device.Encoding, encoding, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "encoding", encoding);
                changed = true;
            }

            if (!string.Equals(device.Latency, profile.Latency, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "unicast_latency", profile.Latency);
                changed = true;
            }

            if (profile.IsRedundant.HasValue && device.IsRedundant != profile.IsRedundant.Value)
            {
                SetBooleanElementAttribute(device.Element, "redundancy", "value", profile.IsRedundant.Value, afterElementName: "friendly_name");
                changed = true;
            }

            if (profile.SetIpAutomatic && SetDeviceIpAddressesDynamic(device))
            {
                changed = true;
            }

            if (changed)
            {
                changedDevices++;
            }
        }

        if (changedDevices > 0)
        {
            RegisterChange("Profil rapide", $"{profile.Key} appliqué à {changedDevices} machine(s)");
        }

        return changedDevices;
    }
}
