using System.IO.Compression;
using System.Text;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class BuiltInChannelLabelTemplateTests
{
    [Theory]
    [InlineData("dmt-dlive")]
    [InlineData("dmt-avantis")]
    [InlineData("dmt-ods-dlive")]
    [InlineData("dmt-ods-avantis")]
    [InlineData("ah-dlive")]
    [InlineData("ah-avantis")]
    [InlineData("yamaha-cl")]
    [InlineData("yamaha-ql")]
    public void EveryNativeTemplateIsEmbedded(string format)
    {
        using Stream template = BuiltInChannelLabelTemplateService.OpenTemplate(format);
        using MemoryStream copy = new();
        template.CopyTo(copy);

        Assert.True(copy.Length > 100);
    }

    [Theory]
    [InlineData("dmt-dlive")]
    [InlineData("dmt-avantis")]
    public void DmtNativeExportContainsOnlyRequestedChannelNames(string format)
    {
        using TestDirectory directory = new();
        string output = directory.PathFor($"{format}.xlsx");

        BuiltInChannelLabelTemplateService.Write(
            format,
            output,
            Labels((1, "TrèsLongLabel"), (2, "Chœur")),
            adaptLabels: true);

        DmtWorkbookReadResult result = DmtChannelWorkbookService.Read(output);
        Assert.Equal([1, 2], result.Document.Sets.Single().Channels.Select(channel => channel.ChannelNumber));
        Assert.Equal(["TresLong", "Choeur"], result.Document.Sets.Single().Channels.Select(channel => channel.Label));
    }

    [Theory]
    [InlineData("dmt-ods-dlive")]
    [InlineData("dmt-ods-avantis")]
    public void DmtOdsNativeExportCanBeReimportedAndPreservesThePackage(string format)
    {
        using TestDirectory directory = new();
        string output = directory.PathFor($"{format}.ods");

        BuiltInChannelLabelTemplateService.Write(
            format,
            output,
            Labels((1, "TrèsLongLabel"), (2, "Chœur")),
            adaptLabels: true);

        DmtWorkbookReadResult result = DmtOpenDocumentService.Read(output);
        Assert.Equal([1, 2], result.Document.Sets.Single().Channels.Select(channel => channel.ChannelNumber));
        Assert.Equal(["TresLong", "Choeur"], result.Document.Sets.Single().Channels.Select(channel => channel.Label));
        Assert.Equal(["TresLong", "Choeur"], ChannelLabelExchangeService.Read(output).Sets.Single().Channels.Select(channel => channel.Label));

        using Stream source = BuiltInChannelLabelTemplateService.OpenTemplate(format);
        using ZipArchive sourceArchive = new(source, ZipArchiveMode.Read);
        using ZipArchive outputArchive = ZipFile.OpenRead(output);
        Assert.Equal(sourceArchive.Entries.Select(entry => entry.FullName).Order(), outputArchive.Entries.Select(entry => entry.FullName).Order());
        foreach (string entryName in new[] { "styles.xml", "settings.xml", "META-INF/manifest.xml" })
        {
            Assert.Equal(ReadEntry(sourceArchive.GetEntry(entryName)!), ReadEntry(outputArchive.GetEntry(entryName)!));
        }
    }

    [Theory]
    [InlineData("ah-dlive", "Allen & Heath dLive")]
    [InlineData("ah-avantis", "Allen & Heath Avantis")]
    public void AllenHeathNativeExportKeepsDirectorHeaderAndUpdatesNames(string format, string expectedTemplate)
    {
        using TestDirectory directory = new();
        string output = directory.PathFor($"{format}.csv");

        BuiltInChannelLabelTemplateService.Write(
            format,
            output,
            Labels((1, "Lead Vox"), (2, "Chœur")),
            adaptLabels: true);

        string text = File.ReadAllText(output, Encoding.Latin1);
        Assert.StartsWith("[Version],V1.0", text, StringComparison.Ordinal);
        ConsoleChannelFileReadResult result = ConsoleChannelFileService.Read(output);
        Assert.Equal(expectedTemplate, result.TemplateName);
        IReadOnlyDictionary<int, string> names = result.Document.Sets.Single().Channels
            .ToDictionary(channel => channel.ChannelNumber, channel => channel.Label);
        Assert.Equal("Lead Vox", names[1]);
        Assert.Equal("Choeur", names[2]);
    }

    [Theory]
    [InlineData("yamaha-cl", "Yamaha CL5")]
    [InlineData("yamaha-ql", "Yamaha QL5")]
    public void YamahaNativeExportPreservesPackageAndUpdatesInName(string format, string expectedTemplate)
    {
        using TestDirectory directory = new();
        string output = directory.PathFor($"{format}.zip");

        BuiltInChannelLabelTemplateService.Write(
            format,
            output,
            Labels((1, "Lead Vox"), (2, "Chœur")),
            adaptLabels: true);

        ConsoleChannelFileReadResult result = ConsoleChannelFileService.Read(output);
        Assert.Equal(expectedTemplate, result.TemplateName);
        Assert.Equal("Lead Vox", result.Document.Sets.Single().Channels.Single(channel => channel.ChannelNumber == 1).Label);
        Assert.Equal("Choeur", result.Document.Sets.Single().Channels.Single(channel => channel.ChannelNumber == 2).Label);

        using Stream source = BuiltInChannelLabelTemplateService.OpenTemplate(format);
        using ZipArchive sourceArchive = new(source, ZipArchiveMode.Read);
        using ZipArchive outputArchive = ZipFile.OpenRead(output);
        Assert.Equal(sourceArchive.Entries.Select(entry => entry.FullName).Order(), outputArchive.Entries.Select(entry => entry.FullName).Order());
        foreach (ZipArchiveEntry sourceEntry in sourceArchive.Entries.Where(entry => !entry.Name.Equals("InName.csv", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.Equal(ReadEntry(sourceEntry), ReadEntry(outputArchive.GetEntry(sourceEntry.FullName)!));
        }
    }

    private static ChannelLabelSet Labels(params (int Channel, string Label)[] labels) =>
        new("Dante device", ChannelLabelDirection.ConsoleInput,
            labels.Select(item => new ChannelLabelEntry(item.Channel, item.Label)).ToArray());

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

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
