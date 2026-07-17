using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class InstallerContractTests
{
    [Fact]
    public void InstallerKeepsV308UpgradeIdentitySeparateFromStableV307()
    {
        string script = File.ReadAllText(RepositoryFile("installer", "DanteConfigEditorV3.iss"));

        Assert.Contains("AppId={{23FF6543-561B-4C55-B733-817C9F92F5AA}", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AppId={{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}", script, StringComparison.Ordinal);
        Assert.Contains("DefaultDirName={autopf}\\Dante Config Editor V3.08", script, StringComparison.Ordinal);
        Assert.Contains("DefaultGroupName=Dante Config Editor V3.08", script, StringComparison.Ordinal);
        Assert.Contains("OutputBaseFilename=DanteConfigEditorV3_08_Installer", script, StringComparison.Ordinal);
        Assert.DoesNotContain("V3.08 Beta", script, StringComparison.Ordinal);
        Assert.Contains("UsePreviousAppDir=yes", script, StringComparison.Ordinal);
        Assert.Contains("DetectExistingInstall", script, StringComparison.Ordinal);
        Assert.Contains("remplacer / mettre à jour", script, StringComparison.Ordinal);
        Assert.Contains("replace/update", script, StringComparison.Ordinal);
        Assert.Contains("HKLM", script, StringComparison.Ordinal);
        Assert.Contains("HKCU", script, StringComparison.Ordinal);
    }

    [Fact]
    public void InstallerPayloadIsSelfContainedAndIncludesBilingualDocumentation()
    {
        string buildScript = File.ReadAllText(RepositoryFile("installer", "build_installer.ps1"));
        string installerScript = File.ReadAllText(RepositoryFile("installer", "DanteConfigEditorV3.iss"));

        Assert.Contains("--self-contained true", buildScript, StringComparison.Ordinal);
        Assert.Contains("-p:PublishSingleFile=true", buildScript, StringComparison.Ordinal);
        Assert.Contains("if ($LASTEXITCODE -ne 0)", buildScript, StringComparison.Ordinal);
        Assert.Contains("QuickStart_DanteConfigEditorV3_FR.pdf", installerScript, StringComparison.Ordinal);
        Assert.Contains("QuickStart_DanteConfigEditorV3_EN.pdf", installerScript, StringComparison.Ordinal);
        Assert.Contains("Notice_DanteConfigEditorV3_FR.pdf", installerScript, StringComparison.Ordinal);
        Assert.Contains("Notice_DanteConfigEditorV3_EN.pdf", installerScript, StringComparison.Ordinal);
        Assert.Contains("https://github.com/Mamat79/DanteConfigEditorV3", installerScript, StringComparison.Ordinal);
        Assert.Contains("SignatureLabel.Caption := 'By Mamat'", installerScript, StringComparison.Ordinal);
        Assert.Contains("SignatureAgentsLabel.Caption := 'et ses agents'", installerScript, StringComparison.Ordinal);
        Assert.Contains("By Mamat", File.ReadAllText(RepositoryFile("MainWindow.xaml")), StringComparison.Ordinal);
        Assert.Contains("et ses agents", File.ReadAllText(RepositoryFile("MainWindow.xaml")), StringComparison.Ordinal);
        Assert.Contains("By Mamat", File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml")), StringComparison.Ordinal);
        Assert.Contains("et ses agents", File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("Services", "ReportExportService.cs")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("packaging", "macos", "Info.plist")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("docs", "generate_guides.py")), StringComparison.Ordinal);
        Assert.Contains("README_EN.md", installerScript, StringComparison.Ordinal);
        Assert.Contains("RELEASE_NOTES_EN.md", installerScript, StringComparison.Ordinal);
        Assert.Contains("Assert-RepositoryPath", buildScript, StringComparison.Ordinal);
        Assert.Contains("Remove-GeneratedPath", buildScript, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash", buildScript, StringComparison.Ordinal);
        Assert.Contains("DanteConfigEditorV3_08_Installer.exe", buildScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TestScriptsCoverBothSuitesAndARealUpgradePass()
    {
        string testScript = File.ReadAllText(RepositoryFile("tests", "run-tests.ps1"));
        string upgradeScript = File.ReadAllText(RepositoryFile("tests", "Test-InstallerUpgrade.ps1"));

        Assert.Contains("DanteConfigEditorV3.Tests.csproj", testScript, StringComparison.Ordinal);
        Assert.Contains("DanteConfigEditor.Mac.Tests.csproj", testScript, StringComparison.Ordinal);
        Assert.Contains("--no-restore", testScript, StringComparison.Ordinal);

        Assert.Contains("/VERYSILENT", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-InstallerPass", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("Mise à niveau de contrôle", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("StableInstallRecords", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("TargetInstallRecords", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("V3InstallRecords", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("23FF6543-561B-4C55-B733-817C9F92F5AA", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1", upgradeScript, StringComparison.Ordinal);
    }

    [Fact]
    public void V308UsesSeparateLocalApplicationDataFolder()
    {
        Assert.Equal("DanteConfigEditorV3.08", ApplicationStoragePaths.RootFolderName);
        Assert.DoesNotContain("DanteConfigEditorV3\\Recovery", ApplicationStoragePaths.Resolve("Recovery"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OfficialV308IncludesMacPackagingMetadata()
    {
        string project = File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "DanteConfigEditor.Mac.csproj"));
        string plist = File.ReadAllText(RepositoryFile("packaging", "macos", "Info.plist"));
        string packaging = File.ReadAllText(RepositoryFile("packaging", "macos", "build-macos.sh"));
        string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "macos-ci.yml"));

        Assert.Contains("<InformationalVersion>3.08</InformationalVersion>", project, StringComparison.Ordinal);
        Assert.Contains("<string>Dante Config Editor V3.08</string>", plist, StringComparison.Ordinal);
        Assert.Contains("<string>3.8.0</string>", plist, StringComparison.Ordinal);
        Assert.Contains("Dante Config Editor V3.08", packaging, StringComparison.Ordinal);
        Assert.Contains("shasum -a 256 \"$DMG_NAME\"", packaging, StringComparison.Ordinal);
        Assert.Contains("branches:", workflow, StringComparison.Ordinal);
        Assert.Contains("- main", workflow, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
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
