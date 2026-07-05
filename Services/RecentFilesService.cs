using System.IO;

namespace DanteConfigEditor.Services;

public static class RecentFilesService
{
    private const int MaxRecentFiles = 8;

    // Stockage volontairement simple : un fichier texte dans le profil Windows
    // de l'utilisateur, pas dans le dossier d'installation.
    private static readonly string RecentFilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DanteConfigEditorV3",
        "recent-files.txt");

    public static IReadOnlyList<string> Load()
    {
        if (!File.Exists(RecentFilesPath))
        {
            return [];
        }

        return File.ReadAllLines(RecentFilesPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxRecentFiles)
            .ToArray();
    }

    public static void Add(string path)
    {
        // Le dernier fichier ouvert remonte en haut de la liste, sans doublon.
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        List<string> recentFiles = Load()
            .Where(existing => !string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
            .ToList();

        recentFiles.Insert(0, path);
        Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
        File.WriteAllLines(RecentFilesPath, recentFiles.Take(MaxRecentFiles));
    }
}
