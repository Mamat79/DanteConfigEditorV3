using System.Xml.Linq;

namespace DanteConfigEditor.Services;

internal static class DanteIpConfiguration
{
    public static XElement? FindPrimaryInterface(XElement deviceElement)
    {
        XElement[] interfaces = deviceElement.Children("interface").ToArray();
        if (interfaces.Length == 0)
        {
            interfaces = deviceElement.DescendantsNamed("interface").ToArray();
        }

        return interfaces.FirstOrDefault(element =>
                string.Equals(element.Attribute("network")?.Value, "0", StringComparison.OrdinalIgnoreCase))
            ?? interfaces.FirstOrDefault();
    }

    public static XElement? FindPrimaryIpv4Address(XElement deviceElement)
    {
        XElement? primaryInterface = FindPrimaryInterface(deviceElement);
        return primaryInterface?.Child("ipv4_address")
            ?? primaryInterface?.DescendantsNamed("ipv4_address").FirstOrDefault();
    }

    public static XElement FindOrCreatePrimaryIpv4Address(XElement deviceElement)
    {
        XElement? existing = FindPrimaryIpv4Address(deviceElement);
        if (existing is not null)
        {
            return existing;
        }

        XElement primaryInterface = FindPrimaryInterface(deviceElement)
            ?? throw new InvalidOperationException($"La machine {deviceElement.ChildValue("name")} ne contient pas de balise <interface> IPv4 modifiable.");
        XElement ipv4Address = new(primaryInterface.ChildName("ipv4_address"));
        primaryInterface.Add(ipv4Address);
        return ipv4Address;
    }
}
