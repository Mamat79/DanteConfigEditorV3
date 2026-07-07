using System.Xml.Linq;

namespace DanteConfigEditor.Services;

public static class DanteXmlChangeGuardService
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/preset/name",
        "/preset/device/name",
        "/preset/device/friendly_name",
        "/preset/device/preferred_master/@value",
        "/preset/device/redundancy/@value",
        "/preset/device/unicast_latency",
        "/preset/device/txchannel/label",
        "/preset/device/txchannel/name",
        "/preset/device/txchannel/channel_name",
        "/preset/device/txchannel/@label",
        "/preset/device/txchannel/@name",
        "/preset/device/txchannel/@channel_name",
        "/preset/device/rxchannel/name",
        "/preset/device/rxchannel/label",
        "/preset/device/rxchannel/channel_name",
        "/preset/device/rxchannel/@name",
        "/preset/device/rxchannel/@label",
        "/preset/device/rxchannel/@channel_name",
        "/preset/device/rxchannel/subscribed_channel",
        "/preset/device/rxchannel/subscribed_channel_name",
        "/preset/device/rxchannel/subscribed_channel_label",
        "/preset/device/rxchannel/subscribed_tx_channel",
        "/preset/device/rxchannel/subscribed_tx_channel_name",
        "/preset/device/rxchannel/subscribed_label",
        "/preset/device/rxchannel/source_channel",
        "/preset/device/rxchannel/source_channel_name",
        "/preset/device/rxchannel/subscribed_device",
        "/preset/device/rxchannel/subscription_device",
        "/preset/device/rxchannel/tx_device",
        "/preset/device/rxchannel/source_device"
    };

    private static readonly string[] ForbiddenExactPaths =
    [
        "/preset/@version",
        "/preset/device/instance_id",
        "/preset/device/instance_id/device_id",
        "/preset/device/instance_id/process_id",
        "/preset/device/manufacturer_id",
        "/preset/device/manufacturer_name",
        "/preset/device/model_id",
        "/preset/device/model_name",
        "/preset/device/model_version",
        "/preset/device/device_type",
        "/preset/device/device_type_string",
        "/preset/device/captureInfo",
        "/preset/device/interface",
        "/preset/device/clock",
        "/preset/device/clock_priority",
        "/preset/device/rtp",
        "/preset/device/txchannel",
        "/preset/device/rxchannel",
        "/preset/device/txchannel/@danteId",
        "/preset/device/rxchannel/@danteId",
        "/preset/device/txchannel/@mediaType",
        "/preset/device/rxchannel/@mediaType"
    ];

    public static DanteValidationResult ValidateChanges(XDocument originalDocument, XDocument currentDocument)
    {
        DanteValidationResult result = new();

        if (originalDocument.Root is null || currentDocument.Root is null)
        {
            result.AddError(DanteIssueCategory.SaveSafety, "Sauvegarde refusée : la racine <preset> est absente.");
            return result;
        }

        if (!string.Equals(originalDocument.Root.Name.LocalName, currentDocument.Root.Name.LocalName, StringComparison.Ordinal))
        {
            result.AddError(DanteIssueCategory.SaveSafety, $"Sauvegarde refusée : racine XML modifiée ({originalDocument.Root.Name.LocalName} -> {currentDocument.Root.Name.LocalName}).");
            return result;
        }

        CompareElements(originalDocument.Root, currentDocument.Root, "/" + originalDocument.Root.Name.LocalName, result);

        if (!result.HasErrors)
        {
            result.AddInfo(DanteIssueCategory.SaveSafety, "Compatibilité XML : aucune modification interdite détectée.");
        }

        return result;
    }

    public static string BuildGuardReport(DanteValidationResult guardResult)
    {
        List<string> lines = [];
        lines.Add("Garde-fou changements XML");
        lines.Add("------------------------");
        lines.Add(guardResult.HasErrors
            ? "ERROR Sauvegarde refusée : une modification interdite du XML Dante a été détectée."
            : "OK Compatibilité XML : aucune modification interdite détectée.");

        foreach (string warning in guardResult.Warnings.Take(80))
        {
            lines.Add("WARNING " + warning);
        }

        foreach (string error in guardResult.Errors.Take(80))
        {
            lines.Add("ERROR " + error);
        }

        if (guardResult.Warnings.Count > 80 || guardResult.Errors.Count > 80)
        {
            lines.Add("WARNING Rapport tronqué : ouvrez la page Santé du fichier pour les détails.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void CompareElements(XElement original, XElement current, string path, DanteValidationResult result)
    {
        if (!string.Equals(original.Name.LocalName, current.Name.LocalName, StringComparison.Ordinal))
        {
            AddChangeIssue(result, path, $"Balise modifiée : <{original.Name.LocalName}> -> <{current.Name.LocalName}>.");
            return;
        }

        CompareAttributes(original, current, path, result);

        XElement[] originalChildren = original.Elements().ToArray();
        XElement[] currentChildren = current.Elements().ToArray();
        if (originalChildren.Length == 0 && currentChildren.Length == 0)
        {
            string originalValue = original.Value.Trim();
            string currentValue = current.Value.Trim();
            if (!string.Equals(originalValue, currentValue, StringComparison.Ordinal))
            {
                AddChangeIssue(result, path, $"Valeur modifiée : {FormatValue(originalValue)} -> {FormatValue(currentValue)}.");
            }

            return;
        }

        int max = Math.Min(originalChildren.Length, currentChildren.Length);
        for (int index = 0; index < max; index++)
        {
            string childPath = NormalizePath(path + "/" + originalChildren[index].Name.LocalName);
            CompareElements(originalChildren[index], currentChildren[index], childPath, result);
        }

        for (int index = max; index < originalChildren.Length; index++)
        {
            AddChangeIssue(result, NormalizePath(path + "/" + originalChildren[index].Name.LocalName), $"Balise supprimée : <{originalChildren[index].Name.LocalName}>.");
        }

        for (int index = max; index < currentChildren.Length; index++)
        {
            AddChangeIssue(result, NormalizePath(path + "/" + currentChildren[index].Name.LocalName), $"Balise ajoutée : <{currentChildren[index].Name.LocalName}>.");
        }
    }

    private static void CompareAttributes(XElement original, XElement current, string path, DanteValidationResult result)
    {
        Dictionary<string, XAttribute> originalAttributes = original.Attributes().ToDictionary(attribute => attribute.Name.LocalName, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, XAttribute> currentAttributes = current.Attributes().ToDictionary(attribute => attribute.Name.LocalName, StringComparer.OrdinalIgnoreCase);

        foreach (string name in originalAttributes.Keys.Union(currentAttributes.Keys, StringComparer.OrdinalIgnoreCase))
        {
            string attributePath = NormalizePath(path + "/@" + name);
            bool hasOriginal = originalAttributes.TryGetValue(name, out XAttribute? originalAttribute);
            bool hasCurrent = currentAttributes.TryGetValue(name, out XAttribute? currentAttribute);

            if (!hasOriginal)
            {
                AddChangeIssue(result, attributePath, $"Attribut ajouté : @{name}.");
            }
            else if (!hasCurrent)
            {
                AddChangeIssue(result, attributePath, $"Attribut supprimé : @{name}.");
            }
            else if (!string.Equals(originalAttribute!.Value, currentAttribute!.Value, StringComparison.Ordinal))
            {
                AddChangeIssue(result, attributePath, $"Attribut @{name} modifié : {FormatValue(originalAttribute.Value)} -> {FormatValue(currentAttribute.Value)}.");
            }
        }
    }

    private static void AddChangeIssue(DanteValidationResult result, string rawPath, string detail)
    {
        string path = NormalizePath(rawPath);
        string message = $"{ToUserPath(path)} : {detail}";

        if (IsAllowed(path))
        {
            return;
        }

        if (IsForbidden(path))
        {
            result.AddError(DanteIssueCategory.SaveSafety, message);
        }
        else
        {
            result.AddWarning(DanteIssueCategory.SaveSafety, "Modification XML suspecte non bloquante : " + message);
        }
    }

    private static bool IsAllowed(string path)
    {
        return AllowedPaths.Contains(path);
    }

    private static bool IsForbidden(string path)
    {
        return ForbiddenExactPaths.Any(forbidden => string.Equals(path, forbidden, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(forbidden + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
    {
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part is "txchannel" or "rxchannel" or "device" ? part : part)
            .ToArray();
        return "/" + string.Join("/", parts);
    }

    private static string ToUserPath(string path)
    {
        return path.Replace("danteId", "Dante Id", StringComparison.Ordinal);
    }

    private static string FormatValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(vide)";
        }

        string compact = value.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 60 ? compact : compact[..57] + "...";
    }
}
