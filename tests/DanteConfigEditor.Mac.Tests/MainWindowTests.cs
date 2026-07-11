using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac.Tests;

public sealed class MainWindowTests
{
    [AvaloniaFact]
    public void ImportantWarnings_AreKeptInsideProjectSidebar()
    {
        MainWindow window = new();
        window.Show();
        try
        {
            Border sidebar = window.FindControl<Border>("ProjectSidebar")!;
            Border warning = window.FindControl<Border>("WarningBorder")!;
            TabControl tabs = window.FindControl<TabControl>("MainTabs")!;

            Assert.Contains(warning.GetLogicalAncestors(), ancestor => ReferenceEquals(ancestor, sidebar));
            Assert.Equal(0, Grid.GetColumn(sidebar));
            Assert.Equal(1, Grid.GetColumn(tabs));
            Assert.False(warning.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MixedAudioFormats_ShowWarningInProjectSidebar()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-ui-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new();
        window.Show();
        try
        {
            await window.OpenStartupFileAsync(temporaryXml);

            Border sidebar = window.FindControl<Border>("ProjectSidebar")!;
            Border warning = window.FindControl<Border>("WarningBorder")!;
            TextBlock warningText = window.FindControl<TextBlock>("WarningText")!;
            TextBlock summary = window.FindControl<TextBlock>("ProjectSummaryText")!;

            Assert.True(warning.IsVisible);
            Assert.False(string.IsNullOrWhiteSpace(warningText.Text));
            Assert.Contains("3", summary.Text);
            Assert.Contains(warning.GetLogicalAncestors(), ancestor => ReferenceEquals(ancestor, sidebar));
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }
}
