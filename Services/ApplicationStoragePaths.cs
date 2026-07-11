using System.IO;

namespace DanteConfigEditor.Services;

public static class ApplicationStoragePaths
{
    // La V3.08 utilise son propre espace afin de ne pas mélanger une récupération
    // de la bêta avec la V3.07 stable installée sur la même machine.
    public const string RootFolderName = "DanteConfigEditorV3.08";

    public static string RootPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        RootFolderName);

    public static string Resolve(params string[] relativeParts)
    {
        return Path.Combine([RootPath, .. relativeParts]);
    }
}
