using System.IO;

namespace DanteConfigEditor.Services;

public static class ApplicationStoragePaths
{
    // La V3.5 réutilise l'espace V3.2 afin de préserver les préférences et les
    // récupérations lors de la mise à niveau demandée par l'utilisateur.
    public const string RootFolderName = "DanteConfigEditorV3.2";

    public static string RootPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        RootFolderName);

    public static string Resolve(params string[] relativeParts)
    {
        return Path.Combine([RootPath, .. relativeParts]);
    }
}
