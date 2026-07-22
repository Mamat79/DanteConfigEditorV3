using System.Xml.Linq;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class PersistenceWorkflowTests
{
    [Fact]
    public void ConsecutiveSaveAsOperationsKeepEachDestinationAndTrackTheLatest()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string firstDestination = directory.PathFor("first-save.xml");
        string secondDestination = directory.PathFor("second-save.xml");
        DanteProject project = DanteProject.Load(source);

        project.SetLatency("DEVICE-C", "2000");
        project.SaveAs(firstDestination);
        project.SetEncoding("DEVICE-C", "32");
        project.SaveAs(secondDestination);

        DanteDevice firstSavedDevice = Assert.IsType<DanteDevice>(DanteProject.Load(firstDestination).FindDevice("DEVICE-C"));
        DanteDevice secondSavedDevice = Assert.IsType<DanteDevice>(DanteProject.Load(secondDestination).FindDevice("DEVICE-C"));
        Assert.Equal("2000", firstSavedDevice.Latency);
        Assert.Equal("24", firstSavedDevice.Encoding);
        Assert.Equal("2000", secondSavedDevice.Latency);
        Assert.Equal("32", secondSavedDevice.Encoding);
        Assert.Equal(Path.GetFullPath(secondDestination), project.OriginalFilePath);
        Assert.Equal(Path.GetFullPath(secondDestination), project.LastSavedPath);
        Assert.False(project.IsModified);
    }

    [Fact]
    public async Task RecoveryAfterConsecutiveSaveUsesTheLatestDestination()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string firstDestination = directory.PathFor("first-save.xml");
        string secondDestination = directory.PathFor("second-save.xml");
        string recoveryDirectory = directory.PathFor("recovery");
        DanteProject project = DanteProject.Load(source);

        project.SetLatency("DEVICE-C", "2000");
        project.SaveAs(firstDestination);
        project.SetEncoding("DEVICE-C", "32");
        project.SaveAs(secondDestination);
        project.RenameChannel("DEVICE-C", DanteChannelKind.Tx, 1, "RECOVERED-TX");
        await SessionRecoveryService.SaveAsync(project, recoveryDirectory);

        Assert.Null(SessionRecoveryService.Find(firstDestination, recoveryDirectory));
        RecoveryCandidate candidate = Assert.IsType<RecoveryCandidate>(SessionRecoveryService.Find(secondDestination, recoveryDirectory));
        Assert.True(candidate.SourceMatches);
        DanteProject recovered = DanteProject.LoadRecovered(secondDestination, candidate.RecoveryXmlPath);
        Assert.Equal("RECOVERED-TX", Assert.Single(recovered.FindDevice("DEVICE-C")!.TxChannels).DisplayName);
    }

    [Fact]
    public void MergeWithManualDuplicateRenameUpdatesImportedPatchReferences()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string imported = directory.PathFor("merge-with-reference.xml");
        string destination = directory.PathFor("merged.xml");
        BuildMergePreset().Save(imported, SaveOptions.DisableFormatting);
        DanteProject project = DanteProject.Load(source);

        DanteMergeResult result = project.MergeDevicesFromXml(
            imported,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["DEVICE-A"] = "DEVICE-A-IMPORTED" });

        Assert.Equal(2, result.ImportedDeviceCount);
        Assert.Equal(1, result.RenamedDeviceCount);
        Assert.NotNull(project.FindDevice("DEVICE-A-IMPORTED"));
        DanteSubscription importedSubscription = Assert.Single(
            project.PatchMatrix.Subscriptions,
            subscription => subscription.RxDevice == "IMPORTED-RX");
        Assert.Equal("DEVICE-A-IMPORTED", importedSubscription.RawTxDeviceName);
        Assert.Equal(DanteSubscriptionKind.Normal, importedSubscription.Kind);
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);

        project.SaveAs(destination);
        DanteSubscription reloadedSubscription = Assert.Single(
            DanteProject.Load(destination).PatchMatrix.Subscriptions,
            subscription => subscription.RxDevice == "IMPORTED-RX");
        Assert.Equal("DEVICE-A-IMPORTED", reloadedSubscription.RawTxDeviceName);
    }

    [Fact]
    public void AutomaticDuplicateRenameUsesCustomSuffixWithoutParentheses()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string imported = directory.PathFor("merge-with-reference.xml");
        BuildMergePreset().Save(imported, SaveOptions.DisableFormatting);
        DanteProject project = DanteProject.Load(source);

        IReadOnlyDictionary<string, string> renameMap = project.BuildAutomaticDuplicateRenameMap(imported, "Formation");

        Assert.Equal("DEVICE-A-Formation", renameMap["DEVICE-A"]);
        Assert.DoesNotContain('(', renameMap["DEVICE-A"]);
        Assert.DoesNotContain(')', renameMap["DEVICE-A"]);
    }

    private static XDocument BuildMergePreset()
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(
                "preset",
                new XAttribute("version", "3.0.0"),
                new XElement("name", "Merge reference test"),
                Device(
                    "DEVICE-A",
                    "MERGE-0001",
                    new XElement(
                        "txchannel",
                        new XAttribute("danteId", "1"),
                        new XAttribute("mediaType", "audio"),
                        new XElement("label", "IMPORTED-TX"))),
                Device(
                    "IMPORTED-RX",
                    "MERGE-0002",
                    new XElement(
                        "rxchannel",
                        new XAttribute("danteId", "1"),
                        new XAttribute("mediaType", "audio"),
                        new XElement("name", "IMPORTED-INPUT"),
                        new XElement("subscribed_channel", "IMPORTED-TX"),
                        new XElement("subscribed_device", "DEVICE-A")))));
    }

    private static XElement Device(string name, string identifier, XElement channel)
    {
        return new XElement(
            "device",
            new XElement("name", name),
            new XElement("friendly_name", name),
            new XElement("instance_id", new XElement("device_id", identifier), new XElement("process_id", "0")),
            new XElement("preferred_master", new XAttribute("value", "false")),
            new XElement("redundancy", new XAttribute("value", "false")),
            new XElement("samplerate", "48000"),
            new XElement("encoding", "24"),
            new XElement("unicast_latency", "1000"),
            new XElement("interface", new XAttribute("network", "0"), new XElement("ipv4_address", new XAttribute("mode", "dynamic"))),
            channel);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.PersistenceTests", Guid.NewGuid().ToString("N"));
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
