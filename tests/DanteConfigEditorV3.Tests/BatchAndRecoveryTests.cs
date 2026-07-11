using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class BatchAndRecoveryTests
{
    [Fact]
    public void BatchMutationSupportsRenameSettingsAndChannelsAsOneModelRefresh()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        DanteProject project = DanteProject.Load(source);

        project.ApplyBatch(batch =>
        {
            batch.RenameDevice("DEVICE-A", "DEVICE-A-BATCH");
            batch.SetNetworkMode("DEVICE-A-BATCH", true);
            batch.SetLatency("DEVICE-A-BATCH", "2000");
            batch.RenameChannel("DEVICE-A-BATCH", DanteChannelKind.Tx, 1, "BATCH-L");
            batch.RenameChannel("DEVICE-A-BATCH", DanteChannelKind.Tx, 2, "BATCH-R");
        });

        DanteDevice device = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-A-BATCH"));
        Assert.True(device.IsRedundant);
        Assert.Equal("2000", device.Latency);
        Assert.Equal(["BATCH-L", "BATCH-R"], device.TxChannels.Select(channel => channel.DisplayName).ToArray());
        Assert.Contains(project.PatchMatrix.Subscriptions, subscription => subscription.TxChannelName == "BATCH-L");
        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
    }

    [Fact]
    public void UndoHistoryIsLimitedToConfiguredMaximum()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));

        for (int index = 0; index < DanteProject.MaximumUndoSnapshots + 5; index++)
        {
            project.PushUndoSnapshot($"snapshot-{index}");
        }

        int undoCount = 0;
        while (project.CanUndo)
        {
            project.UndoLastChange();
            undoCount++;
        }

        Assert.Equal(DanteProject.MaximumUndoSnapshots, undoCount);
    }

    [Fact]
    public async Task AsynchronousRecoveryRoundTripPreservesSnapshot()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string recoveryDirectory = directory.PathFor("recovery");
        DanteProject project = DanteProject.Load(source);
        project.SetEncoding("DEVICE-C", "32");

        await SessionRecoveryService.SaveAsync(project, recoveryDirectory);

        RecoveryCandidate candidate = Assert.IsType<RecoveryCandidate>(SessionRecoveryService.Find(source, recoveryDirectory));
        DanteProject recovered = DanteProject.LoadRecovered(source, candidate.RecoveryXmlPath);
        Assert.Equal("32", recovered.FindDevice("DEVICE-C")?.Encoding);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.BatchTests", Guid.NewGuid().ToString("N"));
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
