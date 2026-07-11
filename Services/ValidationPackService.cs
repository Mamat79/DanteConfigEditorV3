using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public sealed record ValidationPackScenario(
    string Key,
    string Label,
    string FileName,
    string Status,
    string Detail);

public sealed record ValidationPackResult(
    string SourcePath,
    string OutputDirectory,
    string SourceSha256,
    IReadOnlyList<ValidationPackScenario> Scenarios);

public static class ValidationPackService
{
    private const string OriginalCopyFileName = "00_original_copy.xml";

    public static ValidationPackResult Create(string sourcePath, string outputDirectory)
    {
        string fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("Le XML source est introuvable.", fullSourcePath);
        }

        string fullOutputDirectory = PrepareEmptyOutputDirectory(outputDirectory);
        string sourceHashBefore = ComputeSha256(fullSourcePath);
        string originalCopyPath = Path.Combine(fullOutputDirectory, OriginalCopyFileName);
        File.Copy(fullSourcePath, originalCopyPath, overwrite: false);
        string reportsDirectory = Path.Combine(fullOutputDirectory, "reports");
        Directory.CreateDirectory(reportsDirectory);

        List<ValidationPackScenario> scenarios =
        [
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "save-without-change",
                "Sauvegarde sans modification",
                "01_saved_without_change.xml",
                _ => "Aucune modification demandée."),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "device-renamed",
                "Device renommé",
                "02_device_renamed.xml",
                RenameFirstDevice),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "tx-renamed",
                "Canal TX renommé",
                "03_tx_renamed.xml",
                RenameFirstTxChannel),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "patch-modified",
                "Patch modifié",
                "04_patch_modified.xml",
                ModifyFirstPatch),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "latency-modified",
                "Latence modifiée",
                "05_latency_modified.xml",
                ModifyFirstLatency),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "preferred-master-modified",
                "Preferred master modifié",
                "06_preferred_master_modified.xml",
                ModifyFirstPreferredMaster),
            CreateScenario(
                originalCopyPath,
                fullOutputDirectory,
                reportsDirectory,
                "primary-ip-modified",
                "IP principale modifiée",
                "07_primary_ip_modified.xml",
                ModifyFirstPrimaryIp)
        ];

        RemoveInternalBackups(fullOutputDirectory);
        WriteReports(originalCopyPath, fullOutputDirectory, scenarios);
        WriteControllerChecklist(fullOutputDirectory, scenarios);

        string sourceHashAfter = ComputeSha256(fullSourcePath);
        if (!string.Equals(sourceHashBefore, sourceHashAfter, StringComparison.Ordinal))
        {
            throw new IOException("Le XML source a changé pendant la création du pack. Le résultat doit être considéré comme invalide.");
        }

        ValidationPackResult result = new(fullSourcePath, fullOutputDirectory, sourceHashAfter, scenarios);
        WriteManifest(fullOutputDirectory, result);
        WriteHashes(fullOutputDirectory);
        return result;
    }

    private static string PrepareEmptyOutputDirectory(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Le dossier de sortie doit être renseigné.", nameof(outputDirectory));
        }

        string fullOutputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(fullOutputDirectory))
        {
            if (Directory.EnumerateFileSystemEntries(fullOutputDirectory).Any())
            {
                throw new IOException($"Le dossier de sortie doit être vide : {fullOutputDirectory}");
            }
        }
        else
        {
            Directory.CreateDirectory(fullOutputDirectory);
        }

        return fullOutputDirectory;
    }

    private static void RemoveInternalBackups(string outputDirectory)
    {
        // SaveAs protège chaque écriture par une copie. Dans ce pack, la source de
        // SaveAs est déjà notre copie interne : ces doublons peuvent être retirés.
        string backupDirectory = Path.GetFullPath(Path.Combine(outputDirectory, "DanteConfigEditor_Backups"));
        string outputRoot = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!backupDirectory.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Le dossier de sauvegarde interne sort du dossier du pack.");
        }

        if (Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, recursive: true);
        }
    }

    private static ValidationPackScenario CreateScenario(
        string originalCopyPath,
        string outputDirectory,
        string reportsDirectory,
        string key,
        string label,
        string fileName,
        Func<DanteProject, string> mutation)
    {
        DanteProject project = DanteProject.Load(originalCopyPath);
        string detail;
        try
        {
            detail = mutation(project);
        }
        catch (ValidationScenarioNotApplicableException exception)
        {
            File.WriteAllText(
                Path.Combine(reportsDirectory, $"compatibility-{key}.txt"),
                $"SKIPPED - {exception.Message}{Environment.NewLine}",
                new UTF8Encoding(false));
            return new ValidationPackScenario(key, label, fileName, "SKIPPED", exception.Message);
        }

        string compatibilityReport = project.BuildCompatibilityReport();
        string destinationPath = Path.Combine(outputDirectory, fileName);
        project.SaveAs(destinationPath);
        File.WriteAllText(
            Path.Combine(reportsDirectory, $"compatibility-{key}.txt"),
            compatibilityReport,
            new UTF8Encoding(false));
        return new ValidationPackScenario(key, label, fileName, "CREATED", detail);
    }

    private static string RenameFirstDevice(DanteProject project)
    {
        DanteDevice device = project.Devices.First();
        string newName = BuildUniqueDeviceName(project, device.Name + "-VALIDATION");
        project.RenameDevice(device.Name, newName);
        return $"{device.Name} -> {newName}";
    }

    private static string RenameFirstTxChannel(DanteProject project)
    {
        DanteDevice device = project.Devices.FirstOrDefault(candidate => candidate.TxChannels.Count > 0)
            ?? throw new ValidationScenarioNotApplicableException("Aucun canal TX n'est présent dans ce preset.");
        DanteChannel channel = device.TxChannels.First();
        string newName = BuildUniqueChannelName(device, channel.DisplayName + " VALIDATION");
        project.RenameChannel(device.Name, DanteChannelKind.Tx, channel.DanteId, newName);
        return $"{device.Name} / TX {channel.DanteId}: {channel.DisplayName} -> {newName}";
    }

    private static string ModifyFirstPatch(DanteProject project)
    {
        DanteDevice rxDevice = project.Devices.FirstOrDefault(candidate => candidate.RxChannels.Count > 0)
            ?? throw new ValidationScenarioNotApplicableException("Aucun canal RX n'est présent dans ce preset.");
        DanteChannel rxChannel = rxDevice.RxChannels.First();
        DanteSubscription? current = project.PatchMatrix.Subscriptions.FirstOrDefault(subscription =>
            string.Equals(subscription.RxDevice, rxDevice.Name, StringComparison.OrdinalIgnoreCase)
            && subscription.RxDanteId == rxChannel.DanteId);
        PatchCandidate? replacement = project.Devices
            .SelectMany(device => device.TxChannels.Select(channel => new PatchCandidate(device, channel)))
            .FirstOrDefault(candidate =>
                current is null
                || !string.Equals(current.ResolvedTxDeviceName, candidate.Device.Name, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(current.TxChannelName, candidate.Channel.DisplayName, StringComparison.OrdinalIgnoreCase));

        if (replacement is not null)
        {
            project.ApplyPatch(
                rxDevice.Name,
                rxChannel.DanteId,
                replacement.Device.Name,
                replacement.Channel.DisplayName);
            return $"{rxDevice.Name} / RX {rxChannel.DanteId} -> {replacement.Device.Name} / {replacement.Channel.DisplayName}";
        }

        if (current?.IsActive == true)
        {
            project.RemovePatch(rxDevice.Name, rxChannel.DanteId);
            return $"{rxDevice.Name} / RX {rxChannel.DanteId}: patch supprimé";
        }

        throw new ValidationScenarioNotApplicableException("Aucune source TX ne permet de modifier un patch dans ce preset.");
    }

    private static string ModifyFirstLatency(DanteProject project)
    {
        DanteDevice device = project.Devices.First();
        string nextLatency = device.Latency switch
        {
            "250" => "1000",
            "1000" => "2000",
            "2000" => "5000",
            _ => "1000"
        };
        project.SetLatency(device.Name, nextLatency);
        return $"{device.Name}: {device.Latency} -> {nextLatency}";
    }

    private static string ModifyFirstPreferredMaster(DanteProject project)
    {
        DanteDevice device = project.Devices.First();
        project.SetPreferredMaster(device.Name, !device.PreferredMaster);
        return $"{device.Name}: {device.PreferredMaster} -> {!device.PreferredMaster}";
    }

    private static string ModifyFirstPrimaryIp(DanteProject project)
    {
        DanteDevice device = project.Devices.FirstOrDefault(candidate => project.SupportsIpConfiguration(candidate.Name))
            ?? throw new ValidationScenarioNotApplicableException("Aucune interface IPv4 principale modifiable n'est présente dans ce preset.");

        if (device.UsesStaticIp)
        {
            project.SetIpAddressDynamic(device.Name);
            return $"{device.Name}: IP statique {device.StaticIpAddress} -> automatique";
        }

        const string validationAddress = "192.0.2.10";
        project.SetIpAddressStatic(device.Name, validationAddress, "255.255.255.0", "0.0.0.0");
        return $"{device.Name}: IP automatique -> {validationAddress}/24 (TEST-NET-1)";
    }

    private static string BuildUniqueDeviceName(DanteProject project, string proposedName)
    {
        string candidate = proposedName;
        int suffix = 2;
        while (project.FindDevice(candidate) is not null)
        {
            candidate = $"{proposedName}-{suffix++}";
        }

        return candidate;
    }

    private static string BuildUniqueChannelName(DanteDevice device, string proposedName)
    {
        string candidate = proposedName;
        int suffix = 2;
        while (device.TxChannels.Any(channel => string.Equals(channel.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{proposedName} {suffix++}";
        }

        return candidate;
    }

    private static void WriteReports(
        string originalCopyPath,
        string outputDirectory,
        IReadOnlyList<ValidationPackScenario> scenarios)
    {
        string reportsDirectory = Path.Combine(outputDirectory, "reports");
        DanteProject original = DanteProject.Load(originalCopyPath);

        StringBuilder beforeAfter = new();
        beforeAfter.AppendLine("RAPPORT AVANT / APRÈS");
        beforeAfter.AppendLine("======================");
        beforeAfter.AppendLine();
        beforeAfter.AppendLine($"Original copié : {OriginalCopyFileName}");
        beforeAfter.AppendLine();

        foreach (ValidationPackScenario scenario in scenarios)
        {
            beforeAfter.AppendLine($"## {scenario.Label} [{scenario.Status}]");
            beforeAfter.AppendLine(scenario.Detail);
            if (scenario.Status == "CREATED")
            {
                DanteProject generated = DanteProject.Load(Path.Combine(outputDirectory, scenario.FileName));
                beforeAfter.AppendLine(generated.CompareWith(original));
            }

            beforeAfter.AppendLine();
        }

        File.WriteAllText(Path.Combine(reportsDirectory, "before-after.txt"), beforeAfter.ToString(), new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(reportsDirectory, "compatibility-original.txt"),
            original.BuildCompatibilityReport(),
            new UTF8Encoding(false));

    }

    private static void WriteControllerChecklist(string outputDirectory, IReadOnlyList<ValidationPackScenario> scenarios)
    {
        StringBuilder builder = new();
        builder.AppendLine("# Checklist Dante Controller");
        builder.AppendLine();
        builder.AppendLine("Aucune case ne doit être cochée sans import réellement observé dans Dante Controller.");
        builder.AppendLine();
        foreach (ValidationPackScenario scenario in scenarios)
        {
            builder.AppendLine($"## {scenario.Label}");
            builder.AppendLine($"Fichier : `{scenario.FileName}` - état de génération : **{scenario.Status}**");
            builder.AppendLine();
            builder.AppendLine("- [ ] Le fichier est accepté à l'import.");
            builder.AppendLine("- [ ] Les devices et leurs Dante Id sont conservés.");
            builder.AppendLine("- [ ] Les mediaType et les canaux TX/RX sont conservés.");
            builder.AppendLine("- [ ] Les patchs normaux et locaux (`.`) correspondent au résultat attendu.");
            builder.AppendLine("- [ ] La latence, le sample rate et l'encoding correspondent au résultat attendu.");
            builder.AppendLine("- [ ] Le preferred master correspond au résultat attendu.");
            builder.AppendLine("- [ ] L'IP principale est correcte et les interfaces secondaires sont intactes.");
            builder.AppendLine("- [ ] Aucun warning inattendu n'apparaît.");
            builder.AppendLine("- [ ] Les différences avec l'original sont consignées.");
            builder.AppendLine();
        }

        File.WriteAllText(Path.Combine(outputDirectory, "DANTE_CONTROLLER_CHECKLIST.md"), builder.ToString(), new UTF8Encoding(false));
    }

    private static void WriteManifest(string outputDirectory, ValidationPackResult result)
    {
        object manifest = new
        {
            formatVersion = 1,
            generatedUtc = DateTimeOffset.UtcNow,
            sourceFileName = Path.GetFileName(result.SourcePath),
            sourceSha256 = result.SourceSha256,
            originalCopy = OriginalCopyFileName,
            scenarios = result.Scenarios.Select(scenario => new
            {
                scenario.Key,
                scenario.Label,
                scenario.FileName,
                scenario.Status,
                scenario.Detail
            })
        };
        JsonSerializerOptions options = new() { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(outputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, options),
            new UTF8Encoding(false));
    }

    private static void WriteHashes(string outputDirectory)
    {
        string hashFilePath = Path.Combine(outputDirectory, "SHA256SUMS.txt");
        string[] files = Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, hashFilePath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetRelativePath(outputDirectory, path), StringComparer.Ordinal)
            .ToArray();

        StringBuilder builder = new();
        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(outputDirectory, file).Replace('\\', '/');
            builder.AppendLine($"{ComputeSha256(file)}  {relativePath}");
        }

        File.WriteAllText(hashFilePath, builder.ToString(), new UTF8Encoding(false));
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private sealed record PatchCandidate(DanteDevice Device, DanteChannel Channel);

    private sealed class ValidationScenarioNotApplicableException(string message) : Exception(message);
}
