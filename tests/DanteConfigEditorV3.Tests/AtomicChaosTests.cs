using System.Xml.Linq;
using DanteConfigEditor.Models;
using DanteConfigEditorV3.TestSupport;

namespace DanteConfigEditorV3.Tests;

public sealed class AtomicChaosTests
{
    [Fact]
    public void AtomicChaosRandomizesEditableValuesWithoutChangingTechnicalIdentity()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("atomic-source.xml");
        string destination = workspace.PathFor("atomic-result.xml");
        XNamespace ns = "urn:audinate:dante:preset";
        SyntheticPresetFactory.Create(source, deviceCount: 4, txPerDevice: 4, rxPerDevice: 4, xmlNamespace: ns);
        DanteProject project = DanteProject.Load(source);
        string[] technicalIdentityBefore = TechnicalIdentity(project.Document);

        AtomicChaosResult result = project.ApplyAtomicChaos(seed: 24680);

        Assert.Equal(24680, result.Seed);
        Assert.Equal(4, result.DeviceCount);
        Assert.Equal(16, result.TxChannelCount);
        Assert.Equal(16, result.RxChannelCount);
        Assert.Equal(16, result.PatchedRxCount + result.DisconnectedRxCount);
        Assert.Equal(4, result.StaticIpCount + result.DynamicIpCount);
        Assert.True(result.StaticIpCount > 0);
        Assert.True(result.DynamicIpCount > 0);
        Assert.InRange(result.RedundantDeviceCount, 1, 3);
        Assert.InRange(result.PreferredMasterCount, 1, 3);
        Assert.True(result.SampleRateValueCount > 1);
        Assert.True(result.EncodingValueCount > 1);
        Assert.True(result.LatencyValueCount > 1);
        Assert.Equal(technicalIdentityBefore, TechnicalIdentity(project.Document));
        Assert.Equal(ns, project.Document.Root?.Name.Namespace);
        Assert.All(project.Document.Root!.Descendants(), element => Assert.Equal(ns, element.Name.Namespace));

        Assert.Equal(project.Devices.Count, project.Devices.Select(device => device.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(project.Devices, device =>
        {
            Assert.False(device.Name.StartsWith("ATOM-", StringComparison.Ordinal));
            Assert.Matches("^[A-Z0-9-]+$", device.Name);
            Assert.Contains(device.Samplerate, new[] { "44100", "48000", "88200", "96000", "176400", "192000" });
            Assert.Contains(device.Encoding, new[] { "16", "24", "32" });
            Assert.Contains(device.Latency, new[] { "250", "1000", "2000", "5000" });
            Assert.All(device.TxChannels, channel => Assert.StartsWith("CHAOS-TX-", channel.DisplayName, StringComparison.Ordinal));
            Assert.All(device.RxChannels, channel => Assert.StartsWith("CHAOS-RX-", channel.DisplayName, StringComparison.Ordinal));
        });

        Assert.DoesNotContain(project.PatchMatrix.Subscriptions, subscription => subscription.IsWarning || subscription.IsConflict);
        Assert.Equal(2, project.Devices.Select(device => device.IsRedundant).Distinct().Count());
        Assert.Equal(2, project.Devices.Select(device => device.PreferredMaster).Distinct().Count());
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);

        project.SaveAs(destination);
        DanteProject reloaded = DanteProject.Load(destination);
        Assert.Equal(4, reloaded.Devices.Count);
        Assert.False(reloaded.Validate().HasErrors);
    }

    [Fact]
    public void AtomicChaosKeepsFunnyDeviceNamesUniqueBeyondTheNamePool()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("atomic-many-devices.xml");
        SyntheticPresetFactory.Create(source, deviceCount: 80, txPerDevice: 1, rxPerDevice: 1);
        DanteProject project = DanteProject.Load(source);

        project.ApplyAtomicChaos(seed: 314159);

