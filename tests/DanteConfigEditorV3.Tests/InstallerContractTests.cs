namespace DanteConfigEditorV3.Tests;

public sealed class InstallerContractTests
{
    [Fact]
    public void InstallerKeepsStableUpgradeIdentityAndPreviousDestination()
    {
        string script = File.ReadAllText(RepositoryFile("installer", "DanteConfigEditorV3.iss"));

        Assert.Contains("AppId={{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}", script, StringComparison.Ordinal);
        Assert.Contains("DefaultDirName={autopf}\\Dante Config Editor V3", script, StringComparison.Ordinal);
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
        Assert.Contains("By Mamat", installerScript, StringComparison.Ordinal);
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
