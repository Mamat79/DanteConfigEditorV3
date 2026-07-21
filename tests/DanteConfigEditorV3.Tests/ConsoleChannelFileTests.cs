using System.IO.Compression;
using System.Text;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class ConsoleChannelFileTests
{
    [Fact]
    public void AllenHeathCsvIsDetectedAndRead()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("Avantis.csv");
        File.WriteAllText(source, AllenHeathCsv(), Encoding.Latin1);

        ChannelLabelDocument document = ChannelLabelExchangeService.Read(source);

        ChannelLabelSet set = Assert.Single(document.Sets);
        Assert.Equal(ChannelLabelDirection.ConsoleInput, set.Direction);
        Assert.Equal("Kick, In", set.Channels[0].Label);
        Assert.Equal("Allen & Heath Avantis", document.SourceApplication);
    }

    [Fact]
    public void AllenHeathExportChangesOnlyRequestedInputNamesInACopy()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("dLive.csv");
        string output = directory.PathFor("dLive-export.csv");
        File.WriteAllText(source, AllenHeathCsv("MixRack"), Encoding.Latin1);
        byte[] original = File.ReadAllBytes(source);

        ConsoleChannelFileService.WriteCopy(source, output, Labels((1, "TrèsLongLabel"), (2, "Snare")), adaptLabels: true);

        Assert.Equal(original, File.ReadAllBytes(source));
        string exported = File.ReadAllText(output, Encoding.Latin1);
        Assert.Contains("Input,1,TresLong,Green,MixRack,1", exported, StringComparison.Ordinal);
        Assert.Contains("Input,2,Snare,Green,MixRack,2", exported, StringComparison.Ordinal);
        Assert.Contains("[Other],preserved", exported, StringComparison.Ordinal);
    }

    [Fact]
    public void YamahaZipImportReadsInNameAndExportPreservesOtherEntries()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("Yamaha-CL.zip");
        string output = directory.PathFor("Yamaha-CL-export.zip");
        byte[] dca = Encoding.Latin1.GetBytes("[Information]\r\nCL5\r\n[DCAName]\r\nDCA,NAME\r\n_01,DCA 1\r\n");
        CreateYamahaZip(source, YamahaInputCsv("CL5"), dca);

        ChannelLabelDocument imported = ChannelLabelExchangeService.Read(source);
        Assert.Equal(["ch 1", "ch 2"], imported.Sets.Single().Channels.Select(channel => channel.Label));

        ConsoleChannelFileService.WriteCopy(source, output, Labels((1, "Lead Vox"), (2, "Chœur")), adaptLabels: true);

        using ZipArchive archive = ZipFile.OpenRead(output);
        Assert.Equal(dca, ReadEntry(archive, "DCAName.csv"));
        string inName = Encoding.Latin1.GetString(ReadEntry(archive, "InName.csv"));
        Assert.Contains("_01,Lead Vox,Blue,Dynamic,", inName, StringComparison.Ordinal);
        Assert.Contains("_02,Choeur,Blue,Dynamic,", inName, StringComparison.Ordinal);
    }

    [Fact]
    public void YamahaIndividualInNameCsvIsSupported()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("InName.csv");
        string output = directory.PathFor("InName-export.csv");
        File.WriteAllText(source, YamahaInputCsv("QL5"), Encoding.Latin1);

        ConsoleChannelFileReadResult info = ConsoleChannelFileService.Read(source);
        ConsoleChannelFileService.WriteCopy(source, output, Labels((2, "Violin")), adaptLabels: false);

        Assert.Equal("Yamaha QL5", info.TemplateName);
        Assert.Contains("_02,Violin,Blue,Dynamic,", File.ReadAllText(output, Encoding.Latin1), StringComparison.Ordinal);
    }

    [Fact]
    public void ConsoleExportRejectsAChannelMissingFromTheTemplate()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("Avantis.csv");
        string output = directory.PathFor("output.csv");
        File.WriteAllText(source, AllenHeathCsv(), Encoding.Latin1);
        File.WriteAllText(output, "existing destination", Encoding.UTF8);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            ConsoleChannelFileService.WriteCopy(source, output, Labels((99, "Missing")), adaptLabels: false));

        Assert.Contains("99", error.Message, StringComparison.Ordinal);
        Assert.Equal("existing destination", File.ReadAllText(output, Encoding.UTF8));
    }

    private static string AllenHeathCsv(string source = "SLink") =>
        $"[Version],V1.0,,,,\r\n[Channels],,,,,\r\nInput,1,\"Kick, In\",Green,{source},1\r\nInput,2,Snare,Green,{source},2\r\n[Other],preserved\r\n";

    private static string YamahaInputCsv(string model) =>
        $"[Information]\r\n{model}\r\nV4.1\r\n[InName]\r\nIN,NAME,COLOR,ICON,\r\n_01,ch 1,Blue,Dynamic,\r\n_02,ch 2,Blue,Dynamic,\r\n";

    private static ChannelLabelSet Labels(params (int Channel, string Label)[] labels) =>
        new("Console", ChannelLabelDirection.ConsoleInput,
            labels.Select(item => new ChannelLabelEntry(item.Channel, item.Label)).ToArray());

    private static void CreateYamahaZip(string path, string inName, byte[] dca)
    {
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(archive, "InName.csv", Encoding.Latin1.GetBytes(inName));
        WriteEntry(archive, "DCAName.csv", dca);
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using Stream stream = entry.Open();
        stream.Write(content);
    }

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        using Stream stream = archive.GetEntry(name)!.Open();
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
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
            }
        }
    }
}
