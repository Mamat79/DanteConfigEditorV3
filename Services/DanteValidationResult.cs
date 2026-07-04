namespace DanteConfigEditor.Services;

public sealed class DanteValidationResult
{
    public List<string> Errors { get; } = [];

    public List<string> Warnings { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public string ToDisplayText()
    {
        List<string> lines = [];

        if (Errors.Count == 0 && Warnings.Count == 0)
        {
            return "Aucune anomalie bloquante détectée.";
        }

        if (Errors.Count > 0)
        {
            lines.Add("Erreurs bloquantes :");
            lines.AddRange(Errors.Select(error => "- " + error));
        }

        if (Warnings.Count > 0)
        {
            if (lines.Count > 0)
            {
                lines.Add(string.Empty);
            }

            lines.Add("Points à vérifier :");
            lines.AddRange(Warnings.Select(warning => "- " + warning));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
