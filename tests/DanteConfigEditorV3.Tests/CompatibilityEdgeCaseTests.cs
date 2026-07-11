using System.Xml.Linq;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class CompatibilityEdgeCaseTests
{
    [Fact]
    public void LocalPatchMarkerRoundTripsAndFollowsDeviceAndTxRenames()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string destination = directory.PathFor("local-patch-renamed.xml");
        DanteProject project = DanteProject.Load(source);
        DanteSubscription localBefore = Assert.Single(project.PatchMatrix.Subscriptions, subscription => subscription.IsLocalSubscription);
        Assert.Equal(".", localBefore.RawTxDeviceName);
        Assert.Equal("DEVICE-A", localBefore.ResolvedTxDeviceName);

        project.RenameDevice("DEVICE-A", "DEVICE-A-LOCAL");
        project.RenameChannel("DEVICE-A-LOCAL", DanteChannelKind.Tx, 1, "LOCAL-PROGRAM");
        project.SaveAs(destination);

        DanteProject reloaded = DanteProject.Load(destination);
        DanteSubscription localAfter = Assert.Single(reloaded.PatchMatrix.Subscriptions, subscription => subscription.IsLocalSubscription);
        Assert.Equal(".", localAfter.RawTxDeviceName);
        Assert.Equal("DEVICE-A-LOCAL", localAfter.ResolvedTxDeviceName);
        Assert.Equal("LOCAL-PROGRAM", localAfter.TxChannelName);
    }

    [Fact]
    public void MissingTxDeviceIsAWarningAndDoesNotBlockUnchangedSave()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("external-tx.xml");
        string destination = directory.PathFor("external-tx-saved.xml");
        BuildPreset(
            Device("RX-DEVICE", "0001",
                Rx(1, "INPUT", "EXTERNAL-TX", "PROGRAM")))
            .Save(source, SaveOptions.DisableFormatting);

        DanteProject project = DanteProject.Load(source);
        DanteSubscription subscription = Assert.Single(project.PatchMatrix.Subscriptions);
        Assert.Equal(DanteSubscriptionKind.ExternalMissingDevice, subscription.Kind);
        Assert.Contains(project.Validate().Warnings, warning => warning.Contains("device TX absent", StringComparison.OrdinalIgnoreCase));

        project.SaveAs(destination);
        Assert.Equal(DanteSubscriptionKind.ExternalMissingDevice, Assert.Single(DanteProject.Load(destination).PatchMatrix.Subscriptions).Kind);
    }

    [Fact]
    public void MissingTxChannelIsAWarningAndDoesNotBlockUnchangedSave()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("missing-channel.xml");
        string destination = directory.PathFor("missing-channel-saved.xml");
        BuildPreset(
            Device("TX-DEVICE", "0001", Tx(1, "PROGRAM")),
            Device("RX-DEVICE", "0002", Rx(1, "INPUT", "TX-DEVICE", "MISSING")))
            .Save(source, SaveOptions.DisableFormatting);

        DanteProject project = DanteProject.Load(source);
        DanteSubscription subscription = Assert.Single(project.PatchMatrix.Subscriptions);
        Assert.Equal(DanteSubscriptionKind.MissingChannel, subscription.Kind);
        Assert.Contains(project.Validate().Warnings, warning => warning.Contains("canal TX non retrouvé", StringComparison.OrdinalIgnoreCase));

        project.SaveAs(destination);
        Assert.Equal(DanteSubscriptionKind.MissingChannel, Assert.Single(DanteProject.Load(destination).PatchMatrix.Subscriptions).Kind);
    }

    [Fact]
    public void PartialPresetWithoutOptionalFieldsCanRoundTripUnchanged()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("partial.xml");
        string destination = directory.PathFor("partial-saved.xml");
        XDocument document = BuildPreset(new XElement(
            "device",
            new XElement("name", "PARTIAL-DEVICE"),
            new XElement("instance_id", new XElement("device_id", "PARTIAL-0001"))));
        document.Save(source, SaveOptions.DisableFormatting);

        DanteProject project = DanteProject.Load(source);
        Assert.Single(project.Devices);
        Assert.False(project.Validate().HasErrors);
        project.SaveAs(destination);

        DanteProject reloaded = DanteProject.Load(destination);
        Assert.Equal("PARTIAL-DEVICE", Assert.Single(reloaded.Devices).Name);
        Assert.Empty(reloaded.Devices[0].TxChannels);
        Assert.Empty(reloaded.Devices[0].RxChannels);
    }

    [Fact]
    public void DevicesWithoutTxOrRxAreReportedWithoutBlockingSave()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("one-way-devices.xml");
        BuildPreset(
            Device("TX-ONLY", "0001", Tx(1, "PROGRAM")),
            Device("RX-ONLY", "0002", Rx(1, "INPUT")))
            .Save(source, SaveOptions.DisableFormatting);

        DanteProject project = DanteProject.Load(source);
        DanteValidationResult validation = project.Validate();

        Assert.False(validation.HasErrors);
        Assert.Contains(validation.Warnings, warning => warning.Contains("TX-ONLY ne contient aucun canal RX", StringComparison.Ordinal));
        Assert.Contains(validation.Warnings, warning => warning.Contains("RX-ONLY ne contient aucun canal TX", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidationReportsNoPreferredMasterAndSeveralPreferredMasters()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("preferred-masters.xml");
        BuildPreset(Device("DEVICE-1", "0001"), Device("DEVICE-2", "0002"))
            .Save(source, SaveOptions.DisableFormatting);
        DanteProject project = DanteProject.Load(source);

        Assert.Contains(project.Validate().Warnings, warning => warning.Contains("Aucune machine preferred master", StringComparison.Ordinal));

        project.ApplyBatch(batch =>
        {
            batch.SetPreferredMaster("DEVICE-1", true);
            batch.SetPreferredMaster("DEVICE-2", true);
        });

        Assert.Contains(project.Validate().Warnings, warning => warning.Contains("2 machines sont déclarées preferred master", StringComparison.Ordinal));
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
    }

    [Fact]
    public void UnknownExistingValuesAndVendorExtensionArePreserved()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("unknown-values.xml");
        string destination = directory.PathFor("unknown-values-saved.xml");
        XElement vendorExtension = new(
            "vendor_extension",
            new XAttribute("mode", "future"),
            new XElement("opaque", "valeur-inconnue"));
        XElement device = Device("FUTURE-DEVICE", "0001", Tx(1, "PROGRAM"), vendorExtension);
        device.Element("samplerate")!.Value = "12345";
        device.Element("encoding")!.Value = "20";
        BuildPreset(device).Save(source, SaveOptions.DisableFormatting);
        XElement originalExtension = new(vendorExtension);

        DanteProject project = DanteProject.Load(source);
        project.SetLatency("FUTURE-DEVICE", "2000");
        project.SaveAs(destination);

        DanteProject reloaded = DanteProject.Load(destination);
        DanteDevice reloadedDevice = Assert.Single(reloaded.Devices);
        Assert.Equal("12345", reloadedDevice.Samplerate);
        Assert.Equal("20", reloadedDevice.Encoding);
        XElement currentExtension = reloaded.Document.Descendants().Single(element => element.Name.LocalName == "vendor_extension");
        Assert.True(XNode.DeepEquals(originalExtension, currentExtension));
    }

    [Fact]
    public void ExistingUnicodeAndLongNamesSurviveAnUnrelatedSave()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("unicode-long.xml");
        string destination = directory.PathFor("unicode-long-saved.xml");
        string longDeviceName = "Régie-Été-" + new string('X', 160);
        const string unicodeChannelName = "Entrée scène 日本語";
        BuildPreset(Device(longDeviceName, "0001", Tx(1, unicodeChannelName)))
            .Save(source, SaveOptions.DisableFormatting);

        DanteProject project = DanteProject.Load(source);
        project.SetLatency(longDeviceName, "2000");
        project.SaveAs(destination);

        DanteDevice reloaded = Assert.Single(DanteProject.Load(destination).Devices);
        Assert.Equal(longDeviceName, reloaded.Name);
        Assert.Equal(unicodeChannelName, Assert.Single(reloaded.TxChannels).DisplayName);
    }

    private static XDocument BuildPreset(params XElement[] devices)
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("preset", new XAttribute("version", "3.0.0"), new XElement("name", "Compatibility edge cases"), devices));
    }

    private static XElement Device(string name, string identifier, params XElement[] children)
    {
        return new XElement(
            "device",
            new XElement("name", name),
            new XElement("friendly_name", name),
            new XElement("instance_id", new XElement("device_id", "TEST-" + identifier), new XElement("process_id", "0")),
            new XElement("preferred_master", new XAttribute("value", "false")),
            new XElement("redundancy", new XAttribute("value", "false")),
            new XElement("samplerate", "48000"),
            new XElement("encoding", "24"),
            new XElement("unicast_latency", "1000"),
            new XElement("interface", new XAttribute("network", "0"), new XElement("ipv4_address", new XAttribute("mode", "dynamic"))),
            children);
    }

    private static XElement Tx(int danteId, string label)
    {
        return new XElement(
            "txchannel",
            new XAttribute("danteId", danteId),
            new XAttribute("mediaType", "audio"),
            new XElement("label", label));
    }

    private static XElement Rx(int danteId, string name, string? txDevice = null, string? txChannel = null)
    {
        XElement element = new(
            "rxchannel",
            new XAttribute("danteId", danteId),
            new XAttribute("mediaType", "audio"),
            new XElement("name", name));
        if (txChannel is not null)
        {
            element.Add(new XElement("subscribed_channel", txChannel));
        }

        if (txDevice is not null)
        {
            element.Add(new XElement("subscribed_device", txDevice));
        }

        return element;
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.CompatibilityTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string PathFor(string name) => Path.Combine(Root, name);

        public string CopyFixture(string name)
        {
            string destination = PathFor(name);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), destination, true);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
