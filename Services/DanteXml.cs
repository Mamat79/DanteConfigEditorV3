using System.Xml.Linq;

namespace DanteConfigEditor.Services;

internal static class DanteXml
{
    public static XElement? Child(this XElement? parent, string localName)
    {
        return parent?.Elements().FirstOrDefault(element =>
            string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal));
    }

    public static IEnumerable<XElement> Children(this XElement? parent, string localName)
    {
        return parent?.Elements().Where(element =>
            string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal)) ?? [];
    }

    public static IEnumerable<XElement> DescendantsNamed(this XElement? parent, string localName)
    {
        return parent?.Descendants().Where(element =>
            string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal)) ?? [];
    }

    public static XName ChildName(this XElement parent, string localName)
    {
        return parent.Name.Namespace + localName;
    }

    public static string ChildValue(this XElement? parent, string localName)
    {
        return parent.Child(localName)?.Value.Trim() ?? string.Empty;
    }
}
