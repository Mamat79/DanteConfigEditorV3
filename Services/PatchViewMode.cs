namespace DanteConfigEditor.Services;

public static class PatchViewMode
{
    public const string SimpleKey = "PatchView.Simple";
    public const string ExpertKey = "PatchView.Expert";

    public static bool IsExpert(string? selectedKey)
    {
        return string.Equals(selectedKey, ExpertKey, StringComparison.Ordinal);
    }
}
