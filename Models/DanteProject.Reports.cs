using System.IO;
using System.Text;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed partial class DanteProject
{
    public string BuildSaveSummary()
    {
        StringBuilder builder = new();
        DanteValidationResult validation = Validate();

        builder.AppendLine("RÉSUMÉ AVANT SAUVEGARDE");
        builder.AppendLine("=======================");
        builder.AppendLine($"Fichier original : {OriginalFilePath}");
        builder.AppendLine($"Dernier fichier sauvegardé : {LastSavedPath ?? "aucun"}");
        builder.AppendLine();
        builder.AppendLine("Compteurs");
        builder.AppendLine("--------");
        builder.AppendLine($"Devices : {Devices.Count}");
        builder.AppendLine($"Canaux TX : {Devices.Sum(device => device.TxCount)}");
        builder.AppendLine($"Canaux RX : {Devices.Sum(device => device.RxCount)}");
        builder.AppendLine($"Patchs actifs : {PatchMatrix.ActivePatchCount}");
        builder.AppendLine($"Patchs modifiés : {_modifiedRxElements.Count}");
        builder.AppendLine();
        AppendImportantWarnings(builder, BuildImportantWarnings());
        builder.AppendLine();
        builder.AppendLine("Validation");
        builder.AppendLine("----------");
        builder.AppendLine(validation.ToDisplayText());
        builder.AppendLine();
        builder.AppendLine(DanteXmlChangeGuardService.BuildGuardReport(ValidateXmlChangeGuard()));
        builder.AppendLine();
        AppendChangeTable(builder);

        return builder.ToString();
    }

    public string BuildReportText()
    {
        StringBuilder builder = new();
        DanteValidationResult validation = Validate();

        builder.AppendLine("DANTE CONFIG EDITOR - RAPPORT");
        builder.AppendLine("=============================");
        builder.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Fichier : {OriginalFilePath}");
        builder.AppendLine($"Statut : {(IsModified ? "Modifié non sauvegardé" : "Non modifié")}");
        builder.AppendLine();
        builder.AppendLine("Synthèse");
        builder.AppendLine("--------");
        builder.AppendLine($"Devices : {Devices.Count}");
        builder.AppendLine($"Canaux TX : {Devices.Sum(device => device.TxCount)}");
        builder.AppendLine($"Canaux RX : {Devices.Sum(device => device.RxCount)}");
        builder.AppendLine($"Patchs actifs : {PatchMatrix.ActivePatchCount}");
        builder.AppendLine($"Conflits : {PatchMatrix.ConflictCount}");
        builder.AppendLine();
        AppendImportantWarnings(builder, BuildImportantWarnings());
        builder.AppendLine();

        builder.AppendLine("Validation");
        builder.AppendLine("----------");
        builder.AppendLine(validation.ToDisplayText());
        builder.AppendLine();
        builder.AppendLine(BuildCompatibilityReport());
        builder.AppendLine();

        builder.AppendLine("Devices");
        builder.AppendLine("-------");
        AppendTableHeader(builder, "Device", "Réseau", "Latence", "TX/RX");
        foreach (DanteDevice device in Devices)
        {
            AppendTableRow(builder, device.Name, device.NetworkMode, DanteLatencyFormatter.FormatLatencyWithXmlValue(device.Latency), $"{device.TxCount}/{device.RxCount}");
        }

        builder.AppendLine();
        builder.AppendLine("Patchs actifs et conflits");
        builder.AppendLine("-------------------------");
        AppendTableHeader(builder, "RX", "TX", "Canal TX", "État");
        foreach (DanteSubscription subscription in PatchMatrix.Subscriptions.Where(subscription => subscription.IsActive || subscription.IsConflict))
        {
            AppendTableRow(
                builder,
                $"{subscription.RxDevice} / {subscription.RxChannelName}",
                string.IsNullOrWhiteSpace(subscription.TxDevice) ? "-" : subscription.TxDevice,
                string.IsNullOrWhiteSpace(subscription.TxChannelName) ? "-" : subscription.TxChannelName,
                subscription.Status);
        }

        builder.AppendLine();
        AppendChangeTable(builder);

        return builder.ToString();
    }

    public string BuildPatchbookText(string scope, string? scopeDisplay = null)
    {
        DanteValidationResult validation = Validate();
        IEnumerable<DanteSubscription> subscriptions = PatchMatrix.Subscriptions;
        subscriptions = scope switch
        {
            "Filter.ActivePatches" => subscriptions.Where(subscription => subscription.IsActive),
            "Filter.WarningsConflicts" => subscriptions.Where(subscription => subscription.IsWarning || subscription.IsConflict),
            "Patchs actifs" => subscriptions.Where(subscription => subscription.IsActive),
            "Warnings / conflits" => subscriptions.Where(subscription => subscription.IsWarning || subscription.IsConflict),
            _ => subscriptions
        };

        DanteSubscription[] rows = subscriptions
            .OrderBy(subscription => subscription.RxDevice, StringComparer.OrdinalIgnoreCase)
            .ThenBy(subscription => subscription.RxDanteId)
            .ToArray();

        StringBuilder builder = new();
        builder.AppendLine("DANTE CONFIG EDITOR - PATCHBOOK");
        builder.AppendLine("===============================");
        builder.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Nom du fichier source : {Path.GetFileName(OriginalFilePath)}");
        builder.AppendLine($"Chemin du fichier source : {OriginalFilePath}");
        builder.AppendLine($"Preset : {PresetName}");
        builder.AppendLine($"Version preset : {Blank(PresetVersion)}");
        builder.AppendLine($"Scope : {scopeDisplay ?? scope}");
        builder.AppendLine($"Devices : {Devices.Count}");
        builder.AppendLine($"TX total : {Devices.Sum(device => device.TxCount)}");
        builder.AppendLine($"RX total : {Devices.Sum(device => device.RxCount)}");
        builder.AppendLine($"Patchs actifs : {PatchMatrix.ActivePatchCount}");
        builder.AppendLine($"RX libres : {PatchMatrix.FreeRxCount}");
        builder.AppendLine($"Patchs locaux : {PatchMatrix.LocalPatchCount}");
        builder.AppendLine($"Warnings : {validation.Warnings.Count}");
        builder.AppendLine($"Erreurs bloquantes : {validation.Errors.Count}");
        builder.AppendLine();

        foreach (IGrouping<string, DanteSubscription> group in rows.GroupBy(subscription => subscription.RxDevice))
        {
            builder.AppendLine(group.Key);
            builder.AppendLine(new string('-', Math.Max(8, group.Key.Length)));

            foreach (DanteSubscription subscription in group)
            {
                string source = subscription.IsActive
                    ? subscription.SourceFull
                    : "(libre)";

                builder.AppendLine($"RX {subscription.RxDanteId.ToString().PadLeft(3, '0')} | {TrimForPatchbook(subscription.RxChannelName),-28} <- {TrimForPatchbook(source),-48} | {subscription.TypeLabel}");
            }

            builder.AppendLine();
        }

        if (rows.Length == 0)
        {
            builder.AppendLine("Aucune ligne à exporter avec ce filtre.");
        }

        return builder.ToString();
    }

    public string BuildPatchbookCsv(string scope)
    {
        IEnumerable<DanteSubscription> subscriptions = PatchMatrix.Subscriptions;
        subscriptions = scope switch
        {
            "Filter.ActivePatches" => subscriptions.Where(subscription => subscription.IsActive),
            "Filter.WarningsConflicts" => subscriptions.Where(subscription => subscription.IsWarning || subscription.IsConflict),
            "Patchs actifs" => subscriptions.Where(subscription => subscription.IsActive),
            "Warnings / conflits" => subscriptions.Where(subscription => subscription.IsWarning || subscription.IsConflict),
            _ => subscriptions
        };

        StringBuilder builder = new();
        builder.AppendLine("\"RxDevice\",\"Rx Dante Id\",\"RxChannel\",\"TxDevice\",\"TxChannel\",\"Type\",\"Status\"");
        foreach (DanteSubscription subscription in subscriptions.OrderBy(subscription => subscription.RxDevice, StringComparer.OrdinalIgnoreCase).ThenBy(subscription => subscription.RxDanteId))
        {
            string txDevice = subscription.IsLocalSubscription ? "LOCAL" : subscription.DisplayTxDeviceName;
            builder.AppendLine(string.Join(",",
                Csv(subscription.RxDevice),
                subscription.RxDanteId.ToString(),
                Csv(subscription.RxChannelName),
                Csv(txDevice),
                Csv(subscription.TxChannelName),
                Csv(subscription.TypeLabel),
                Csv(subscription.Status)));
        }

        return builder.ToString();
    }

    public string BuildCompatibilityReport()
    {
        DanteValidationResult validation = Validate();
        DanteValidationResult compatibility = DanteXmlCompatibilityService.ValidateCompatibility(Document, _originalCompatibilityProfile);
        DanteValidationResult guard = ValidateXmlChangeGuard();

        StringBuilder builder = new();
        builder.AppendLine("Compatibilité XML");
        builder.AppendLine("-----------------");
        builder.AppendLine(Document.Root?.Name.LocalName == "preset" ? "OK Racine <preset>" : "ERROR Racine <preset> absente ou modifiée");
        builder.AppendLine(!string.IsNullOrWhiteSpace(PresetVersion) ? "OK Version du preset conservée" : "WARNING Version du preset absente");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("nombre de devices", StringComparison.OrdinalIgnoreCase)) ? "ERROR Devices modifiés" : "OK Devices conservés");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("canaux TX", StringComparison.OrdinalIgnoreCase)) ? "ERROR TX modifiés" : "OK TX conservés");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("canaux RX", StringComparison.OrdinalIgnoreCase)) ? "ERROR RX modifiés" : "OK RX conservés");
        builder.AppendLine(HasCompatibilityError(compatibility, "dante", "TX")
            ? "ERROR Dante Id TX manquant ou modifié"
            : "OK Tous les Dante Id TX sont présents");
        builder.AppendLine(HasCompatibilityError(compatibility, "dante", "RX")
            ? "ERROR Dante Id RX manquant ou modifié"
            : "OK Tous les Dante Id RX sont présents");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("mediaType", StringComparison.OrdinalIgnoreCase) && error.Contains("TX", StringComparison.OrdinalIgnoreCase))
            ? "ERROR mediaType TX manquant ou modifié"
            : "OK Tous les mediaType TX sont présents");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("mediaType", StringComparison.OrdinalIgnoreCase) && error.Contains("RX", StringComparison.OrdinalIgnoreCase))
            ? "ERROR mediaType RX manquant ou modifié"
            : "OK Tous les mediaType RX sont présents");
        builder.AppendLine(compatibility.Errors.Any(error => error.Contains("Balise technique", StringComparison.OrdinalIgnoreCase)) ? "ERROR Balises techniques principales modifiées" : "OK Balises techniques principales conservées");
        builder.AppendLine(guard.HasErrors ? "ERROR Changement interdit détecté" : "OK Aucun changement interdit détecté");
        builder.AppendLine($"WARNING Devices TX référencés mais absents du preset : {PatchMatrix.ExternalMissingDeviceCount}");
        builder.AppendLine($"WARNING Canaux TX référencés mais absents : {PatchMatrix.MissingTxChannelCount}");
        builder.AppendLine($"Warnings non bloquants : {validation.Warnings.Count}");
        builder.AppendLine($"Erreurs bloquantes : {validation.Errors.Count}");
        builder.AppendLine();
        builder.AppendLine(DanteXmlChangeGuardService.BuildGuardReport(guard));
        return builder.ToString();
    }

    public string BuildTopologyText()
    {
        DanteSubscription[] activeSubscriptions = PatchMatrix.Subscriptions
            .Where(subscription => subscription.IsActive)
            .ToArray();

        StringBuilder builder = new();
        builder.AppendLine("TOPOLOGIE SIMPLE");
        builder.AppendLine("================");
        builder.AppendLine();
        builder.AppendLine("Sources les plus utilisées");
        builder.AppendLine("--------------------------");
        foreach (IGrouping<string, DanteSubscription> group in activeSubscriptions.GroupBy(subscription => subscription.IsLocalSubscription ? "LOCAL" : subscription.DisplayTxDeviceName).OrderByDescending(group => group.Count()).Take(20))
        {
            builder.AppendLine($"{Blank(group.Key),-30} -> {group.Count()} RX");
        }

        builder.AppendLine();
        builder.AppendLine("Receivers les plus patchés");
        builder.AppendLine("--------------------------");
        foreach (IGrouping<string, DanteSubscription> group in activeSubscriptions.GroupBy(subscription => subscription.RxDevice).OrderByDescending(group => group.Count()).Take(20))
        {
            builder.AppendLine($"{group.Key,-30} -> {group.Count()} RX actifs");
        }

        builder.AppendLine();
        builder.AppendLine("Relations TX -> RX");
        builder.AppendLine("------------------");
        foreach (IGrouping<string, DanteSubscription> sourceGroup in activeSubscriptions.GroupBy(subscription => subscription.IsLocalSubscription ? "LOCAL" : subscription.DisplayTxDeviceName).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase).Take(80))
        {
            builder.AppendLine(Blank(sourceGroup.Key));
            foreach (IGrouping<string, DanteSubscription> rxGroup in sourceGroup.GroupBy(subscription => subscription.RxDevice).OrderByDescending(group => group.Count()).Take(20))
            {
                builder.AppendLine($"  -> {rxGroup.Key} : {rxGroup.Count()} patchs");
            }
        }

        return builder.ToString();
    }

    public string ListRedundantDevices()
    {
        return BuildDeviceList(Devices.Where(device => device.IsRedundant), "Aucune machine redondante trouvée.");
    }

    public string ListDaisychainDevices()
    {
        return BuildDeviceList(Devices.Where(device => !device.IsRedundant), "Aucune machine en daisychain trouvée.");
    }

    public string ListLatencies()
    {
        List<string> lines = Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Latency))
            .Select(device => $"{device.Name}: {DanteLatencyFormatter.FormatLatencyWithXmlValue(device.Latency)}")
            .ToList();

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Aucune latence définie.";
    }

    public string ListSamplerates()
    {
        List<string> lines = Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Samplerate))
            .Select(device => $"{device.Name}: {FormatSamplerateForDisplay(device.Samplerate)}")
            .ToList();

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Aucune sample rate définie.";
    }

    public string ListEncodings()
    {
        List<string> lines = Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Encoding))
            .Select(device => $"{device.Name}: {FormatEncodingForDisplay(device.Encoding)}")
            .ToList();

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Aucun encodage défini.";
    }

    public string ListStaticIpDevices()
    {
        DanteDevice[] staticIpDevices = Devices.Where(device => device.UsesStaticIp).ToArray();
        if (staticIpDevices.Length == 0)
        {
            return "Aucune IP fixe détectée.";
        }

        return string.Join(Environment.NewLine, staticIpDevices.Select(FormatStaticIpDevice));
    }

    public string ListPreferredMasters()
    {
        return BuildDeviceList(Devices.Where(device => device.PreferredMaster), "Aucune machine preferred master trouvée.");
    }

    private static bool HasCompatibilityError(DanteValidationResult compatibility, string firstNeedle, string secondNeedle)
    {
        return compatibility.Errors.Any(error =>
            error.Contains(firstNeedle, StringComparison.OrdinalIgnoreCase)
            && error.Contains(secondNeedle, StringComparison.OrdinalIgnoreCase));
    }

    private void AppendChangeTable(StringBuilder builder)
    {
        builder.AppendLine("Modifications");
        builder.AppendLine("-------------");

        if (_changes.Count == 0)
        {
            builder.AppendLine("- Aucune modification depuis le chargement.");
        }
        else
        {
            AppendTableHeader(builder, "Heure", "Action", "Détail", "");
            foreach (ChangeRecord change in _changes)
            {
                AppendTableRow(builder, change.Timestamp.ToString("HH:mm:ss"), change.Action, change.Details, "");
            }
        }
    }

    private static void AppendImportantWarnings(StringBuilder builder, IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        builder.AppendLine("!!! POINTS À VÉRIFIER IMPORTANTS !!!");
        builder.AppendLine("------------------------------------");
        foreach (string warning in warnings)
        {
            builder.AppendLine("- " + warning);
        }
    }

    private static void AppendTableHeader(StringBuilder builder, string first, string second, string third, string fourth)
    {
        builder.AppendLine($"{TrimForColumn(first),-18} | {TrimForColumn(second),-22} | {TrimForColumn(third),-36} | {TrimForColumn(fourth),-18}");
        builder.AppendLine(new string('-', 103));
    }

    private static void AppendTableRow(StringBuilder builder, string first, string second, string third, string fourth)
    {
        builder.AppendLine($"{TrimForColumn(first),-18} | {TrimForColumn(second),-22} | {TrimForColumn(third),-36} | {TrimForColumn(fourth),-18}");
    }

    private static string TrimForColumn(string value)
    {
        string cleanValue = value.ReplaceLineEndings(" ").Trim();
        return cleanValue.Length <= 34 ? cleanValue : cleanValue[..31] + "...";
    }

    private static string TrimForPatchbook(string value)
    {
        string cleanValue = value.ReplaceLineEndings(" ").Trim();
        return cleanValue.Length <= 46 ? cleanValue : cleanValue[..43] + "...";
    }

    private static string Csv(string value)
    {
        string cleanValue = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{cleanValue}\"";
    }

    private static string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(vide)" : value;
    }

    private static string FormatStaticIpDevice(DanteDevice device)
    {
        return string.IsNullOrWhiteSpace(device.StaticIpAddress)
            ? device.Name
            : $"{device.Name} ({device.StaticIpAddress})";
    }

    private static string BuildDeviceList(IEnumerable<DanteDevice> devices, string emptyMessage)
    {
        List<string> names = devices.Select(device => device.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        return names.Count > 0 ? string.Join(Environment.NewLine, names) : emptyMessage;
    }
}
