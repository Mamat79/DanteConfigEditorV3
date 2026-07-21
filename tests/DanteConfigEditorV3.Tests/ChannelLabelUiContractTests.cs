namespace DanteConfigEditorV3.Tests;

public sealed class ChannelLabelUiContractTests
{
    [Fact]
    public void WindowsAndMacExposePreviewedChannelLabelExchange()
    {
        string windowsXaml = Read("MainWindow.xaml");
        string windowsCode = Read("MainWindow.xaml.cs");
        string macXaml = Read("src", "DanteConfigEditor.Mac", "MainWindow.axaml");
        string macCode = Read("src", "DanteConfigEditor.Mac", "MainWindow.axaml.cs");

        Assert.Contains("ImportChannelLabelsButton", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("ExportChannelLabelsButton", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelImportWindow", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelExportWindow", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ImportChannelLabelsButton", macXaml, StringComparison.Ordinal);
        Assert.Contains("ExportChannelLabelsButton", macXaml, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelImportDialog", macCode, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelExportDialog", macCode, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportAndExportRequireExplicitPreviewAndDmtAdaptation()
    {
        string windowsImport = Read("ChannelLabelImportWindow.xaml");
        string windowsExport = Read("ChannelLabelExportWindow.xaml");
        string macImport = Read("src", "DanteConfigEditor.Mac", "ChannelLabelImportDialog.axaml");
        string macExport = Read("src", "DanteConfigEditor.Mac", "ChannelLabelExportDialog.axaml");

        Assert.Contains("PreviewGrid", windowsImport, StringComparison.Ordinal);
        Assert.Contains("AdaptDmtCheckBox", windowsExport, StringComparison.Ordinal);
        Assert.Contains("PreviewGrid", macImport, StringComparison.Ordinal);
        Assert.Contains("AdaptDmtCheckBox", macExport, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentationCreditsDmtAndReleaseWorkflowNeverOverwritesHistory()
    {
        string readmeFr = Read("README.md");
        string readmeEn = Read("README_EN.md");
        string workflow = Read(".github", "workflows", "versioned-release.yml");

        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", readmeFr, StringComparison.Ordinal);
        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", readmeEn, StringComparison.Ordinal);
        Assert.Contains("Refusing to overwrite it", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("gh release delete", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--clobber", workflow, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(params string[] parts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DanteConfigEditorV3.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory!.FullName, .. parts]));
    }
}
