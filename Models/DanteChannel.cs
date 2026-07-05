using System.Xml.Linq;

namespace DanteConfigEditor.Models;

public sealed class DanteChannel
{
    internal DanteChannel(string deviceName, DanteChannelKind kind, int index, XElement element)
    {
        DeviceName = deviceName;
        Kind = kind;
        Index = index;
        Element = element;
        ChannelNameReadResult nameReadResult = ReadChannelName(kind, index, element);
        Name = nameReadResult.Value;
        NameSource = nameReadResult.SourceName;
        NameSourceIsAttribute = nameReadResult.IsAttribute;
    }

    public string DeviceName { get; }

    public DanteChannelKind Kind { get; }

    public int Index { get; }

    public string Name { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Index.ToString() : Name;

    internal string? NameSource { get; }

    internal bool NameSourceIsAttribute { get; }

    internal XElement Element { get; }

    private static ChannelNameReadResult ReadChannelName(DanteChannelKind kind, int index, XElement element)
    {
        string[] preferredNames = kind == DanteChannelKind.Tx
            ? ["label", "name", "channel_name", "id"]
            : ["name", "label", "channel_name", "id"];

        foreach (string name in preferredNames)
        {
            string? value = element.Element(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return new ChannelNameReadResult(value.Trim(), name, false);
            }

            value = element.Attribute(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return new ChannelNameReadResult(value.Trim(), name, true);
            }
        }

        return new ChannelNameReadResult(index.ToString(), null, false);
    }

    private sealed record ChannelNameReadResult(string Value, string? SourceName, bool IsAttribute);
}
