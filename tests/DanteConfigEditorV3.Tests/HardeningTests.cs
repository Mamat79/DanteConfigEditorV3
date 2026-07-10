using System.Reflection;
using System.Xml.Linq;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;
using DanteConfigEditorV3.TestSupport;

namespace DanteConfigEditorV3.Tests;

public sealed class HardeningTests
{
    public static TheoryData<string> SubscriptionDeviceAliases => new()
    {
        "subscribed_device",
        "subscription_device",
        "tx_device",
        "source_device"
    };

    public static TheoryData<string> SubscriptionChannelAliases => new()
    {
        "subscribed_channel",
        "subscribed_channel_name",
        "subscribed_channel_label",
        "subscribed_tx_channel",
        "subscribed_tx_channel_name",
        "subscribed_label",
        "source_channel",
        "source_channel_name"
    };

    [Fact]
    public void RenameThenInstanceIdChangeBlocksSave()
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        string destination = workspace.PathFor("blocked-instance-id.xml");
        DanteProject project = DanteProject.Load(source);

        project.RenameDevice("DEVICE-A", "DEVICE-A-RENAMED");
        XElement renamedDevice = FindDeviceElement(project.Document, "DEVICE-A-RENAMED");
        FindChild(FindChild(renamedDevice, "instance_id"), "device_id").Value = "001DC1FFFE00FFFF";

