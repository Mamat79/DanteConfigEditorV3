using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public static class SessionRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(DanteProject project, string? recoveryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        string directory = ResolveRecoveryDirectory(recoveryDirectory);
        Directory.CreateDirectory(directory);
        string identifier = BuildIdentifier(project.OriginalFilePath);
        string xmlPath = Path.Combine(directory, identifier + ".xml");
        string metadataPath = Path.Combine(directory, identifier + ".json");
        string xmlTemporaryPath = xmlPath + ".tmp";
        string metadataTemporaryPath = metadataPath + ".tmp";
        FileInfo source = new(project.OriginalFilePath);

        try
        {
            project.Document.Save(xmlTemporaryPath, SaveOptions.DisableFormatting);
            XDocument verification = XDocument.Load(xmlTemporaryPath, LoadOptions.PreserveWhitespace);
            if (verification.Root is null || !verification.Root.Children("device").Any())
            {
                throw new InvalidOperationException("La copie de récupération XML n'est pas exploitable.");
            }

            File.Move(xmlTemporaryPath, xmlPath, true);
            RecoveryMetadata metadata = new(
                Path.GetFullPath(project.OriginalFilePath),
                source.Exists ? source.LastWriteTimeUtc : DateTime.MinValue,
                source.Exists ? source.Length : 0,
                DateTime.UtcNow,
                Path.GetFileName(xmlPath));
            File.WriteAllText(metadataTemporaryPath, JsonSerializer.Serialize(metadata, JsonOptions), new UTF8Encoding(false));
            File.Move(metadataTemporaryPath, metadataPath, true);
        }
        finally
        {
            DeleteIfExists(xmlTemporaryPath);
            DeleteIfExists(metadataTemporaryPath);
        }
    }

    public static RecoveryCandidate? Find(string originalFilePath, string? recoveryDirectory = null)
    {
        string directory = ResolveRecoveryDirectory(recoveryDirectory);
        string identifier = BuildIdentifier(originalFilePath);
        string metadataPath = Path.Combine(directory, identifier + ".json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            RecoveryMetadata? metadata = JsonSerializer.Deserialize<RecoveryMetadata>(File.ReadAllText(metadataPath));
            if (metadata is null
                || !string.Equals(Path.GetFullPath(metadata.OriginalFilePath), Path.GetFullPath(originalFilePath), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string xmlPath = Path.Combine(directory, metadata.XmlFileName);
            if (!File.Exists(xmlPath))
            {
                Delete(originalFilePath, directory);
                return null;
            }

            FileInfo source = new(originalFilePath);
            bool sourceMatches = source.Exists
                && source.LastWriteTimeUtc == metadata.SourceLastWriteTimeUtc
                && source.Length == metadata.SourceLength;
            return new RecoveryCandidate(xmlPath, metadata.SavedAtUtc, sourceMatches);
        }
        catch
        {
            Delete(originalFilePath, directory);
            return null;
        }
    }

    public static void Delete(string originalFilePath, string? recoveryDirectory = null)
    {
        string directory = ResolveRecoveryDirectory(recoveryDirectory);
        string identifier = BuildIdentifier(originalFilePath);
        DeleteIfExists(Path.Combine(directory, identifier + ".xml"));
        DeleteIfExists(Path.Combine(directory, identifier + ".json"));
    }

    public static void CleanupOld(TimeSpan maximumAge, string? recoveryDirectory = null)
    {
        string directory = ResolveRecoveryDirectory(recoveryDirectory);
        if (!Directory.Exists(directory))
        {
            return;
        }

        DateTime limit = DateTime.UtcNow - maximumAge;
        foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles())
        {
            if (file.LastWriteTimeUtc < limit && file.Extension is ".xml" or ".json" or ".tmp")
            {
                file.Delete();
            }
        }
    }

    private static string ResolveRecoveryDirectory(string? recoveryDirectory)
    {
        return string.IsNullOrWhiteSpace(recoveryDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DanteConfigEditorV3", "Recovery")
            : Path.GetFullPath(recoveryDirectory);
    }

    private static string BuildIdentifier(string path)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed record RecoveryMetadata(
        string OriginalFilePath,
        DateTime SourceLastWriteTimeUtc,
        long SourceLength,
        DateTime SavedAtUtc,
        string XmlFileName);
}

public sealed record RecoveryCandidate(
    string RecoveryXmlPath,
    DateTime SavedAtUtc,
    bool SourceMatches);
