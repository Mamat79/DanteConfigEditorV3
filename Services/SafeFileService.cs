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
        // Le backup est créé avant toute écriture du fichier final.
        if (!File.Exists(originalPath))
        {
            throw new FileNotFoundException("Le fichier original est introuvable, sauvegarde de sécurité impossible.", originalPath);
        }

        string originalDirectory = Path.GetDirectoryName(originalPath) ?? Environment.CurrentDirectory;
        string backupDirectory = Path.Combine(originalDirectory, "DanteConfigEditor_Backups");
        Directory.CreateDirectory(backupDirectory);

        string name = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);
        string backupPath = Path.Combine(backupDirectory, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}_original{extension}");
        File.Copy(originalPath, backupPath, overwrite: false);
        return backupPath;
    }
}