        Assert.True(project.ValidateXmlChangeGuard().HasErrors);
        Assert.Throws<InvalidOperationException>(() => project.SaveAs(destination));
        Assert.False(File.Exists(destination));
    }

    [Theory]
    [InlineData("danteId", "999")]
    [InlineData("mediaType", "video")]
    public void RenameThenChannelTechnicalAttributeChangeBlocksSave(string attributeName, string value)
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        string destination = workspace.PathFor($"blocked-{attributeName}.xml");
        DanteProject project = DanteProject.Load(source);

        project.RenameDevice("DEVICE-A", "DEVICE-A-RENAMED");
        XElement renamedDevice = FindDeviceElement(project.Document, "DEVICE-A-RENAMED");
        FindChildren(renamedDevice, "txchannel").First().SetAttributeValue(attributeName, value);

        Assert.True(project.ValidateXmlChangeGuard().HasErrors);
        Assert.Throws<InvalidOperationException>(() => project.SaveAs(destination));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void TechnicalElementReorderingDoesNotTriggerGuard()
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        XDocument original = XDocument.Load(source, LoadOptions.PreserveWhitespace);
        XDocument reordered = new(original);
        XElement device = FindDeviceElement(reordered, "DEVICE-A");
        XElement rtp = FindChild(device, "rtp");
        XElement clock = FindChild(device, "clock");

        rtp.Remove();
        clock.AddAfterSelf(rtp);

        DanteValidationResult result = DanteXmlChangeGuardService.ValidateChanges(original, reordered);
        Assert.False(result.HasErrors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void UnknownTechnicalElementBlocksSaveByDefault()
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        string destination = workspace.PathFor("blocked-unknown.xml");
        DanteProject project = DanteProject.Load(source);
        XElement device = FindDeviceElement(project.Document, "DEVICE-A");
        device.Add(new XElement(device.Name.Namespace + "future_transport_setting", "unsafe-default"));

        DanteValidationResult guard = project.ValidateXmlChangeGuard();
        Assert.True(guard.HasErrors);
        Assert.Throws<InvalidOperationException>(() => project.SaveAs(destination));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void FailureAfterTemporaryFileCreationKeepsExistingDestinationIntact()
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        string destination = workspace.PathFor("existing-destination.xml");
        const string originalDestinationContent = "<existing>must survive</existing>";
        File.WriteAllText(destination, originalDestinationContent);
        DanteProject project = DanteProject.Load(source);
        project.SetLatency("DEVICE-A", "2000");

        MethodInfo? testableSave = typeof(DanteProject).GetMethod(
            "SaveAs",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(string), typeof(Action<string>)],
            modifiers: null);
        Assert.NotNull(testableSave);

        Action<string> failBeforeCommit = stage =>
        {
            if (string.Equals(stage, "BeforeDestinationCommit", StringComparison.Ordinal))
            {
                throw new IOException("Injected failure after temporary file creation.");
            }
        };

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => testableSave.Invoke(project, [destination, failBeforeCommit]));
        Assert.IsType<IOException>(exception.InnerException);
        Assert.Equal(originalDestinationContent, File.ReadAllText(destination));
    }

    [Fact]
    public void SaveAsBecomesRecoveryReferenceForLaterChanges()
    {
        using TestWorkspace workspace = new();
        string source = workspace.CopyFixture("representative-preset.xml");
        string savedPath = workspace.PathFor("renamed-session.xml");
        string recoveryDirectory = workspace.PathFor("recovery");
        DanteProject project = DanteProject.Load(source);
        project.SetLatency("DEVICE-C", "2000");

        project.SaveAs(savedPath);
        project.SetEncoding("DEVICE-C", "32");
        SessionRecoveryService.Save(project, recoveryDirectory);

        Assert.Equal(Path.GetFullPath(savedPath), Path.GetFullPath(project.OriginalFilePath));
        RecoveryCandidate candidate = Assert.IsType<RecoveryCandidate>(SessionRecoveryService.Find(savedPath, recoveryDirectory));
        Assert.True(candidate.SourceMatches);
        DanteProject recovered = DanteProject.LoadRecovered(savedPath, candidate.RecoveryXmlPath);
        Assert.Equal("32", recovered.FindDevice("DEVICE-C")?.Encoding);
        Assert.True(recovered.IsModified);
    }

    [Fact]
    public void StaticIpChangePreservesSecondaryInterfaceAndDns()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("two-interfaces-static.xml");
        CreateTwoInterfacePreset(source);
        DanteProject project = DanteProject.Load(source);
        XElement device = FindDeviceElement(project.Document, "DEVICE-IP");
        XElement secondaryBefore = new(FindInterface(device, "1"));

        project.SetIpAddressStatic("DEVICE-IP", "192.168.10.99", "255.255.255.0", "192.168.10.1");

        XElement currentDevice = FindDeviceElement(project.Document, "DEVICE-IP");
        XElement primary = FindInterface(currentDevice, "0");
        Assert.Equal("192.168.10.99", FindChild(FindChild(primary, "ipv4_address"), "address").Value);
        Assert.Equal("9.9.9.9", FindChild(FindChild(primary, "ipv4_address"), "dnsserver").Value);
        Assert.True(XNode.DeepEquals(secondaryBefore, FindInterface(currentDevice, "1")));
    }

    [Fact]
    public void DynamicIpChangePreservesSecondaryInterfaceGatewayAndDns()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("two-interfaces-dynamic.xml");
        CreateTwoInterfacePreset(source);
        DanteProject project = DanteProject.Load(source);
        XElement device = FindDeviceElement(project.Document, "DEVICE-IP");
        XElement secondaryBefore = new(FindInterface(device, "1"));

        project.SetIpAddressDynamic("DEVICE-IP");

        XElement currentDevice = FindDeviceElement(project.Document, "DEVICE-IP");
        XElement primaryAddress = FindChild(FindInterface(currentDevice, "0"), "ipv4_address");
        Assert.Equal("dynamic", primaryAddress.Attribute("mode")?.Value);
        Assert.Equal("192.168.10.1", FindChild(primaryAddress, "gateway").Value);
        Assert.Equal("9.9.9.9", FindChild(primaryAddress, "dnsserver").Value);
        Assert.True(XNode.DeepEquals(secondaryBefore, FindInterface(currentDevice, "1")));
    }

    [Theory]
    [MemberData(nameof(SubscriptionDeviceAliases))]
    public void EverySubscriptionDeviceAliasIsReadAndUpdated(string alias)
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor($"device-alias-{alias}.xml");
        CreateAliasPreset(source, alias, "subscribed_channel");
        DanteProject project = DanteProject.Load(source);
        Assert.Equal("TX-DEVICE", Assert.Single(project.PatchMatrix.Subscriptions).RawTxDeviceName);

        project.RenameDevice("TX-DEVICE", "TX-RENAMED");

        DanteSubscription subscription = Assert.Single(project.PatchMatrix.Subscriptions);
        Assert.Equal("TX-RENAMED", subscription.RawTxDeviceName);
        Assert.Equal("TX-RENAMED", FindChild(subscription.RxElementForTests(), alias).Value);
    }

    [Theory]
    [MemberData(nameof(SubscriptionChannelAliases))]
    public void EverySubscriptionChannelAliasIsReadAndUpdated(string alias)
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor($"channel-alias-{alias}.xml");
        CreateAliasPreset(source, "subscribed_device", alias);
        DanteProject project = DanteProject.Load(source);
        Assert.Equal("SOURCE", Assert.Single(project.PatchMatrix.Subscriptions).TxChannelName);

        project.RenameChannel("TX-DEVICE", DanteChannelKind.Tx, 1, "SOURCE-RENAMED");

        DanteSubscription subscription = Assert.Single(project.PatchMatrix.Subscriptions);
        Assert.Equal("SOURCE-RENAMED", subscription.TxChannelName);
        Assert.Equal("SOURCE-RENAMED", FindChild(subscription.RxElementForTests(), alias).Value);
    }

    [Fact]
    public void DefaultNamespacePresetCanBeEditedSavedAndReloaded()
    {
        using TestWorkspace workspace = new();
        string source = workspace.PathFor("default-namespace.xml");
        string destination = workspace.PathFor("default-namespace-saved.xml");
        XNamespace ns = "urn:audinate:dante:preset";
        SyntheticPresetFactory.Create(source, 2, txPerDevice: 2, rxPerDevice: 2, xmlNamespace: ns);

        DanteProject project = DanteProject.Load(source);
        Assert.Equal(2, project.Devices.Count);
        project.RenameDevice("DEVICE-001", "DEVICE-001-RENAMED");
        project.RenameChannel("DEVICE-001-RENAMED", DanteChannelKind.Tx, 1, "PROGRAM");
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);

        project.SaveAs(destination);
        DanteProject reloaded = DanteProject.Load(destination);
        Assert.NotNull(reloaded.FindDevice("DEVICE-001-RENAMED"));
        Assert.Equal(ns, reloaded.Document.Root?.Name.Namespace);
        Assert.All(reloaded.Document.Root!.Descendants(), element => Assert.Equal(ns, element.Name.Namespace));
    }

    private static XElement FindDeviceElement(XDocument document, string name)
    {
        return document.Root!.Elements()
            .Where(element => element.Name.LocalName == "device")
            .Single(element => string.Equals(FindChild(element, "name").Value, name, StringComparison.Ordinal));
    }

    private static XElement FindInterface(XElement device, string network)
    {
        return FindChildren(device, "interface").Single(element => element.Attribute("network")?.Value == network);
    }

    private static XElement FindChild(XElement parent, string localName)
    {
        return parent.Elements().Single(element => element.Name.LocalName == localName);
    }

    private static IEnumerable<XElement> FindChildren(XElement parent, string localName)
    {
        return parent.Elements().Where(element => element.Name.LocalName == localName);
    }

    private static void CreateAliasPreset(string path, string deviceAlias, string channelAlias)
    {
        XDocument document = BuildMinimalPreset(
            new XElement("device",
                new XElement("name", "TX-DEVICE"),
                new XElement("friendly_name", "TX-DEVICE"),
                new XElement("instance_id", new XElement("device_id", "001DC1FFFE000101"), new XElement("process_id", "0")),
                new XElement("samplerate", "48000"),
                new XElement("encoding", "24"),
                new XElement("unicast_latency", "1000"),
                new XElement("txchannel", new XAttribute("danteId", "1"), new XAttribute("mediaType", "audio"), new XElement("label", "SOURCE"))),
            new XElement("device",
                new XElement("name", "RX-DEVICE"),
                new XElement("friendly_name", "RX-DEVICE"),
                new XElement("instance_id", new XElement("device_id", "001DC1FFFE000102"), new XElement("process_id", "0")),
                new XElement("samplerate", "48000"),
                new XElement("encoding", "24"),
                new XElement("unicast_latency", "1000"),
                new XElement("rxchannel",
                    new XAttribute("danteId", "1"),
                    new XAttribute("mediaType", "audio"),
                    new XElement("name", "INPUT"),
                    new XElement(channelAlias, "SOURCE"),
                    new XElement(deviceAlias, "TX-DEVICE"))));
        document.Save(path, SaveOptions.DisableFormatting);
    }

    private static void CreateTwoInterfacePreset(string path)
    {
        XElement device = new(
            "device",
            new XElement("name", "DEVICE-IP"),
            new XElement("friendly_name", "DEVICE-IP"),
            new XElement("instance_id", new XElement("device_id", "001DC1FFFE000201"), new XElement("process_id", "0")),
            new XElement("samplerate", "48000"),
            new XElement("encoding", "24"),
            new XElement("unicast_latency", "1000"),
            BuildStaticInterface("0", "192.168.10.10", "192.168.10.1", "9.9.9.9"),
            BuildStaticInterface("1", "10.50.0.10", "10.50.0.1", "1.1.1.1"),
            new XElement("txchannel", new XAttribute("danteId", "1"), new XAttribute("mediaType", "audio"), new XElement("label", "TX")),
            new XElement("rxchannel", new XAttribute("danteId", "1"), new XAttribute("mediaType", "audio"), new XElement("name", "RX")));
        BuildMinimalPreset(device).Save(path, SaveOptions.DisableFormatting);
    }

    private static XElement BuildStaticInterface(string network, string address, string gateway, string dns)
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

    private static XDocument BuildMinimalPreset(params XElement[] devices)
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("preset", new XAttribute("version", "3.0.0"), new XElement("name", "Hardening test"), devices));
    }

    private sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.HardeningTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public string PathFor(string fileName) => Path.Combine(DirectoryPath, fileName);

        public string CopyFixture(string fixtureName)
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
            string destination = PathFor(fixtureName);
            File.Copy(source, destination, true);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
        }
    }
}

internal static class SubscriptionTestExtensions
{
    public static XElement RxElementForTests(this DanteSubscription subscription)
    {
        PropertyInfo property = typeof(DanteSubscription).GetProperty("RxElement", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RxElement is unavailable.");
        return (XElement)(property.GetValue(subscription) ?? throw new InvalidOperationException("RxElement is null."));
    }
}
