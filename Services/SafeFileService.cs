using System.IO;

namespace DanteConfigEditor.Services;

public static class SafeFileService
{
    public static string BuildDefaultSavePath(string originalPath)
    {
        // Par défaut, on propose un nouveau fichier à côté de l'original.
        // Cela évite d'écraser involontairement le XML source.
        string directory = Path.GetDirectoryName(originalPath) ?? Environment.CurrentDirectory;
        string name = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);
        string candidate = Path.Combine(directory, $"{name}_V3{extension}");

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        return Path.Combine(directory, $"{name}_V3_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
    }

    public static string CreateOriginalBackup(string originalPath)
    {
        return CreateBackupCopy(originalPath, "original");
    }

    public static string BuildDestinationBackupPath(string destinationPath)
    {
        // File.Replace créera cette copie dans le même volume que la destination,
        // ce qui permet de conserver un remplacement réellement atomique.
        return BuildUniqueBackupPath(destinationPath, "destination-remplacee");
    }

    private static string CreateBackupCopy(string sourcePath, string label)
    {
        // Le backup est créé avant toute écriture du fichier final.
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Le fichier source est introuvable, sauvegarde de sécurité impossible.", sourcePath);
        }

        string backupPath = BuildUniqueBackupPath(sourcePath, label);
        File.Copy(sourcePath, backupPath, overwrite: false);
        return backupPath;
    }

    private static string BuildUniqueBackupPath(string sourcePath, string label)
    {
        string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory;
        string backupDirectory = Path.Combine(sourceDirectory, "DanteConfigEditor_Backups");
        Directory.CreateDirectory(backupDirectory);

        string name = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        return Path.Combine(backupDirectory, $"{name}_{timestamp}_{label}_{Guid.NewGuid():N}{extension}");
    }
}
