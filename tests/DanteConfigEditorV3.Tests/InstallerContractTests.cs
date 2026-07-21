using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class InstallerContractTests
{
    [Fact]
    public void InstallerV33ReplacesV32AndOlderInstalledVersions()
    {
        string script = File.ReadAllText(RepositoryFile("installer", "DanteConfigEditorV3.iss"));

        Assert.Contains("AppId={{76E68F80-5C89-4415-A090-370CA60EB3AD}", script, StringComparison.Ordinal);
        Assert.Contains("A2CC4547-2811-4EB5-B0BC-FBE4B7B847DF", script, StringComparison.Ordinal);
        Assert.Contains("C72399DF-AC3B-4FFA-A503-D79A4D6D9380", script, StringComparison.Ordinal);
        Assert.Contains("23FF6543-561B-4C55-B733-817C9F92F5AA", script, StringComparison.Ordinal);
        Assert.Contains("D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1", script, StringComparison.Ordinal);
        Assert.Contains("DefaultDirName={autopf}\\Dante Config Editor V3.3", script, StringComparison.Ordinal);
        Assert.Contains("DefaultGroupName=Dante Config Editor V3.3", script, StringComparison.Ordinal);
        Assert.Contains("OutputBaseFilename=DanteConfigEditorV3_3_Installer", script, StringComparison.Ordinal);
        Assert.Contains("UsePreviousAppDir=no", script, StringComparison.Ordinal);
        Assert.Contains("DetectExistingInstall", script, StringComparison.Ordinal);
        Assert.Contains("RunLegacyUninstaller", script, StringComparison.Ordinal);
        Assert.Contains("PrepareToInstall", script, StringComparison.Ordinal);
        Assert.Contains("ExistingInstallVersion <> '{#MyAppVersion}'", script, StringComparison.Ordinal);
        Assert.Contains("remplacer / mettre à jour", script, StringComparison.Ordinal);
        Assert.Contains("replace/update", script, StringComparison.Ordinal);
        Assert.Contains("HKLM", script, StringComparison.Ordinal);
        Assert.Contains("HKCU", script, StringComparison.Ordinal);
        Assert.Contains("DeleteLegacyShortcuts", script, StringComparison.Ordinal);
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
        Assert.Contains("DMT_LICENSE.txt", installerScript, StringComparison.Ordinal);
        Assert.Contains("https://github.com/Mamat79/DanteConfigEditorV3", installerScript, StringComparison.Ordinal);
        Assert.Contains("SignatureLabel.Caption := 'By Mamat'", installerScript, StringComparison.Ordinal);
        Assert.Contains("SignatureAgentsLabel.Caption := 'et ses agents'", installerScript, StringComparison.Ordinal);
        string windowsWindow = File.ReadAllText(RepositoryFile("MainWindow.xaml"));
        Assert.Contains("MinHeight=\"34\"", windowsWindow, StringComparison.Ordinal);
        Assert.Contains("<TextBlock Text=\"By Mamat\"", windowsWindow, StringComparison.Ordinal);
        Assert.Contains("<TextBlock Text=\"et ses agents\"", windowsWindow, StringComparison.Ordinal);
        Assert.Contains("By Mamat", File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml")), StringComparison.Ordinal);
        Assert.Contains("et ses agents", File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("Services", "ReportExportService.cs")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("packaging", "macos", "Info.plist")), StringComparison.Ordinal);
        Assert.Contains("By Mamat et ses agents", File.ReadAllText(RepositoryFile("docs", "generate_guides.py")), StringComparison.Ordinal);
        Assert.Contains("README_EN.md", installerScript, StringComparison.Ordinal);
        Assert.Contains("RELEASE_NOTES_EN.md", installerScript, StringComparison.Ordinal);
        Assert.Contains("Name: \"desktopicon\"", installerScript, StringComparison.Ordinal);
        Assert.DoesNotContain("Name: \"desktopicon\"; Description: \"{cm:CreateDesktopIcon}\"; GroupDescription: \"{cm:AdditionalIcons}\"; Flags: unchecked", installerScript, StringComparison.Ordinal);
        Assert.Contains("{autodesktop}\\{code:GetShortcutAppName}", installerScript, StringComparison.Ordinal);
        Assert.Contains("Assert-RepositoryPath", buildScript, StringComparison.Ordinal);
        Assert.Contains("Remove-GeneratedPath", buildScript, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash", buildScript, StringComparison.Ordinal);
        Assert.Contains("DanteConfigEditorV3_3_Installer.exe", buildScript, StringComparison.Ordinal);
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
        Assert.Contains("TargetInstallRecords", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("V3InstallRecords", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("76E68F80-5C89-4415-A090-370CA60EB3AD", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("A2CC4547-2811-4EB5-B0BC-FBE4B7B847DF", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("CommonDesktopDirectory", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("raccourci Bureau manquant", upgradeScript, StringComparison.Ordinal);
        Assert.Contains("une seule installation V3", upgradeScript, StringComparison.Ordinal);
    }

    [Fact]
    public void V33KeepsTheV32LocalApplicationDataFolderForUpgradeContinuity()
    {
        Assert.Equal("DanteConfigEditorV3.2", ApplicationStoragePaths.RootFolderName);
        Assert.DoesNotContain("DanteConfigEditorV3\\Recovery", ApplicationStoragePaths.Resolve("Recovery"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void V33IncludesMacPackagingMetadata()
    {
        string project = File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "DanteConfigEditor.Mac.csproj"));
        string plist = File.ReadAllText(RepositoryFile("packaging", "macos", "Info.plist"));
        string packaging = File.ReadAllText(RepositoryFile("packaging", "macos", "build-macos.sh"));
        string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "macos-ci.yml"));

        Assert.Contains("<InformationalVersion>3.3</InformationalVersion>", project, StringComparison.Ordinal);
        Assert.Contains("<string>Dante Config Editor V3.3</string>", plist, StringComparison.Ordinal);
        Assert.Contains("<string>3.3.0</string>", plist, StringComparison.Ordinal);
        Assert.Contains("Dante Config Editor V3.3", packaging, StringComparison.Ordinal);
        Assert.Contains("shasum -a 256 \"$DMG_NAME\"", packaging, StringComparison.Ordinal);
        Assert.Contains("branches:", workflow, StringComparison.Ordinal);
        Assert.Contains("- main", workflow, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionedReleaseWorkflowPreservesPublishedHistory()
    {
        string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "versioned-release.yml"));

        Assert.Contains("refs/tags/${{ needs.prepare.outputs.tag }}", workflow, StringComparison.Ordinal);
        Assert.Contains("Release $tag already exists. Refusing to overwrite it.", workflow, StringComparison.Ordinal);
        Assert.Contains("gh release create", workflow, StringComparison.Ordinal);
        Assert.Contains("--verify-tag", workflow, StringComparison.Ordinal);
        Assert.Contains("make_latest", workflow, StringComparison.Ordinal);
        Assert.Contains("find docs -maxdepth 1 -type f -name '*.pdf'", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("docs/media", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("*.mp4", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gh release delete", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--clobber", workflow, StringComparison.OrdinalIgnoreCase);
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
