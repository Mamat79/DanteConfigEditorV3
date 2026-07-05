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

    // Nom affiché dans l'interface. Si le XML ne donne pas de nom lisible,
    // on retombe sur le numéro du canal pour garder une valeur exploitable.
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Index.ToString() : Name;

    // Mémorise où le nom a été lu dans le XML. Au renommage, on réécrit au
    // même endroit pour limiter les risques d'incompatibilité avec Dante.
    internal string? NameSource { get; }

    internal bool NameSourceIsAttribute { get; }

    internal XElement Element { get; }

    private static ChannelNameReadResult ReadChannelName(DanteChannelKind kind, int index, XElement element)
    {
        // Les TX utilisent souvent "label", les RX plutôt "name".
        // Les autres noms sont des variantes vues dans certains exports.
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
