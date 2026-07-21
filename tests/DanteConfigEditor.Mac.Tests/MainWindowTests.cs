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
    public void OfficialV33VersionIsShownInMacApplication()
    {
        string version = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? string.Empty;
        MainWindow window = new();
        window.Show();
        try
        {
            Assert.Equal("3.3", version);
            Assert.Equal("Dante Config Editor V3.3 - macOS", window.Title);
            Assert.Equal("Add XML", LocalizationService.TranslateLiteral(UiLanguage.English, "Ajouter XML"));
            Assert.Equal("Device or channel", LocalizationService.TranslateLiteral(UiLanguage.English, "Machine ou canal"));
            Assert.Equal("All", LocalizationService.TranslateLiteral(UiLanguage.English, "Toutes"));
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
    public async Task AtomicButtonBecomesAvailableInDedicatedTabAfterProjectLoad()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-atomic-layout-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = 1366, Height = 768 };
        window.Show();
        try
        {
            Button atomicButton = window.FindControl<Button>("AtomicChaosButton")!;
            Assert.False(atomicButton.IsEnabled);

            await window.OpenStartupFileAsync(temporaryXml);
            TabItem safetyTab = window.FindControl<TabItem>("SafetyTab")!;
            TabItem atomicTab = window.FindControl<TabItem>("AtomicTab")!;
            atomicTab.IsSelected = true;
            Dispatcher.UIThread.RunJobs();

            Assert.True(atomicButton.IsEnabled);
            Assert.True(MainTabs(window).Items.IndexOf(atomicTab) > MainTabs(window).Items.IndexOf(safetyTab));
            AssertControlFits(window, atomicButton);
            foreach (string checkBoxName in new[]
            {
                "AtomicDeviceNamesCheckBox", "AtomicTxLabelsCheckBox", "AtomicRxLabelsCheckBox",
                "AtomicPatchesCheckBox", "AtomicNetworkModeCheckBox", "AtomicPreferredMasterCheckBox",
                "AtomicLatencyCheckBox", "AtomicSampleRateCheckBox", "AtomicEncodingCheckBox", "AtomicIpCheckBox"
            })
            {
                CheckBox checkBox = window.FindControl<CheckBox>(checkBoxName)!;
                Assert.True(checkBox.IsChecked);
                AssertControlFits(window, checkBox);
            }
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    [AvaloniaFact]
    public async Task LabelExportForRxOnlyDeviceOpensDirectlyOnRx()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        string temporaryXml = Path.Combine(Path.GetTempPath(), $"dante-mac-label-export-{Guid.NewGuid():N}.xml");
        File.Copy(source, temporaryXml);

        MainWindow window = new() { Width = 1366, Height = 768 };
        window.Show();
        try
        {
            await window.OpenStartupFileAsync(temporaryXml);
            DataGrid devices = window.FindControl<DataGrid>("DeviceGrid")!;
            devices.SelectedIndex = 1; // DEVICE-B : 0 TX, 2 RX.

            window.FindControl<Button>("ExportChannelLabelsButton")!
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Window dialog = Assert.Single(window.OwnedWindows);
            Assert.Equal(1, dialog.FindControl<ComboBox>("KindCombo")!.SelectedIndex);
            Assert.True(dialog.FindControl<Button>("ExportButton")!.IsEnabled);
            Assert.Contains("2", dialog.FindControl<TextBlock>("SummaryText")!.Text);
            dialog.Close();
        }
        finally
        {
            window.Close();
            SessionRecoveryService.Delete(temporaryXml);
            File.Delete(temporaryXml);
        }
    }

    [AvaloniaFact]
    public async Task ReimportingIdenticalLabelsExplainsWhyApplyIsDisabledAndKeepsButtonsVisible()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "representative-preset.xml");
        DanteProject project = DanteProject.Load(source);
        ChannelLabelDocument document = ChannelLabelExchangeService.CreateFromProject(
            project,
            ["DEVICE-A"],
            DanteChannelKind.Tx);
        MainWindow owner = new() { Width = 1920, Height = 1080 };
        owner.Show();

        Task<IReadOnlyList<ChannelLabelAssignment>?> resultTask = ChannelLabelImportDialog.ShowAsync(
            owner,
            project,
            document,
            UiLanguage.English,
            "DEVICE-A");
        Dispatcher.UIThread.RunJobs();

        Window dialog = Assert.Single(owner.OwnedWindows);
        try
        {
            Button preview = dialog.FindControl<Button>("PreviewButton")!;
            Button apply = dialog.FindControl<Button>("ApplyButton")!;
            preview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            AssertControlFits(dialog, preview);
            AssertControlFits(dialog, apply);
            Assert.True(preview.Bounds.Height >= 34);
            Assert.False(apply.IsEnabled);
            Assert.Contains("0 change", dialog.FindControl<TextBlock>("PreviewSummaryText")!.Text);
            Assert.Contains("already match", dialog.FindControl<TextBlock>("SafetyText")!.Text);
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }

        Assert.Null(await resultTask);
    }

    private static TabControl MainTabs(MainWindow window) => window.FindControl<TabControl>("MainTabs")!;

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

            window.FindControl<TabItem>("ExportsTab")!.IsSelected = true;
            window.FindControl<TabItem>("ChannelLabelsTab")!.IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            AssertControlFits(window, window.FindControl<Button>("ImportChannelLabelsButton")!);
            AssertControlFits(window, window.FindControl<Button>("ExportChannelLabelsButton")!);
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
