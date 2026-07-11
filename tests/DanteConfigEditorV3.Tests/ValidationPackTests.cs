using System.Security.Cryptography;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class ValidationPackTests
{
    [Fact]
    public void PackCreatesIndependentScenariosWithoutChangingSource()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        string originalHash = ComputeSha256(source);
        byte[] originalBytes = File.ReadAllBytes(source);
        string output = directory.PathFor("validation-pack");

        ValidationPackResult result = ValidationPackService.Create(source, output);

        Assert.Equal(originalHash, result.SourceSha256);
        Assert.Equal(originalBytes, File.ReadAllBytes(source));
        Assert.Equal(7, result.Scenarios.Count);
        Assert.All(result.Scenarios, scenario => Assert.Equal("CREATED", scenario.Status));
        Assert.True(File.Exists(Path.Combine(output, "00_original_copy.xml")));
        Assert.True(File.Exists(Path.Combine(output, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(output, "SHA256SUMS.txt")));
        Assert.True(File.Exists(Path.Combine(output, "DANTE_CONTROLLER_CHECKLIST.md")));
        Assert.True(File.Exists(Path.Combine(output, "reports", "before-after.txt")));
        Assert.False(Directory.Exists(Path.Combine(output, "DanteConfigEditor_Backups")));

        foreach (ValidationPackScenario scenario in result.Scenarios)
        {
            string scenarioPath = Path.Combine(output, scenario.FileName);
            Assert.True(File.Exists(scenarioPath));
            DanteProject generated = DanteProject.Load(scenarioPath);
            Assert.False(generated.ValidateXmlChangeGuard().HasErrors);
        }

        DanteProject original = DanteProject.Load(source);
        DanteProject unchanged = DanteProject.Load(Path.Combine(output, "01_saved_without_change.xml"));
        Assert.Contains("Aucune différence détectée", unchanged.CompareWith(original), StringComparison.Ordinal);

        string hashList = File.ReadAllText(Path.Combine(output, "SHA256SUMS.txt"));
        Assert.Contains("00_original_copy.xml", hashList, StringComparison.Ordinal);
        Assert.DoesNotContain("SHA256SUMS.txt", hashList, StringComparison.Ordinal);
    }

    [Fact]
    public void PackRefusesNonEmptyOutputDirectoryWithoutChangingSource()
    {
        using TestDirectory directory = new();
        string source = directory.CopyFixture("representative-preset.xml");
        byte[] originalBytes = File.ReadAllBytes(source);
        string output = directory.PathFor("existing-output");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "keep.txt"), "keep");

        IOException exception = Assert.Throws<IOException>(() => ValidationPackService.Create(source, output));

        Assert.Contains("doit être vide", exception.Message, StringComparison.Ordinal);
        Assert.Equal(originalBytes, File.ReadAllBytes(source));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(output, "keep.txt")));
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.ValidationPackTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string PathFor(string name) => Path.Combine(Root, name);

        public string CopyFixture(string name)
        {
            string destination = PathFor(name);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), destination, true);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