        Assert.Equal(80, project.Devices.Select(device => device.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(project.Devices, device => device.Name.StartsWith("ATOM-", StringComparison.Ordinal));
        Assert.Contains(project.Devices, device => device.Name.StartsWith("ATHENA", StringComparison.Ordinal)
            || device.Name.StartsWith("RAVENNA", StringComparison.Ordinal)
            || device.Name.StartsWith("PYRAMIX", StringComparison.Ordinal));
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
    }

    [Fact]
    public void AtomicChaosIsOneUndoableOperation()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("atomic-undo.xml");
        SyntheticPresetFactory.Create(source, deviceCount: 3, txPerDevice: 3, rxPerDevice: 3);
        DanteProject project = DanteProject.Load(source);
        string originalXml = project.Document.ToString(SaveOptions.DisableFormatting);

        project.PushUndoSnapshot("Atomic Bomb");
        project.ApplyAtomicChaos(seed: 12345);
        Assert.NotEqual(originalXml, project.Document.ToString(SaveOptions.DisableFormatting));

        Assert.Equal("Atomic Bomb", project.UndoLastChange());
        Assert.Equal(originalXml, project.Document.ToString(SaveOptions.DisableFormatting));
        Assert.False(project.IsModified);
    }

    [Fact]
    public void AtomicChaosPreservesGatewayDnsAndSecondaryInterface()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("atomic-interfaces.xml");
        CreateTwoInterfacePreset(source);
        DanteProject project = DanteProject.Load(source);
        XElement deviceBefore = Device(project.Document);
        XElement secondaryBefore = new(Interface(deviceBefore, "1"));

        AtomicChaosResult result = project.ApplyAtomicChaos(seed: 99);

        XElement deviceAfter = Device(project.Document);
        XElement primaryAddress = Child(Interface(deviceAfter, "0"), "ipv4_address");
        Assert.Equal("192.168.10.1", Child(primaryAddress, "gateway").Value);
        Assert.Equal("9.9.9.9", Child(primaryAddress, "dnsserver").Value);
        Assert.True(XNode.DeepEquals(secondaryBefore, Interface(deviceAfter, "1")));
        Assert.Equal(1, result.StaticIpCount + result.DynamicIpCount);
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
    }

    private static string[] TechnicalIdentity(XDocument document)
    {
        return document.Root!.Elements()
            .Where(element => element.Name.LocalName == "device")
            .SelectMany(device =>
            {
                string deviceId = Child(Child(device, "instance_id"), "device_id").Value;
                IEnumerable<string> channels = device.Elements()
                    .Where(channel => channel.Name.LocalName is "txchannel" or "rxchannel")
                    .Select(channel => $"{channel.Name.LocalName}:{channel.Attribute("danteId")?.Value}:{channel.Attribute("mediaType")?.Value}");
                return new[] { $"device_id:{deviceId}" }.Concat(channels);
            })
            .ToArray();
    }

    private static void CreateTwoInterfacePreset(string path)
    {
        XElement device = new(
            "device",
            new XElement("name", "DEVICE-IP"),
            new XElement("friendly_name", "DEVICE-IP"),
            new XElement("instance_id", new XElement("device_id", "001DC1FFFE00A701"), new XElement("process_id", "0")),
            new XElement("preferred_master", new XAttribute("value", "true")),
            new XElement("redundancy", new XAttribute("value", "false")),
            new XElement("samplerate", "48000"),
            new XElement("encoding", "24"),
            new XElement("unicast_latency", "1000"),
            BuildInterface("0", "192.168.10.10", "192.168.10.1", "9.9.9.9"),
            BuildInterface("1", "10.50.0.10", "10.50.0.1", "1.1.1.1"),
            new XElement("txchannel", new XAttribute("danteId", "1"), new XAttribute("mediaType", "audio"), new XElement("label", "TX")),
            new XElement("rxchannel", new XAttribute("danteId", "1"), new XAttribute("mediaType", "audio"), new XElement("name", "RX")));
        XDocument document = new(new XElement("preset", new XAttribute("version", "3.0.0"), new XElement("name", "Atomic interfaces"), device));
        document.Save(path, SaveOptions.DisableFormatting);
    }

    private static XElement BuildInterface(string network, string address, string gateway, string dns)
    {
        return new XElement(
            "interface",
            new XAttribute("network", network),
            new XElement(
                "ipv4_address",
                new XAttribute("mode", "static"),
                new XElement("address", address),
                new XElement("netmask", "255.255.255.0"),
                new XElement("gateway", gateway),
                new XElement("dnsserver", dns)));
    }

    private static XElement Device(XDocument document) => document.Root!.Elements().Single(element => element.Name.LocalName == "device");

    private static XElement Interface(XElement device, string network) =>
        device.Elements().Single(element => element.Name.LocalName == "interface" && element.Attribute("network")?.Value == network);

    private static XElement Child(XElement parent, string localName) =>
        parent.Elements().Single(element => element.Name.LocalName == localName);

    private sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.AtomicChaosTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        private string Root { get; }

        public string PathFor(string name) => Path.Combine(Root, name);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
