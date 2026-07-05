using System.Net;
using System.Xml.Linq;

namespace DanteConfigEditor.Models;

public sealed class DanteDevice
{
    internal DanteDevice(XElement element)
    {
        Element = element;
        Name = ReadElementValue(element, "name");
        FriendlyName = ReadElementValue(element, "friendly_name");
        IsRedundant = string.Equals(element.Element("redundancy")?.Attribute("value")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        PreferredMaster = string.Equals(element.Element("preferred_master")?.Attribute("value")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        Latency = ReadElementValue(element, "unicast_latency");
        UsesStaticIp = DetectStaticIp(element);
        StaticIpAddress = UsesStaticIp ? ReadIpAddress(element) : string.Empty;

        // L'index est basé sur l'ordre des balises dans le XML, ce qui
        // correspond à la numérotation affichée dans l'application.
        TxChannels = element.Elements("txchannel")
            .Select((channel, index) => new DanteChannel(Name, DanteChannelKind.Tx, index + 1, channel))
            .ToList();

        RxChannels = element.Elements("rxchannel")
            .Select((channel, index) => new DanteChannel(Name, DanteChannelKind.Rx, index + 1, channel))
            .ToList();
    }

    public string Name { get; }

    public string FriendlyName { get; }

    public bool IsRedundant { get; }

    public string NetworkMode => IsRedundant ? "Redondant" : "Daisychain";

    public string Latency { get; }

    public bool PreferredMaster { get; }

    public bool UsesStaticIp { get; }

    public string StaticIpAddress { get; }

    public int TxCount => TxChannels.Count;

    public int RxCount => RxChannels.Count;

    public IReadOnlyList<DanteChannel> TxChannels { get; }

    public IReadOnlyList<DanteChannel> RxChannels { get; }

    internal XElement Element { get; }

    private static string ReadElementValue(XElement element, string name)
    {
        return element.Element(name)?.Value?.Trim() ?? string.Empty;
    }

    private static bool DetectStaticIp(XElement element)
    {
        foreach (XElement candidate in element.DescendantsAndSelf())
        {
            if (IsStaticIpMarker(candidate.Name.LocalName, candidate.Value, BuildXmlPath(candidate)))
            {
                return true;
            }

            foreach (XAttribute attribute in candidate.Attributes())
            {
                if (IsStaticIpMarker(attribute.Name.LocalName, attribute.Value, BuildXmlPath(candidate)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsStaticIpMarker(string name, string value, string path)
    {
        string cleanName = name.Trim().ToLowerInvariant();
        string cleanValue = value.Trim().ToLowerInvariant();
        string cleanPath = path.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(cleanValue))
        {
            return false;
        }

        bool looksNetworkRelated = cleanName.Contains("ip", StringComparison.Ordinal)
            || cleanName.Contains("address", StringComparison.Ordinal)
            || cleanName.Contains("dhcp", StringComparison.Ordinal)
            || cleanPath.Contains("network", StringComparison.Ordinal)
            || cleanPath.Contains("interface", StringComparison.Ordinal);

        if (!looksNetworkRelated)
        {
            return false;
        }

        if (cleanName.Contains("static", StringComparison.Ordinal)
            && IPAddress.TryParse(cleanValue, out IPAddress? address)
            && address.AddressFamily is System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return true;
        }

        if (cleanName.Contains("dhcp", StringComparison.Ordinal)
            && (cleanValue is "false" or "0" or "off" or "no" or "disabled"))
        {
            return true;
        }

        bool staticValue = cleanValue is "static" or "manual" or "fixed" or "fixe" or "manuelle"
            || cleanValue.Contains("static", StringComparison.Ordinal)
            || cleanValue.Contains("manual", StringComparison.Ordinal)
            || cleanValue.Contains("fixed", StringComparison.Ordinal);

        if (!staticValue)
        {
            return false;
        }

        return cleanName.Contains("mode", StringComparison.Ordinal)
            || cleanName.Contains("method", StringComparison.Ordinal)
            || cleanName.Contains("config", StringComparison.Ordinal)
            || cleanName.Contains("type", StringComparison.Ordinal)
            || cleanName.Contains("static", StringComparison.Ordinal)
            || cleanPath.Contains("ip", StringComparison.Ordinal)
            || cleanPath.Contains("address", StringComparison.Ordinal);
    }

    private static string ReadIpAddress(XElement element)
    {
        List<(int Priority, string Value)> candidates = [];
        foreach (XElement candidate in element.Descendants())
        {
            AddIpCandidate(candidates, candidate.Name.LocalName, candidate.Value);

            foreach (XAttribute attribute in candidate.Attributes())
            {
                AddIpCandidate(candidates, attribute.Name.LocalName, attribute.Value);
            }
        }

        return candidates
            .OrderBy(candidate => candidate.Priority)
            .Select(candidate => candidate.Value)
            .FirstOrDefault() ?? string.Empty;
    }

    private static void AddIpCandidate(List<(int Priority, string Value)> candidates, string name, string value)
    {
        string cleanName = name.Trim().ToLowerInvariant();
        string cleanValue = value.Trim();

        if (cleanName.Contains("gateway", StringComparison.Ordinal)
            || cleanName.Contains("mask", StringComparison.Ordinal)
            || cleanName.Contains("subnet", StringComparison.Ordinal))
        {
            return;
        }

        if (!IPAddress.TryParse(cleanValue, out IPAddress? address)
            || address.AddressFamily is not System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return;
        }

        int priority = cleanName.Contains("static", StringComparison.Ordinal) ? 0
            : cleanName.Contains("ip", StringComparison.Ordinal) && cleanName.Contains("address", StringComparison.Ordinal) ? 1
            : cleanName is "ip" or "address" ? 2
            : 3;

        candidates.Add((priority, address.ToString()));
    }

    private static string BuildXmlPath(XElement element)
    {
        return string.Join("/", element.AncestorsAndSelf().Reverse().Select(ancestor => ancestor.Name.LocalName));
    }
}
