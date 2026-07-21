using System.IO;

namespace DanteConfigEditor.Services;

public static class ApplicationStoragePaths
{
    // La V3.2 utilise son propre espace afin de ne pas reprendre par erreur une
    // récupération créée par une ancienne version installée sur la machine.
    public const string RootFolderName = "DanteConfigEditorV3.2";

    public static string RootPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        RootFolderName);

    public static string Resolve(params string[] relativeParts)
    {
        return Path.Combine([RootPath, .. relativeParts]);
    }
}
