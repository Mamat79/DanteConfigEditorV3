using System.Reflection;
using System.Text.RegularExpressions;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class LocalizationConsistencyTests
{
    [Fact]
    public void FrenchAndEnglishTranslationsStaySynchronized()
    {
        IReadOnlyDictionary<string, string> french = Dictionary("French");
        IReadOnlyDictionary<string, string> english = Dictionary("English");

        Assert.Equal(french.Keys.Order(), english.Keys.Order());
        foreach (string key in french.Keys)
        {
            Assert.Equal(Placeholders(french[key]), Placeholders(english[key]));
        }

        Assert.Equal("HORRIBLE EXPERIENCE GENERATOR (BUT EDUCATIONAL)",
            LocalizationService.TranslateLiteral(UiLanguage.English, "GÉNÉRATEUR D'EXPÉRIENCE HORRIBLE (MAIS PÉDAGOGIQUE)"));
        Assert.Equal("JSON/CSV labels, DMT XLSX/ODS, A&H dLive/Avantis, and Yamaha CL/QL.",
            LocalizationService.TranslateLiteral(UiLanguage.English, "Labels JSON/CSV, DMT XLSX/ODS, A&H dLive/Avantis et Yamaha CL/QL."));
        Assert.Equal("Export a generic file or create a copy of a DMT, A&H, or Yamaha template.",
            LocalizationService.TranslateLiteral(UiLanguage.English, "Exportez en générique ou créez une copie d'un modèle DMT, A&H ou Yamaha."));
    }

    private static IReadOnlyDictionary<string, string> Dictionary(string fieldName)
    {
        FieldInfo? field = typeof(LocalizationService).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        return Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(field?.GetValue(null));
    }

    private static string[] Placeholders(string value) =>
        Regex.Matches(value, @"\{\d+(?::[^}]*)?\}").Select(match => match.Value).ToArray();
}
