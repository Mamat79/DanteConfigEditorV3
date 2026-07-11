using System.IO;

namespace DanteConfigEditor.Services;

public static class LanguageSettingsService
{
    private static readonly string SettingsPath = ApplicationStoragePaths.Resolve("language.txt");

    public static UiLanguage Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return UiLanguage.French;
        }

        string value = File.ReadAllText(SettingsPath).Trim();
        return string.Equals(value, "en", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.English
            : UiLanguage.French;
    }

    public static void Save(UiLanguage language)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, language == UiLanguage.English ? "en" : "fr");
    }
}
