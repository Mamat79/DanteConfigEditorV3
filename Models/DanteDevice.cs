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

    public int TxCount => TxChannels.Count;

    public int RxCount => RxChannels.Count;

    public IReadOnlyList<DanteChannel> TxChannels { get; }

    public IReadOnlyList<DanteChannel> RxChannels { get; }

    internal XElement Element { get; }

    private static string ReadElementValue(XElement element, string name)
    {
        return element.Element(name)?.Value?.Trim() ?? string.Empty;
    }
}
