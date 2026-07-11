using System.Globalization;
using System.Xml.Linq;

namespace DanteConfigEditorV3.TestSupport;

public static class SyntheticPresetFactory
{
    public static void Create(string path, int deviceCount, int txPerDevice = 64, int rxPerDevice = 64, XNamespace? xmlNamespace = null)
    {
        if (deviceCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deviceCount));
        }

        XNamespace ns = xmlNamespace ?? XNamespace.None;
        XElement preset = new(
            ns + "preset",
            new XAttribute("version", "3.0.0"),
            new XElement(ns + "name", $"Synthetic-{deviceCount}"),
            new XElement(ns + "description", "V3.06 synthetic regression preset"));

        for (int deviceIndex = 1; deviceIndex <= deviceCount; deviceIndex++)
        {
            string deviceName = $"DEVICE-{deviceIndex:D3}";
            XElement device = BuildDevice(ns, deviceIndex, deviceName, txPerDevice, rxPerDevice);
            preset.Add(device);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Environment.CurrentDirectory);
        new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), preset).Save(path, SaveOptions.DisableFormatting);
    }

    private static XElement BuildDevice(XNamespace ns, int deviceIndex, string deviceName, int txCount, int rxCount)
    {
        XElement device = new(
            ns + "device",
            new XElement(ns + "captureInfo"),
            new XElement(ns + "name", deviceName),
            new XElement(ns + "default_name", $"DEFAULT-{deviceIndex:D3}"),
            new XElement(
                ns + "instance_id",
                new XElement(ns + "device_id", $"001DC1{deviceIndex:X10}"),
                new XElement(ns + "process_id", "0")),
            new XElement(ns + "manufacturer_id", "0000000000000001"),
            new XElement(ns + "manufacturer_name", "Synthetic Manufacturer"),
            new XElement(ns + "model_id", "0000000000000001"),
            new XElement(ns + "model_name", "Synthetic 64x64"),
            new XElement(ns + "model_version", "1.0"),
            new XElement(ns + "device_type", "0000000000000001"),
            new XElement(ns + "device_type_string", "SyntheticDevice"),
            new XElement(ns + "friendly_name", deviceName),
            new XElement(ns + "preferred_master", new XAttribute("value", deviceIndex == 1 ? "true" : "false")),
            new XElement(ns + "redundancy", new XAttribute("value", "false")),
            new XElement(ns + "samplerate", "48000"),
            new XElement(ns + "encoding", "24"),
            new XElement(ns + "unicast_latency", "1000"),
            new XElement(
                ns + "interface",
                new XAttribute("network", "0"),
                new XElement(ns + "ipv4_address", new XAttribute("mode", "dynamic"))));

        for (int channelIndex = 1; channelIndex <= txCount; channelIndex++)
        {
            device.Add(new XElement(
                ns + "txchannel",
                new XAttribute("danteId", channelIndex.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("mediaType", "audio"),
                new XElement(ns + "label", $"TX-{channelIndex:D2}")));
        }

        for (int channelIndex = 1; channelIndex <= rxCount; channelIndex++)
        {
            string sourceDevice = deviceIndex == 1 ? "." : $"DEVICE-{deviceIndex - 1:D3}";
            device.Add(new XElement(
                ns + "rxchannel",
                new XAttribute("danteId", channelIndex.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("mediaType", "audio"),
                new XElement(ns + "name", $"RX-{channelIndex:D2}"),
                new XElement(ns + "subscribed_channel", $"TX-{channelIndex:D2}"),
                new XElement(ns + "subscribed_device", sourceDevice)));
        }

        device.Add(
            new XElement(ns + "rtp", new XElement(ns + "interop_mode", "none")),
            new XElement(ns + "clock", new XElement(ns + "subdomain_name", "_DFLT")),
            new XElement(
                ns + "clock_priority",
                new XElement(ns + "preferred", deviceIndex == 1 ? "true" : "false"),
                new XElement(ns + "follower_only", "false")));
        return device;
    }
}
