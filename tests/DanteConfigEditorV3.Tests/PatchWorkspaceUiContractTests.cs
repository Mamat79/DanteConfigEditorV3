namespace DanteConfigEditorV3.Tests;

public sealed class PatchWorkspaceUiContractTests
{
    [Fact]
    public void WindowsPatchWorkspaceUsesSelectionPreviewAndRangeControls()
    {
        string xaml = File.ReadAllText(RepositoryFile("PatchWorkspaceWindow.xaml"));
        string codeBehind = File.ReadAllText(RepositoryFile("PatchWorkspaceWindow.xaml.cs"));

        Assert.Contains("x:Name=\"TxChannelListBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RxChannelListBox\"", xaml, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(xaml, "SelectionMode=\"Extended\""));
        Assert.Contains("x:Name=\"PreviewGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConflictResolutionComboBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RangeStartTxComboBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RangeStartRxComboBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RangeCountTextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MatrixGrid\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowDrop=", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DragDrop.DoDragDrop", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void DeviceDetailsExposesRxPatchWorkspaceAndAppliesPatchesBeforeRenames()
    {
        string xaml = File.ReadAllText(RepositoryFile("DeviceDetailsWindow.xaml"));
        string codeBehind = File.ReadAllText(RepositoryFile("DeviceDetailsWindow.xaml.cs"));
        string mainWindow = File.ReadAllText(RepositoryFile("MainWindow.xaml.cs"));

        Assert.Contains("x:Name=\"PatchTab\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OpenPatchWorkspaceButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("returnEditsOnly: true", codeBehind, StringComparison.Ordinal);
        Assert.Contains("lockRxDeviceSelection: true", codeBehind, StringComparison.Ordinal);

        int patchLoop = mainWindow.IndexOf("foreach (PatchEditRequest edit in result.PatchEdits)", StringComparison.Ordinal);
        int rename = mainWindow.IndexOf("_project.RenameDevice(currentName, result.DeviceName)", StringComparison.Ordinal);
        Assert.True(patchLoop >= 0 && rename > patchLoop, "Les patchs du détail machine doivent être appliqués avant les renommages.");
    }

    private static int CountOccurrences(string value, string expected)
    {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(expected, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += expected.Length;
        }

        return count;
    }

    private static string RepositoryFile(params string[] relativeParts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DanteConfigEditorV3.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory!.FullName, .. relativeParts]);
    }
}
