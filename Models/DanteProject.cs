using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed class DanteProject
{
    // Noms de balises reconnus pour identifier le device TX auquel un RX est abonné.
    // Plusieurs variantes sont acceptées pour rester tolérant avec les exports XML.
    private static readonly string[] SubscriptionDeviceElementNames =
    [
        "subscribed_device",
        "subscription_device",
        "tx_device",
        "source_device"
    ];

    // Noms de balises reconnus pour le canal TX de destination d'un patch.
    private static readonly string[] SubscriptionChannelElementNames =
    [
        "subscribed_channel",
        "subscribed_channel_name",
        "subscribed_channel_label",
        "subscribed_tx_channel",
        "subscribed_tx_channel_name",
        "subscribed_label",
        "source_channel",
        "source_channel_name"
    ];

    // Les éléments RX modifiés sont gardés pour l'affichage et pour le résumé
    // avant sauvegarde. La clé reste l'élément XML exact.
    private readonly Dictionary<XElement, bool> _modifiedRxElements = [];
    private readonly List<ChangeRecord> _changes = [];
    private readonly Stack<UndoSnapshot> _undoSnapshots = [];

    private DanteProject(string originalFilePath, XDocument document)
    {
        OriginalFilePath = originalFilePath;
        Document = document;
        ReloadModel();
    }

    public string OriginalFilePath { get; }

    public string? LastSavedPath { get; private set; }

    public XDocument Document { get; private set; }

    public IReadOnlyList<DanteDevice> Devices { get; private set; } = [];

    public DantePatchMatrix PatchMatrix { get; private set; } = new([]);

    public IReadOnlyList<ChangeRecord> Changes => _changes;

    public bool IsModified { get; private set; }

    public bool CanUndo => _undoSnapshots.Count > 0;

    public string LastUndoLabel => _undoSnapshots.TryPeek(out UndoSnapshot? snapshot) ? snapshot.Label : "Aucune action";

    public static DanteProject Load(string path)
    {
        XDocument document;
        try
        {
            // PreserveWhitespace évite de réécrire inutilement tout le fichier
            // lors du chargement.
            document = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Le fichier XML n'est pas lisible : {ex.Message}", ex);
        }

        if (document.Root is null)
        {
            throw new InvalidOperationException("Le fichier XML ne contient pas de racine.");
        }

        DanteProject project = new(path, document);
        if (project.Devices.Count == 0)
        {
            throw new InvalidOperationException("Aucun élément <device> n'a été trouvé. Ce fichier ne semble pas compatible avec cette version.");
        }

        return project;
    }

    public void PushUndoSnapshot(string label)
    {
        // Copie complète du XML : plus lourd qu'une annulation ciblée, mais plus sûr
        // car les modifications peuvent toucher plusieurs balises de patch.
        _undoSnapshots.Push(new UndoSnapshot(
            label,
            new XDocument(Document),
            IsModified,
            _changes.Count,
            CaptureModifiedRxReferences()));
    }

    public void RestoreLastUndoSnapshot()
    {
        if (_undoSnapshots.Count == 0)
        {
            return;
        }

        RestoreSnapshot(_undoSnapshots.Pop());
    }

    public string UndoLastChange()
    {
        if (_undoSnapshots.Count == 0)
        {
            throw new InvalidOperationException("Aucune action à annuler.");
        }

        UndoSnapshot snapshot = _undoSnapshots.Pop();
        RestoreSnapshot(snapshot);
        return snapshot.Label;
    }

    public DanteDevice? FindDevice(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Devices.FirstOrDefault(device => string.Equals(device.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void RenameDevice(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        {
            throw new InvalidOperationException("Le nom actuel et le nouveau nom doivent être renseignés.");
        }

        if (ContainsProblematicCharacters(newName))
        {
            throw new InvalidOperationException("Le nouveau nom contient des caractères non imprimables.");
        }

        if (Devices.Any(device => string.Equals(device.Name, newName, StringComparison.OrdinalIgnoreCase) && !string.Equals(device.Name, oldName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Un autre device porte déjà ce nom.");
        }

        DanteDevice device = FindDevice(oldName) ?? throw new InvalidOperationException("Device introuvable.");
        SetElementValue(device.Element, "name", newName.Trim());
        SetElementValue(device.Element, "friendly_name", newName.Trim());

        // Si le device TX est renommé, les patchs qui pointaient vers son ancien
        // nom doivent suivre pour ne pas casser les abonnements reconnus.
        foreach (XElement rxChannel in Document.Root!.Elements("device").Elements("rxchannel"))
        {
            XElement? subscribedDevice = FindFirstElement(rxChannel, SubscriptionDeviceElementNames);
            if (subscribedDevice is not null && string.Equals(subscribedDevice.Value.Trim(), oldName, StringComparison.OrdinalIgnoreCase))
            {
                subscribedDevice.Value = newName.Trim();
                _modifiedRxElements[rxChannel] = true;
            }
        }

        RegisterChange("Nom device", $"{oldName} -> {newName.Trim()}");
    }

    public void SetNetworkMode(string deviceName, bool redundant)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        SetBooleanElementAttribute(device.Element, "redundancy", "value", redundant, afterElementName: "friendly_name");
        RegisterChange("Mode réseau", $"{deviceName} -> {(redundant ? "redondant" : "daisychain")}");
    }

    public void SetAllNetworkModes(bool redundant)
    {
        foreach (DanteDevice device in Devices)
        {
            SetBooleanElementAttribute(device.Element, "redundancy", "value", redundant, afterElementName: "friendly_name");
        }

        RegisterChange("Mode réseau global", redundant ? "Tous redondants" : "Tous en daisychain");
    }

    public void SetLatency(string deviceName, string latency)
    {
        ValidateLatency(latency);
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        SetElementValue(device.Element, "unicast_latency", latency);
        RegisterChange("Latence", $"{deviceName} -> {latency} ms");
    }

    public void SetAllLatencies(string latency)
    {
        ValidateLatency(latency);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "unicast_latency", latency);
        }

        RegisterChange("Latence globale", $"Tous -> {latency} ms");
    }

    public void SetPreferredMaster(string deviceName, bool preferredMaster)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        SetBooleanElementAttribute(device.Element, "preferred_master", "value", preferredMaster, afterElementName: "redundancy");
        RegisterChange("Preferred master", $"{deviceName} -> {preferredMaster}");
    }

    public void SetAllPreferredMasters(bool preferredMaster)
    {
        foreach (DanteDevice device in Devices)
        {
            SetBooleanElementAttribute(device.Element, "preferred_master", "value", preferredMaster, afterElementName: "redundancy");
        }

        RegisterChange("Preferred master global", $"Tous -> {preferredMaster}");
    }

    public void ResetChannels(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        ResetDeviceChannels(device);
        RegisterChange("Canaux", $"Réinitialisation des canaux de {deviceName}");
    }

    public void RenameChannel(string deviceName, DanteChannelKind channelKind, int channelIndex, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new InvalidOperationException("Le nouveau nom de canal doit être renseigné.");
        }

        if (ContainsProblematicCharacters(newName))
        {
            throw new InvalidOperationException("Le nouveau nom de canal contient des caractères non imprimables.");
        }

        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        IReadOnlyList<DanteChannel> channels = channelKind == DanteChannelKind.Tx ? device.TxChannels : device.RxChannels;
        DanteChannel channel = channels.FirstOrDefault(candidate => candidate.Index == channelIndex)
            ?? throw new InvalidOperationException("Canal introuvable.");

        string oldName = channel.DisplayName;
        string trimmedNewName = newName.Trim();

        if (channelKind == DanteChannelKind.Tx)
        {
            SetChannelDisplayName(channel, "label", trimmedNewName);
            // Un canal TX peut être utilisé par plusieurs RX : on met à jour
            // toutes les références reconnues dans le fichier.
            UpdateSubscriptionsForRenamedTxChannel(device.Name, oldName, trimmedNewName);
        }
        else
        {
            SetChannelDisplayName(channel, "name", trimmedNewName);
        }

        RegisterChange("Nom canal", $"{deviceName} {channelKind} {channelIndex}: {oldName} -> {trimmedNewName}");
    }

    public void BatchRenameChannels(string deviceName, DanteChannelKind channelKind, string prefix, int firstNumber, int startChannelIndex, int endChannelIndex)
    {
        if (firstNumber < 0)
        {
            throw new InvalidOperationException("Le numéro de départ doit être positif.");
        }

        if (startChannelIndex > endChannelIndex)
        {
            throw new InvalidOperationException("La plage de canaux est invalide.");
        }

        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        IReadOnlyList<DanteChannel> channels = channelKind == DanteChannelKind.Tx ? device.TxChannels : device.RxChannels;
        if (channels.Count == 0)
        {
            throw new InvalidOperationException("Aucun canal à renommer pour ce device.");
        }

        DanteChannel[] selectedChannels = channels
            .Where(channel => channel.Index >= startChannelIndex && channel.Index <= endChannelIndex)
            .ToArray();

        if (selectedChannels.Length == 0)
        {
            throw new InvalidOperationException("Aucun canal trouvé dans cette plage.");
        }

        string cleanPrefix = prefix.Trim();
        int digits = Math.Max(2, (firstNumber + selectedChannels.Length - 1).ToString().Length);
        int number = firstNumber;
        List<(string OldName, string NewName)> txRenames = [];

        // On renomme seulement la plage demandée. Les autres canaux restent
        // intacts, même s'ils sont du même type TX/RX.
        foreach (DanteChannel channel in selectedChannels)
        {
            string oldName = channel.DisplayName;
            string newName = string.IsNullOrWhiteSpace(cleanPrefix)
                ? number.ToString().PadLeft(digits, '0')
                : $"{cleanPrefix} {number.ToString().PadLeft(digits, '0')}";

            if (ContainsProblematicCharacters(newName))
            {
                throw new InvalidOperationException("Le préfixe contient des caractères non imprimables.");
            }

            SetChannelDisplayName(channel, channelKind == DanteChannelKind.Tx ? "label" : "name", newName);
            if (channelKind == DanteChannelKind.Tx)
            {
                txRenames.Add((oldName, newName));
            }

            number++;
        }

        if (channelKind == DanteChannelKind.Tx)
        {
            // Mise à jour groupée après le renommage : cela évite les effets
            // de cascade si un nouveau nom ressemble à un ancien nom.
            UpdateSubscriptionsForRenamedTxChannels(device.Name, txRenames);
        }

        RegisterChange("Renommage série", $"{deviceName} {channelKind}: canaux {startChannelIndex}-{endChannelIndex}, {cleanPrefix} depuis {firstNumber}");
    }

    public void ResetAllChannels()
    {
        foreach (DanteDevice device in Devices)
        {
            ResetDeviceChannels(device);
        }

        RegisterChange("Canaux global", "Réinitialisation des canaux de tous les devices");
    }

    public void ApplyPatch(string rxDeviceName, int rxIndex, string txDeviceName, string txChannelName)
    {
        if (string.IsNullOrWhiteSpace(rxDeviceName))
        {
            throw new InvalidOperationException("Aucun device récepteur sélectionné.");
        }

        if (string.IsNullOrWhiteSpace(txDeviceName))
        {
            throw new InvalidOperationException("Aucun device émetteur sélectionné.");
        }

        DanteDevice txDevice = FindDevice(txDeviceName) ?? throw new InvalidOperationException("Le device émetteur n'existe pas dans ce fichier.");
        if (!string.IsNullOrWhiteSpace(txChannelName) && txDevice.TxChannels.Count > 0 && !ChannelExists(txDevice.TxChannels, txChannelName))
        {
            throw new InvalidOperationException("Le canal TX sélectionné n'existe pas sur le device émetteur.");
        }

        XElement rxElement = FindRxElement(rxDeviceName, rxIndex);
        // Si les balises de patch n'existent pas encore, elles sont créées avec
        // le premier nom reconnu par l'application.
        SetElementValue(rxElement, SubscriptionDeviceElementNames[0], txDeviceName.Trim());
        SetSubscriptionChannel(rxElement, txChannelName.Trim());
        _modifiedRxElements[rxElement] = true;

        RegisterChange("Patch", $"{rxDeviceName} RX {rxIndex} -> {txDeviceName} {txChannelName}".Trim());
    }

    public void RemovePatch(string rxDeviceName, int rxIndex)
    {
        XElement rxElement = FindRxElement(rxDeviceName, rxIndex);
        foreach (string elementName in SubscriptionDeviceElementNames.Concat(SubscriptionChannelElementNames))
        {
            rxElement.Element(elementName)?.Remove();
        }

        _modifiedRxElements[rxElement] = true;
        RegisterChange("Patch supprimé", $"{rxDeviceName} RX {rxIndex}");
    }

    public DanteValidationResult Validate()
    {
        // Validation volontairement prudente : erreurs bloquantes pour les cas
        // structurels, avertissements pour les patchs non résolus.
        DanteValidationResult result = new();
        if (Document.Root is null)
        {
            result.Errors.Add("Le document XML ne contient pas de racine.");
            return result;
        }

        if (Devices.Count == 0)
        {
            result.Errors.Add("Aucun device Dante n'a été détecté.");
            return result;
        }

        result.Warnings.AddRange(BuildImportantWarnings());

        foreach (DanteDevice device in Devices)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                result.Errors.Add("Un device a un nom vide.");
            }

            if (ContainsProblematicCharacters(device.Name))
            {
                result.Errors.Add($"Le device '{device.Name}' contient des caractères non imprimables.");
            }

            foreach (DanteChannel channel in device.TxChannels.Concat(device.RxChannels))
            {
                if (string.IsNullOrWhiteSpace(channel.DisplayName))
                {
                    result.Warnings.Add($"{device.Name} contient un canal {channel.Kind} sans nom lisible.");
                }

                if (ContainsProblematicCharacters(channel.DisplayName))
                {
                    result.Warnings.Add($"{device.Name} / {channel.DisplayName} contient des caractères non imprimables.");
                }
            }
        }

        foreach (IGrouping<string, DanteDevice> group in Devices.GroupBy(device => device.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            {
                result.Errors.Add($"Le nom de device '{group.Key}' est présent plusieurs fois.");
            }
        }

        foreach (DanteSubscription subscription in PatchMatrix.Subscriptions.Where(subscription => subscription.IsActive))
        {
            DanteDevice? txDevice = FindDevice(subscription.TxDevice);
            if (txDevice is null)
            {
                result.Warnings.Add($"{subscription.Display} pointe vers un device émetteur introuvable : {subscription.TxDevice}.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(subscription.TxChannelName) && txDevice.TxChannels.Count > 0 && !ChannelExists(txDevice.TxChannels, subscription.TxChannelName))
            {
                result.Warnings.Add($"{subscription.Display} pointe vers un canal TX non retrouvé : {subscription.TxChannelName}.");
            }
        }

        return result;
    }

    public IReadOnlyList<string> BuildImportantWarnings()
    {
        List<string> warnings = [];

        int redundantCount = Devices.Count(device => device.IsRedundant);
        int daisychainCount = Devices.Count(device => !device.IsRedundant);
        if (redundantCount > 0 && daisychainCount > 0)
        {
            warnings.Add($"ATTENTION : le fichier mélange {redundantCount} machine(s) en redondant et {daisychainCount} machine(s) en daisychain. Vérifiez que c'est volontaire pour ce réseau.");
        }

        DanteDevice[] staticIpDevices = Devices.Where(device => device.UsesStaticIp).ToArray();
        if (staticIpDevices.Length > 0)
        {
            string devices = string.Join(", ", staticIpDevices.Take(12).Select(FormatStaticIpDevice));
            if (staticIpDevices.Length > 12)
            {
                devices += $", +{staticIpDevices.Length - 12} autre(s)";
            }

            warnings.Add($"IP fixe détectée sur {staticIpDevices.Length} machine(s) : {devices}.");
        }

        return warnings;
    }

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

        builder.AppendLine("Devices");
        builder.AppendLine("-------");
        AppendTableHeader(builder, "Device", "Réseau", "Latence", "TX/RX");
        foreach (DanteDevice device in Devices)
        {
            AppendTableRow(builder, device.Name, device.NetworkMode, string.IsNullOrWhiteSpace(device.Latency) ? "-" : device.Latency, $"{device.TxCount}/{device.RxCount}");
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
            CompareValue(differences, $"{deviceName} / latence", current.Latency, compared.Latency);
            CompareValue(differences, $"{deviceName} / preferred master", current.PreferredMaster.ToString(), compared.PreferredMaster.ToString());
            CompareChannels(differences, deviceName, "TX", current.TxChannels, compared.TxChannels);
            CompareChannels(differences, deviceName, "RX", current.RxChannels, compared.RxChannels);
        }

        Dictionary<string, DanteSubscription> currentPatches = PatchMatrix.Subscriptions.ToDictionary(BuildPatchKey, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, DanteSubscription> otherPatches = other.PatchMatrix.Subscriptions.ToDictionary(BuildPatchKey, StringComparer.OrdinalIgnoreCase);
        foreach (string patchKey in currentPatches.Keys.Intersect(otherPatches.Keys, StringComparer.OrdinalIgnoreCase))
        {
            DanteSubscription current = currentPatches[patchKey];
            DanteSubscription compared = otherPatches[patchKey];
            CompareValue(differences, $"{patchKey} / TX device", current.TxDevice, compared.TxDevice);
            CompareValue(differences, $"{patchKey} / TX canal", current.TxChannelName, compared.TxChannelName);
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

    public string SaveAs(string destinationPath)
    {
        DanteValidationResult validation = Validate();
        if (validation.HasErrors)
        {
            throw new InvalidOperationException("Sauvegarde impossible tant que des erreurs bloquantes existent." + Environment.NewLine + validation.ToDisplayText());
        }

        string backupPath = SafeFileService.CreateOriginalBackup(OriginalFilePath);
        string temporaryPath = destinationPath + ".tmp";

        try
        {
            // On sauvegarde d'abord dans un fichier temporaire, puis on le relit.
            // Cela évite de remplacer le fichier final par un XML illisible.
            Document.Save(temporaryPath, SaveOptions.DisableFormatting);
            _ = Load(temporaryPath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(temporaryPath, destinationPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        LastSavedPath = destinationPath;
        IsModified = false;
        RegisterChange("Sauvegarde", $"Fichier sauvegardé sous {destinationPath}");
        IsModified = false;
        _undoSnapshots.Clear();
        return backupPath;
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
            .Select(device => $"{device.Name}: {device.Latency} ms")
            .ToList();

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Aucune latence définie.";
    }

    public string ListPreferredMasters()
    {
        return BuildDeviceList(Devices.Where(device => device.PreferredMaster), "Aucune machine preferred master trouvée.");
    }

    private void RestoreSnapshot(UndoSnapshot snapshot)
    {
        // Restauration utilisée par Annuler action et par les erreurs pendant
        // une action utilisateur.
        Document = new XDocument(snapshot.Document);
        IsModified = snapshot.WasModified;

        while (_changes.Count > snapshot.ChangeCount)
        {
            _changes.RemoveAt(_changes.Count - 1);
        }

        _modifiedRxElements.Clear();
        ReloadModel();
        RestoreModifiedRxReferences(snapshot.ModifiedRxReferences);
        ReloadModel();
    }

    private IReadOnlyList<ModifiedRxReference> CaptureModifiedRxReferences()
    {
        return PatchMatrix.Subscriptions
            .Where(subscription => subscription.IsModified)
            .Select(subscription => new ModifiedRxReference(subscription.RxDevice, subscription.RxIndex))
            .ToArray();
    }

    private void RestoreModifiedRxReferences(IEnumerable<ModifiedRxReference> references)
    {
        foreach (ModifiedRxReference reference in references)
        {
            DanteDevice? rxDevice = FindDevice(reference.RxDevice);
            DanteChannel? channel = rxDevice?.RxChannels.FirstOrDefault(rx => rx.Index == reference.RxIndex);
            if (channel is not null)
            {
                _modifiedRxElements[channel.Element] = true;
            }
        }
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
        int max = Math.Max(currentChannels.Count, comparedChannels.Count);
        for (int index = 0; index < max; index++)
        {
            DanteChannel? current = currentChannels.ElementAtOrDefault(index);
            DanteChannel? compared = comparedChannels.ElementAtOrDefault(index);
            if (current is null)
            {
                differences.Add($"{deviceName} / {kind} {index + 1}: absent dans le fichier ouvert, présent dans le fichier comparé ({compared!.DisplayName})");
            }
            else if (compared is null)
            {
                differences.Add($"{deviceName} / {kind} {index + 1}: présent dans le fichier ouvert ({current.DisplayName}), absent dans le fichier comparé");
            }
            else if (!string.Equals(current.DisplayName, compared.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add($"{deviceName} / {kind} {index + 1}: {current.DisplayName} -> {compared.DisplayName}");
            }
        }
    }

    private static string BuildPatchKey(DanteSubscription subscription)
    {
        return $"{subscription.RxDevice} / RX {subscription.RxIndex}";
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

    private void ReloadModel()
    {
        // Après chaque modification XML, les objets de lecture sont reconstruits
        // pour refléter les nouvelles valeurs.
        Devices = Document.Root?.Elements("device").Select(device => new DanteDevice(device)).ToList() ?? [];
        PatchMatrix = new DantePatchMatrix(BuildSubscriptions());
    }

    private IReadOnlyList<DanteSubscription> BuildSubscriptions()
    {
        List<DanteSubscription> subscriptions = [];
        Dictionary<string, DanteDevice> devicesByName = Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Name))
            .GroupBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (DanteDevice rxDevice in Devices)
        {
            foreach (DanteChannel rxChannel in rxDevice.RxChannels)
            {
                string txDeviceName = FindFirstElement(rxChannel.Element, SubscriptionDeviceElementNames)?.Value.Trim() ?? string.Empty;
                string txChannelName = FindFirstElement(rxChannel.Element, SubscriptionChannelElementNames)?.Value.Trim() ?? string.Empty;
                string status = "Libre";

                // La table Patch indique aussi les conflits simples :
                // device TX absent ou canal TX introuvable.
                if (!string.IsNullOrWhiteSpace(txDeviceName))
                {
                    if (!devicesByName.TryGetValue(txDeviceName, out DanteDevice? txDevice))
                    {
                        status = "Conflit - device TX introuvable";
                    }
                    else if (!string.IsNullOrWhiteSpace(txChannelName) && txDevice.TxChannels.Count > 0 && !ChannelExists(txDevice.TxChannels, txChannelName))
                    {
                        status = "Conflit - canal TX introuvable";
                    }
                    else
                    {
                        status = "Patch actif";
                    }
                }

                subscriptions.Add(new DanteSubscription(
                    rxDevice.Name,
                    rxChannel.Index,
                    rxChannel.DisplayName,
                    rxChannel.Element,
                    txDeviceName,
                    txChannelName,
                    _modifiedRxElements.ContainsKey(rxChannel.Element),
                    status));
            }
        }

        return subscriptions;
    }

    private XElement FindRxElement(string rxDeviceName, int rxIndex)
    {
        DanteDevice rxDevice = FindDevice(rxDeviceName) ?? throw new InvalidOperationException("Le device récepteur n'existe pas dans ce fichier.");
        DanteChannel? channel = rxDevice.RxChannels.FirstOrDefault(rx => rx.Index == rxIndex);
        return channel?.Element ?? throw new InvalidOperationException("Le canal RX sélectionné est introuvable.");
    }

    private static bool ChannelExists(IEnumerable<DanteChannel> channels, string channelName)
    {
        return channels.Any(channel =>
            string.Equals(channel.DisplayName, channelName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(channel.Index.ToString(), channelName, StringComparison.OrdinalIgnoreCase));
    }

    private static XElement? FindFirstElement(XElement parent, IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            XElement? element = parent.Element(name);
            if (element is not null)
            {
                return element;
            }
        }

        return null;
    }

    private static void SetElementValue(XElement parent, string elementName, string value)
    {
        XElement? element = parent.Element(elementName);
        if (element is null)
        {
            parent.Add(new XElement(elementName, value));
        }
        else
        {
            element.Value = value;
        }
    }

    private static void SetChannelDisplayName(DanteChannel channel, string fallbackElementName, string value)
    {
        // Point important pour la compatibilité : si le nom venait d'un attribut
        // ou d'une balise précise, on écrit au même endroit.
        if (!string.IsNullOrWhiteSpace(channel.NameSource))
        {
            if (channel.NameSourceIsAttribute)
            {
                channel.Element.SetAttributeValue(channel.NameSource, value);
            }
            else
            {
                SetElementValue(channel.Element, channel.NameSource, value);
            }

            return;
        }

        SetElementValue(channel.Element, fallbackElementName, value);
    }

    private static void SetBooleanElementAttribute(XElement parent, string elementName, string attributeName, bool value, string afterElementName)
    {
        XElement? element = parent.Element(elementName);
        if (element is null)
        {
            element = new XElement(elementName, new XAttribute(attributeName, value.ToString().ToLowerInvariant()));
            XElement? previous = parent.Element(afterElementName);
            if (previous is null)
            {
                parent.Add(element);
            }
            else
            {
                previous.AddAfterSelf(element);
            }
        }
        else
        {
            element.SetAttributeValue(attributeName, value.ToString().ToLowerInvariant());
        }
    }

    private static void SetSubscriptionChannel(XElement rxElement, string txChannelName)
    {
        XElement? channelElement = FindFirstElement(rxElement, SubscriptionChannelElementNames);
        if (string.IsNullOrWhiteSpace(txChannelName))
        {
            channelElement?.Remove();
            return;
        }

        if (channelElement is null)
        {
            rxElement.Add(new XElement(SubscriptionChannelElementNames[0], txChannelName));
        }
        else
        {
            channelElement.Value = txChannelName;
        }
    }

    private void ResetDeviceChannels(DanteDevice device)
    {
        int index = 1;
        List<(string OldName, string NewName)> txRenames = [];

        // Les TX sont traités à part pour pouvoir mettre à jour les patchs qui
        // utilisaient leurs anciens noms.
        foreach (DanteChannel channel in device.TxChannels)
        {
            string oldName = channel.DisplayName;
            string newName = index.ToString();
            SetChannelDisplayName(channel, "label", newName);
            txRenames.Add((oldName, newName));
            index++;
        }

        UpdateSubscriptionsForRenamedTxChannels(device.Name, txRenames);

        index = 1;
        foreach (DanteChannel channel in device.RxChannels)
        {
            SetChannelDisplayName(channel, "name", index.ToString());
            index++;
        }
    }

    private void UpdateSubscriptionsForRenamedTxChannel(string txDeviceName, string oldChannelName, string newChannelName)
    {
        UpdateSubscriptionsForRenamedTxChannels(txDeviceName, new[] { (OldName: oldChannelName, NewName: newChannelName) });
    }

    private void UpdateSubscriptionsForRenamedTxChannels(string txDeviceName, IEnumerable<(string OldName, string NewName)> renamedChannels)
    {
        // Dictionnaire ancien nom -> nouveau nom. Il est insensible à la casse
        // pour mieux tolérer les variations d'écriture dans les XML.
        Dictionary<string, string> renamedByOldName = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string oldName, string newName) in renamedChannels)
        {
            string cleanOldName = oldName.Trim();
            string cleanNewName = newName.Trim();
            if (string.IsNullOrWhiteSpace(cleanOldName)
                || string.Equals(cleanOldName, cleanNewName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            renamedByOldName.TryAdd(cleanOldName, cleanNewName);
        }

        if (renamedByOldName.Count == 0)
        {
            return;
        }

        foreach (XElement rxChannel in Document.Root!.Elements("device").Elements("rxchannel"))
        {
            bool sameDevice = rxChannel.Elements()
                .Where(element => SubscriptionDeviceElementNames.Contains(element.Name.LocalName))
                .Any(element => string.Equals(element.Value.Trim(), txDeviceName, StringComparison.OrdinalIgnoreCase));

            if (!sameDevice)
            {
                continue;
            }

            foreach (XElement subscribedChannel in rxChannel.Elements().Where(element => SubscriptionChannelElementNames.Contains(element.Name.LocalName)))
            {
                if (renamedByOldName.TryGetValue(subscribedChannel.Value.Trim(), out string? newChannelName))
                {
                    subscribedChannel.Value = newChannelName;
                    _modifiedRxElements[rxChannel] = true;
                }
            }
        }
    }

    private static void ValidateLatency(string latency)
    {
        string[] allowedValues = ["250", "1000", "2000", "5000"];
        if (!allowedValues.Contains(latency))
        {
            throw new InvalidOperationException("Latence non reconnue. Valeurs autorisées : 250, 1000, 2000 ou 5000 ms.");
        }
    }

    private void RegisterChange(string action, string details)
    {
        _changes.Add(new ChangeRecord(DateTime.Now, action, details));
        IsModified = true;
        ReloadModel();
    }

    private static bool ContainsProblematicCharacters(string value)
    {
        return value.Any(character => char.IsControl(character));
    }

    private static string BuildDeviceList(IEnumerable<DanteDevice> devices, string emptyMessage)
    {
        List<string> names = devices.Select(device => device.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        return names.Count > 0 ? string.Join(Environment.NewLine, names) : emptyMessage;
    }

    private sealed record UndoSnapshot(
        string Label,
        XDocument Document,
        bool WasModified,
        int ChangeCount,
        IReadOnlyList<ModifiedRxReference> ModifiedRxReferences);

    private sealed record ModifiedRxReference(string RxDevice, int RxIndex);
}
