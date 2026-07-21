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
        Assert.Contains("ChannelLabelsTab", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"Labels\"", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("Importer des labels", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("Exporter des labels", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("dLive / Avantis", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelImportWindow", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelExportWindow", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ImportChannelLabelsButton", macXaml, StringComparison.Ordinal);
        Assert.Contains("ExportChannelLabelsButton", macXaml, StringComparison.Ordinal);
        Assert.Contains("ChannelLabelsTab", macXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"Labels\"", macXaml, StringComparison.Ordinal);
        Assert.Contains("Importer des labels", macXaml, StringComparison.Ordinal);
        Assert.Contains("Exporter des labels", macXaml, StringComparison.Ordinal);
        Assert.Contains("dLive / Avantis", macXaml, StringComparison.Ordinal);
        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", macCode, StringComparison.Ordinal);
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
        Assert.Contains("CSV générique - nouveau fichier", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Tag=\"dmt-dlive\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Tag=\"dmt-ods-dlive\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Tag=\"dmt-ods-avantis\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Tag=\"ah-avantis\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Tag=\"yamaha-ql\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding IsAvailable}\"", windowsExport, StringComparison.Ordinal);
        Assert.Contains("Height=\"260\"", windowsImport, StringComparison.Ordinal);
        Assert.Contains("TargetDeviceCheckBox_Click", windowsImport, StringComparison.Ordinal);
        Assert.Contains("PreviewGrid", macImport, StringComparison.Ordinal);
        Assert.Contains("AdaptDmtCheckBox", macExport, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding IsAvailable}\"", macExport, StringComparison.Ordinal);
        Assert.Contains("RowDefinitions=\"Auto,286,*,Auto\"", macImport, StringComparison.Ordinal);
        Assert.Contains("TargetDeviceCheckBox_Click", macImport, StringComparison.Ordinal);
        string windowsMain = Read("MainWindow.xaml.cs");
        string macMain = Read("src", "DanteConfigEditor.Mac", "MainWindow.axaml.cs");
        string macExportCode = Read("src", "DanteConfigEditor.Mac", "ChannelLabelExportDialog.axaml.cs");
        Assert.Contains("dmt-dlive", macExportCode, StringComparison.Ordinal);
        Assert.Contains("dmt-ods-dlive", macExportCode, StringComparison.Ordinal);
        Assert.Contains("yamaha-ql", macExportCode, StringComparison.Ordinal);
        Assert.Contains("BuiltInChannelLabelTemplateService.Write", windowsMain, StringComparison.Ordinal);
        Assert.Contains("BuiltInChannelLabelTemplateService.Write", macMain, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose the original DMT template", windowsMain, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose the original DMT template", macMain, StringComparison.Ordinal);
        string windowsExportCode = Read("ChannelLabelExportWindow.xaml.cs");
        Assert.Contains("SelectedIndex = 1", windowsExportCode, StringComparison.Ordinal);
        Assert.Contains("private bool _initializing = true", windowsExportCode, StringComparison.Ordinal);
        Assert.Contains("_initializing = false", windowsExportCode, StringComparison.Ordinal);
        Assert.Contains("private bool _initializing = true", macExportCode, StringComparison.Ordinal);
        Assert.Contains("_initializing = false", macExportCode, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoMatchCheckBox.IsEnabled = document.Sets.Count > 1", Read("ChannelLabelImportWindow.xaml.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentationCreditsDmtAndReleaseWorkflowNeverOverwritesHistory()
    {
        string readmeFr = Read("README.md");
        string readmeEn = Read("README_EN.md");
        string workflow = Read(".github", "workflows", "versioned-release.yml");

        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", readmeFr, StringComparison.Ordinal);
        Assert.Contains("https://github.com/togrupe/dlive-midi-tools", readmeEn, StringComparison.Ordinal);
        Assert.Contains("Import et export de labels", readmeFr, StringComparison.Ordinal);
        Assert.Contains("dLive et Avantis", readmeFr, StringComparison.Ordinal);
        Assert.Contains("Importing and exporting labels", readmeEn, StringComparison.Ordinal);
        Assert.Contains("dLive and Avantis", readmeEn, StringComparison.Ordinal);
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
