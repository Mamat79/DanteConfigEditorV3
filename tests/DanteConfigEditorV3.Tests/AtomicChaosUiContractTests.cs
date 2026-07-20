namespace DanteConfigEditorV3.Tests;

public sealed class AtomicChaosUiContractTests
{
    [Fact]
    public void AtomicButtonRequiresThreeExplicitConfirmationsOnWindowsAndMac()
    {
        string windowsXaml = File.ReadAllText(RepositoryFile("MainWindow.xaml"));
        string windowsCode = File.ReadAllText(RepositoryFile("MainWindow.xaml.cs"));
        string macXaml = File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml"));
        string macCode = File.ReadAllText(RepositoryFile("src", "DanteConfigEditor.Mac", "MainWindow.axaml.cs"));

        Assert.Contains("x:Name=\"AtomicChaosButton\"", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AtomicButtonStyle}\"", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AtomicChaosButton_Click\"", windowsXaml, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStage(\"Dialog.AtomicChaosFirst\")", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStage(\"Dialog.AtomicChaosSecond\")", windowsCode, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStage(\"Dialog.AtomicChaosThird\")", windowsCode, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"AtomicChaosButton\"", macXaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"atomic\"", macXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AtomicChaosButton_Click\"", macXaml, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStageAsync(\"Dialog.AtomicChaosFirst\")", macCode, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStageAsync(\"Dialog.AtomicChaosSecond\")", macCode, StringComparison.Ordinal);
        Assert.Contains("ConfirmAtomicChaosStageAsync(\"Dialog.AtomicChaosThird\")", macCode, StringComparison.Ordinal);
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
