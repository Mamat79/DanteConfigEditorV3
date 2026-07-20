using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac.Tests;

public sealed class MainWindowTests
{
    [AvaloniaFact]
    public void OfficialV309VersionIsShownInMacApplication()
    {
        string version = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? string.Empty;
        MainWindow window = new();
        window.Show();
        try
        {
            Assert.Equal("3.09", version);
            Assert.Equal("Dante Config Editor V3.09 - macOS", window.Title);
        }
        finally
        {
            window.Close();
        }
    }

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

    [AvaloniaFact]
    public async Task Compact1366By768LayoutKeepsPrimaryAreasInsideTheWindow()
    {
        await AssertPrimaryAreasFitAsync(1366, 768);
    }

    [AvaloniaFact]
    public async Task FullHdLayoutKeepsPrimaryAreasInsideTheWindow()
    {
        await AssertPrimaryAreasFitAsync(1920, 1080);
    }

    [AvaloniaFact]
    public async Task CompactPatchToolbarKeepsVisualWorkspaceButtonVisible()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-patch-layout-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = 1366, Height = 768 };
        window.Show();
        try
        {
            await window.OpenStartupFileAsync(temporaryXml);
            window.FindControl<TabItem>("PatchTab")!.IsSelected = true;
            Dispatcher.UIThread.RunJobs();

            AssertControlFits(window, window.FindControl<Button>("VisualPatchButton")!);
            AssertControlFits(window, window.FindControl<DataGrid>("PatchGrid")!);
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    [AvaloniaFact]
    public async Task AtomicButtonBecomesAvailableAfterProjectLoad()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-atomic-layout-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = 1366, Height = 768 };
        window.Show();
        try
        {
            Button atomicButton = window.FindControl<Button>("AtomicChaosButton")!;
            Button sidebarAtomicButton = window.FindControl<Button>("AtomicChaosSidebarButton")!;
            Assert.False(atomicButton.IsEnabled);
            Assert.False(sidebarAtomicButton.IsEnabled);

            await window.OpenStartupFileAsync(temporaryXml);
            window.FindControl<TabItem>("SafetyTab")!.IsSelected = true;
            Dispatcher.UIThread.RunJobs();

            Assert.True(atomicButton.IsEnabled);
            Assert.True(sidebarAtomicButton.IsEnabled);
            AssertControlFits(window, atomicButton);
            AssertControlFits(window, sidebarAtomicButton);
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    [AvaloniaFact]
    public async Task TabKeyMovesFocusFromOpenToMergeAfterProjectLoad()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-keyboard-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = 1366, Height = 768 };
        window.Show();
        try
        {
            await window.OpenStartupFileAsync(temporaryXml);
            Button open = window.FindControl<Button>("OpenButton")!;
            Button merge = window.FindControl<Button>("MergeButton")!;

            Assert.True(open.Focus());
            Assert.True(open.IsFocused);
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);

            Assert.True(merge.IsFocused);
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    [AvaloniaFact]
    public void VisualPatchDialogStagesMatrixEditWithoutChangingProject()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        DanteProject project = DanteProject.Load(source);
        PatchWorkspaceDialog dialog = new(
            UiLanguage.French,
            project,
            initialTxDeviceName: "DEVICE-A",
            initialRxDeviceName: "DEVICE-B")
        {
            Width = 960,
            Height = 640
        };
        dialog.Show();

        try
        {
            ListBox txList = dialog.FindControl<ListBox>("TxChannelList")!;
            ListBox rxList = dialog.FindControl<ListBox>("RxChannelList")!;
            Grid matrix = dialog.FindControl<Grid>("MatrixPanel")!;
            Button apply = dialog.FindControl<Button>("ApplyButton")!;

            Assert.Equal(2, txList.ItemCount);
            Assert.Equal(2, rxList.ItemCount);
            Assert.Equal(3, matrix.ColumnDefinitions.Count);
            Assert.Equal(3, matrix.RowDefinitions.Count);
            Assert.False(apply.IsEnabled);
            Assert.Empty(dialog.Edits);
            Assert.False(project.IsModified);

            Button activeCell = matrix.Children.OfType<Button>().First(button => Equals(button.Content, "●"));
            activeCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(apply.IsEnabled);
            PatchEditRequest edit = Assert.Single(dialog.Edits);
            Assert.True(edit.IsRemoval);
            Assert.Equal("DEVICE-B", edit.RxDeviceName);
            Assert.False(project.IsModified);

            dialog.FindControl<TabItem>("MatrixTab")!.IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            AssertControlFits(dialog, matrix);
        }
        finally
        {
            dialog.Close();
        }
    }

    private static async Task AssertPrimaryAreasFitAsync(double width, double height)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-layout-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = width, Height = height };
        window.Show();
        try
        {
            await window.OpenStartupFileAsync(temporaryXml);

            AssertControlFits(window, window.FindControl<Border>("ProjectSidebar")!);
            AssertControlFits(window, window.FindControl<TabControl>("MainTabs")!);
            AssertControlFits(window, window.FindControl<DataGrid>("DeviceGrid")!);
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    private static void AssertControlFits(Window window, Control control)
    {
        Point? origin = control.TranslatePoint(default, window);
        Assert.NotNull(origin);
        Assert.True(control.IsEffectivelyVisible, $"{control.Name} devrait être visible.");
        Assert.True(control.Bounds.Width > 0, $"{control.Name} devrait avoir une largeur positive.");
        Assert.True(control.Bounds.Height > 0, $"{control.Name} devrait avoir une hauteur positive.");
        Assert.InRange(origin.Value.X, -0.5, window.ClientSize.Width + 0.5);
        Assert.InRange(origin.Value.Y, -0.5, window.ClientSize.Height + 0.5);
        Assert.True(origin.Value.X + control.Bounds.Width <= window.ClientSize.Width + 0.5, $"{control.Name} dépasse horizontalement.");
        Assert.True(origin.Value.Y + control.Bounds.Height <= window.ClientSize.Height + 0.5, $"{control.Name} dépasse verticalement.");
    }
}
