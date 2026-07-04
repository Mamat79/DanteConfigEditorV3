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
        Name = ReadChannelName(kind, index, element);
    }

    public string DeviceName { get; }

    public DanteChannelKind Kind { get; }

    public int Index { get; }

    public string Name { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Index.ToString() : Name;

    internal XElement Element { get; }

    private static string ReadChannelName(DanteChannelKind kind, int index, XElement element)
    {
        string[] preferredNames = kind == DanteChannelKind.Tx
            ? ["label", "name", "channel_name", "id"]
            : ["name", "label", "channel_name", "id"];

        foreach (string name in preferredNames)
        {
            string? value = element.Element(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            value = element.Attribute(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return index.ToString();
    }
}
