using DanteConfigEditor.Models;
using DanteConfigEditor.Services;
using DanteConfigEditorV3.TestSupport;

namespace DanteConfigEditorV3.Tests;

public sealed class ChannelNameSeriesTests
{
    [Fact]
    public void ExtendsExcelStyleSeriesWithoutAddingLeadingZeroes()
    {
        ChannelSeriesValue[] channels = Enumerable.Range(1, 5)
            .Select(index => new ChannelSeriesValue(index, index <= 2 ? $"Mic {index}" : index.ToString()))
            .ToArray();

        IReadOnlyList<ChannelSeriesValue> result = ChannelNameSeriesService.Extend(channels, [1, 2], 5);

        Assert.Equal(["Mic 3", "Mic 4", "Mic 5"], result.Select(item => item.Name));
    }

    [Fact]
    public void ExtendsASingleNumberedSeedWithAUnitStep()
    {
        ChannelSeriesValue[] channels = Enumerable.Range(1, 5)
            .Select(index => new ChannelSeriesValue(index, index == 1 ? "Micro 1" : index.ToString()))
            .ToArray();

        IReadOnlyList<ChannelSeriesValue> result = ChannelNameSeriesService.Extend(channels, [1], 5);

        Assert.Equal(["Micro 2", "Micro 3", "Micro 4", "Micro 5"], result.Select(item => item.Name));
    }

    [Fact]
    public void PreservesExplicitNumericPaddingAndStep()
    {
        ChannelSeriesValue[] channels =
        [
            new(1, "HF 01"),
            new(2, "HF 03"),
            new(3, "3"),
            new(4, "4")
        ];

        IReadOnlyList<ChannelSeriesValue> result = ChannelNameSeriesService.Extend(channels, [1, 2], 4);

        Assert.Equal(["HF 05", "HF 07"], result.Select(item => item.Name));
    }

    [Fact]
    public void RejectsNonConsecutiveSeeds()
    {
        ChannelSeriesValue[] channels =
        [
            new(1, "Mic 1"),
            new(2, "Other"),
            new(3, "Mic 2"),
            new(4, "4")
        ];

        Assert.Throws<InvalidOperationException>(() => ChannelNameSeriesService.Extend(channels, [1, 3], 4));
    }

    [Fact]
    public void ProjectSeriesRenameUpdatesTxSubscriptions()
    {
        using TestDirectory directory = new();
        string source = directory.PathFor("series.xml");
        SyntheticPresetFactory.Create(source, deviceCount: 2, txPerDevice: 4, rxPerDevice: 4);
        DanteProject project = DanteProject.Load(source);
        project.RenameChannel("DEVICE-001", DanteChannelKind.Tx, 1, "Mic 1");
        project.RenameChannel("DEVICE-001", DanteChannelKind.Tx, 2, "Mic 2");

        project.ExtendChannelNameSeries("DEVICE-001", DanteChannelKind.Tx, [1, 2], 4);

        DanteDevice device = Assert.Single(project.Devices, item => item.Name == "DEVICE-001");
        Assert.Equal(["Mic 1", "Mic 2", "Mic 3", "Mic 4"], device.TxChannels.Take(4).Select(channel => channel.DisplayName));
        Assert.Contains(project.PatchMatrix.Subscriptions, subscription => subscription.TxChannelName == "Mic 3");
        Assert.Contains(project.PatchMatrix.Subscriptions, subscription => subscription.TxChannelName == "Mic 4");
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.SeriesTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        private string Root { get; }

        public string PathFor(string fileName) => Path.Combine(Root, fileName);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
