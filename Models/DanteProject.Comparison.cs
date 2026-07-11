using System.Text;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed partial class DanteProject
{
    public string CompareWith(DanteProject other)
    {
        StringBuilder builder = new();
        builder.AppendLine("COMPARAISON XML");
        builder.AppendLine("===============");
        builder.AppendLine($"Fichier ouvert : {OriginalFilePath}");
        builder.AppendLine($"Fichier comparé : {other.OriginalFilePath}");
        builder.AppendLine();

        Dictionary<string, DanteDevice> currentDevices = Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Name))
            .ToDictionary(device => device.Name, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, DanteDevice> otherDevices = other.Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Name))
            .ToDictionary(device => device.Name, StringComparer.OrdinalIgnoreCase);

        List<string> differences = [];

        foreach (string deviceName in currentDevices.Keys.Except(otherDevices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add($"Device seulement dans le fichier ouvert : {deviceName}");
        }

        foreach (string deviceName in otherDevices.Keys.Except(currentDevices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add($"Device seulement dans le fichier comparé : {deviceName}");
        }

        foreach (string deviceName in currentDevices.Keys.Intersect(otherDevices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            DanteDevice current = currentDevices[deviceName];
            DanteDevice compared = otherDevices[deviceName];
            CompareValue(differences, $"{deviceName} / mode réseau", current.NetworkMode, compared.NetworkMode);
            CompareValue(differences, $"{deviceName} / latence", DanteLatencyFormatter.FormatLatencyDisplay(current.Latency), DanteLatencyFormatter.FormatLatencyDisplay(compared.Latency));
            CompareValue(differences, $"{deviceName} / preferred master", current.PreferredMaster.ToString(), compared.PreferredMaster.ToString());
            CompareValue(differences, $"{deviceName} / samplerate", current.Element.Child("samplerate")?.Value.Trim() ?? string.Empty, compared.Element.Child("samplerate")?.Value.Trim() ?? string.Empty);
            CompareValue(differences, $"{deviceName} / encoding", current.Element.Child("encoding")?.Value.Trim() ?? string.Empty, compared.Element.Child("encoding")?.Value.Trim() ?? string.Empty);
            CompareChannels(differences, deviceName, "TX", current.TxChannels, compared.TxChannels);
            CompareChannels(differences, deviceName, "RX", current.RxChannels, compared.RxChannels);
        }

        Dictionary<string, DanteSubscription> currentPatches = PatchMatrix.Subscriptions
            .GroupBy(BuildPatchKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, DanteSubscription> otherPatches = other.PatchMatrix.Subscriptions
            .GroupBy(BuildPatchKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (string patchKey in currentPatches.Keys.Except(otherPatches.Keys, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add($"{patchKey} : patch seulement dans le fichier ouvert ({FormatPatchForComparison(currentPatches[patchKey])})");
        }

        foreach (string patchKey in otherPatches.Keys.Except(currentPatches.Keys, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add($"{patchKey} : patch seulement dans le fichier comparé ({FormatPatchForComparison(otherPatches[patchKey])})");
        }

        foreach (string patchKey in currentPatches.Keys.Intersect(otherPatches.Keys, StringComparer.OrdinalIgnoreCase))
        {
            DanteSubscription current = currentPatches[patchKey];
            DanteSubscription compared = otherPatches[patchKey];
            string currentPatch = FormatPatchForComparison(current);
            string comparedPatch = FormatPatchForComparison(compared);
            if (!string.Equals(currentPatch, comparedPatch, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add($"{patchKey} : fichier ouvert = {currentPatch} | fichier comparé = {comparedPatch}");
            }
        }

        if (differences.Count == 0)
        {
            builder.AppendLine("Aucune différence détectée dans les champs connus.");
        }
        else
        {
            foreach (string difference in differences.Take(250))
            {
                builder.AppendLine("- " + difference);
            }

            if (differences.Count > 250)
            {
                builder.AppendLine($"- {differences.Count - 250} différence(s) supplémentaire(s) non affichée(s).");
            }
        }

        return builder.ToString();
    }

    private static void CompareValue(List<string> differences, string label, string current, string compared)
    {
        if (!string.Equals(current, compared, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add($"{label}: {Blank(current)} -> {Blank(compared)}");
        }
    }

    private static void CompareChannels(
        List<string> differences,
        string deviceName,
        string kind,
        IReadOnlyList<DanteChannel> currentChannels,
        IReadOnlyList<DanteChannel> comparedChannels)
    {
        Dictionary<int, DanteChannel> currentById = currentChannels
            .GroupBy(channel => channel.DanteId)
            .ToDictionary(group => group.Key, group => group.First());
        Dictionary<int, DanteChannel> comparedById = comparedChannels
            .GroupBy(channel => channel.DanteId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (int danteId in currentById.Keys.Except(comparedById.Keys).OrderBy(id => id))
        {
            differences.Add($"{deviceName} / {kind} Dante Id {danteId}: seulement dans le fichier ouvert ({currentById[danteId].DisplayName})");
        }

        foreach (int danteId in comparedById.Keys.Except(currentById.Keys).OrderBy(id => id))
        {
            differences.Add($"{deviceName} / {kind} Dante Id {danteId}: seulement dans le fichier comparé ({comparedById[danteId].DisplayName})");
        }

        foreach (int danteId in currentById.Keys.Intersect(comparedById.Keys).OrderBy(id => id))
        {
            DanteChannel current = currentById[danteId];
            DanteChannel compared = comparedById[danteId];
            if (!string.Equals(current.DisplayName, compared.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add($"{deviceName} / {kind} Dante Id {danteId}: {current.DisplayName} -> {compared.DisplayName}");
            }
        }
    }

    private static string BuildPatchKey(DanteSubscription subscription)
    {
        return $"{subscription.RxDevice} / RX Dante Id {subscription.RxDanteId}";
    }

    private static string FormatPatchForComparison(DanteSubscription subscription)
    {
        if (!subscription.IsActive)
        {
            return "(libre)";
        }

        string sourceDevice = subscription.IsLocalSubscription
            ? $"LOCAL / {subscription.ResolvedTxDeviceName}"
            : Blank(subscription.DisplayTxDeviceName);

        return $"{sourceDevice} / {Blank(subscription.TxChannelName)} [{subscription.TypeLabel}]";
    }
}
