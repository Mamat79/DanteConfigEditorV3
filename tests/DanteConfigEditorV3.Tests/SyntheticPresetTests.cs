using DanteConfigEditor.Models;
using DanteConfigEditorV3.TestSupport;

namespace DanteConfigEditorV3.Tests;

public sealed class SyntheticPresetTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void LargePresetRoundTripPreservesAllDevicesAndChannels(int deviceCount)
    {
        string directory = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.SyntheticTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, $"synthetic-{deviceCount}.xml");
            string destination = Path.Combine(directory, $"synthetic-{deviceCount}-saved.xml");
            SyntheticPresetFactory.Create(source, deviceCount);

            DanteProject project = DanteProject.Load(source);
            Assert.Equal(deviceCount, project.Devices.Count);
            Assert.All(project.Devices, device =>
            {
                Assert.Equal(64, device.TxCount);
                Assert.Equal(64, device.RxCount);
            });
            Assert.Equal(deviceCount * 64, project.PatchMatrix.Subscriptions.Count);

            project.SetAllLatencies("2000");
            Assert.False(project.ValidateXmlChangeGuard().HasErrors);
            project.SaveAs(destination);

            DanteProject reloaded = DanteProject.Load(destination);
            Assert.Equal(deviceCount, reloaded.Devices.Count);
            Assert.All(reloaded.Devices, device => Assert.Equal("2000", device.Latency));
            Assert.Equal(deviceCount * 64, reloaded.PatchMatrix.Subscriptions.Count);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
