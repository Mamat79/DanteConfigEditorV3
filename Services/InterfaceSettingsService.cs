using System.IO;

namespace DanteConfigEditor.Services;

public static class InterfaceSettingsService
{
    private static readonly string ConfigurationEditorsPath = ApplicationStoragePaths.Resolve("configuration-editors.txt");

    public static bool LoadConfigurationEditorsExpanded(string? settingsPath = null)
    {
        string path = settingsPath ?? ConfigurationEditorsPath;
        if (!File.Exists(path))
        {
            return true;
        }

        return !string.Equals(
            File.ReadAllText(path).Trim(),
            "collapsed",
            StringComparison.OrdinalIgnoreCase);
    }

    public static void SaveConfigurationEditorsExpanded(bool expanded, string? settingsPath = null)
    {
        string path = settingsPath ?? ConfigurationEditorsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, expanded ? "expanded" : "collapsed");
    }
}
