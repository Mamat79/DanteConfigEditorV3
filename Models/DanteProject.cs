using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Net;
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

    private static readonly HashSet<string> SupportedSamplerates = new(StringComparer.OrdinalIgnoreCase)
    {
        "44100",
        "48000",
        "88200",
        "96000",
        "176400",
        "192000"
    };

    private static readonly HashSet<string> SupportedEncodings = new(StringComparer.OrdinalIgnoreCase)
    {
        "16",
        "24",
        "32"
    };

    private static readonly string[] StaticIpAttributeNames =
    [
        "address",
        "dnsserver",
        "gateway",
        "ip",
        "ipv4",
        "mask",
        "netmask",
        "static_address",
        "static_ip",
        "subnet",
        "subnet_mask",
        "value"
    ];

    private static readonly string[] StaticIpElementNames =
    [
        "address",
        "netmask",
        "gateway",
        "dnsserver"
    ];

    // Les éléments RX modifiés sont gardés pour l'affichage et pour le résumé
    // avant sauvegarde. La clé reste l'élément XML exact.
    private readonly Dictionary<XElement, bool> _modifiedRxElements = [];
    private readonly List<ChangeRecord> _changes = [];
    private readonly Stack<UndoSnapshot> _undoSnapshots = [];
    private readonly XDocument _originalDocument;
    private readonly DanteXmlCompatibilityProfile _originalCompatibilityProfile;

    private DanteProject(string originalFilePath, XDocument document, XDocument? originalDocument = null)
    {
        OriginalFilePath = originalFilePath;
        Document = document;
        _originalDocument = new XDocument(originalDocument ?? document);
        _originalCompatibilityProfile = DanteXmlCompatibilityService.CaptureProfile(_originalDocument);
        ReloadModel();
    }

    public string OriginalFilePath { get; }

    public string PresetName => Document.Root?.Element("name")?.Value.Trim() ?? Path.GetFileNameWithoutExtension(OriginalFilePath);

    public string PresetVersion => Document.Root?.Attribute("version")?.Value ?? string.Empty;

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
        XDocument document = LoadDocument(path);

        DanteProject project = new(path, document);
        if (project.Devices.Count == 0)
        {
            throw new InvalidOperationException("Aucun élément <device> n'a été trouvé. Ce fichier ne semble pas compatible avec cette version.");
        }

        return project;
    }

    public static DanteProject LoadRecovered(string originalPath, string recoveryPath)
    {
        XDocument originalDocument = LoadDocument(originalPath);
        XDocument recoveredDocument = LoadDocument(recoveryPath);
        DanteProject project = new(originalPath, recoveredDocument, originalDocument);
        if (project.Devices.Count == 0)
        {
            throw new InvalidOperationException("La session récupérée ne contient aucune machine Dante.");
        }

        project.IsModified = true;
        project._changes.Add(new ChangeRecord(DateTime.Now, "Récupération automatique", "Session temporaire restaurée après interruption"));
        project.MarkRecoveredPatchChanges();
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
        RegisterChange("Latence", $"{deviceName} -> {DanteLatencyFormatter.FormatLatencyWithXmlValue(latency)}");
    }

    public void SetAllLatencies(string latency)
    {
        ValidateLatency(latency);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "unicast_latency", latency);
        }

        RegisterChange("Latence globale", $"Tous -> {DanteLatencyFormatter.FormatLatencyWithXmlValue(latency)}");
    }

    public void SetSamplerate(string deviceName, string samplerate)
    {
        string cleanSamplerate = ValidateSamplerate(samplerate);
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        SetElementValue(device.Element, "samplerate", cleanSamplerate);
        RegisterChange("Sample rate", $"{deviceName} -> {FormatSamplerateForDisplay(cleanSamplerate)}");
    }

    public void SetAllSamplerates(string samplerate)
    {
        string cleanSamplerate = ValidateSamplerate(samplerate);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "samplerate", cleanSamplerate);
        }

        RegisterChange("Sample rate globale", $"Tous -> {FormatSamplerateForDisplay(cleanSamplerate)}");
    }

    public void SetEncoding(string deviceName, string encoding)
    {
        string cleanEncoding = ValidateEncoding(encoding);
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        SetElementValue(device.Element, "encoding", cleanEncoding);
        RegisterChange("Bits par échantillon", $"{deviceName} -> {FormatEncodingForDisplay(cleanEncoding)}");
    }

    public void SetAllEncodings(string encoding)
    {
        string cleanEncoding = ValidateEncoding(encoding);
        foreach (DanteDevice device in Devices)
        {
            SetElementValue(device.Element, "encoding", cleanEncoding);
        }

        RegisterChange("Bits par échantillon globaux", $"Tous -> {FormatEncodingForDisplay(cleanEncoding)}");
    }

    public bool SetIpAddressDynamic(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        bool changed = SetDeviceIpAddressesDynamic(device);
        if (!changed)
        {
            RegisterChange("IP automatique", $"{deviceName} déjà en automatique ou sans adresse IPv4 modifiable");
            return false;
        }

        RegisterChange("IP automatique", $"{deviceName} -> dynamique");
        return true;
    }

    public void SetIpAddressStatic(string deviceName, string address, string netmask, string gateway)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        string cleanAddress = ValidateIpv4Address(address, "adresse IP");
        string cleanNetmask = ValidateIpv4Address(string.IsNullOrWhiteSpace(netmask) ? "255.255.255.0" : netmask, "masque");
        string cleanGateway = ValidateIpv4Address(string.IsNullOrWhiteSpace(gateway) ? "0.0.0.0" : gateway, "passerelle");
        SetDeviceIpAddressStatic(device, cleanAddress, cleanNetmask, cleanGateway);
        RegisterChange("IP fixe", $"{deviceName} -> {cleanAddress} / {cleanNetmask} / gateway {cleanGateway}");
    }

    public bool SupportsIpConfiguration(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        return DeviceSupportsIpConfiguration(device);
    }

    public int SetAllIpAddressesDynamic()
    {
        int changedDevices = 0;
        foreach (DanteDevice device in Devices)
        {
            if (SetDeviceIpAddressesDynamic(device))
            {
                changedDevices++;
            }
        }

        RegisterChange("IP automatique globale", $"{changedDevices} machine(s) passée(s) en dynamique");
        return changedDevices;
    }

    public void SetAllIpAddressesStaticSequential(string prefix, int startHost, string netmask, string gateway)
    {
        string cleanPrefix = ValidateIpv4Prefix(prefix);
        string cleanNetmask = ValidateIpv4Address(string.IsNullOrWhiteSpace(netmask) ? "255.255.255.0" : netmask, "masque");
        string cleanGateway = ValidateIpv4Address(string.IsNullOrWhiteSpace(gateway) ? "0.0.0.0" : gateway, "passerelle");

        if (startHost < 1 || startHost > 254)
        {
            throw new InvalidOperationException("Le premier numéro IP doit être compris entre 1 et 254.");
        }

        DanteDevice[] configurableDevices = Devices.Where(DeviceSupportsIpConfiguration).ToArray();
        if (configurableDevices.Length == 0)
        {
            throw new InvalidOperationException("Aucune machine du XML ne contient d'interface IPv4 modifiable.");
        }

        if (startHost + configurableDevices.Length - 1 > 254)
        {
            throw new InvalidOperationException("La plage IP dépasse 254. Choisissez un numéro de départ plus bas.");
        }

        int host = startHost;
        foreach (DanteDevice device in configurableDevices)
        {
            SetDeviceIpAddressStatic(device, $"{cleanPrefix}.{host}", cleanNetmask, cleanGateway);
            host++;
        }

        int skipped = Devices.Count - configurableDevices.Length;
        RegisterChange("IP fixes globales", $"{configurableDevices.Length} machine(s) depuis {cleanPrefix}.{startHost}, {skipped} ignorée(s) sans interface IPv4");
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

    public void SetSolePreferredMaster(string deviceName)
    {
        if (FindDevice(deviceName) is null)
        {
            throw new InvalidOperationException("Device introuvable.");
        }

        foreach (DanteDevice device in Devices)
        {
            SetBooleanElementAttribute(device.Element, "preferred_master", "value", string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase), afterElementName: "redundancy");
        }

        RegisterChange("Preferred master unique", $"{deviceName} défini comme seul preferred master");
    }

    public int DeleteDevice(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        if (Devices.Count <= 1)
        {
            throw new InvalidOperationException("Impossible de supprimer la dernière machine du preset.");
        }

        int removedSubscriptions = RemoveSubscriptionsReferencingDevice(device.Name);
        device.Element.Remove();
        RegisterChange("Machine supprimée", $"{deviceName} supprimé, {removedSubscriptions} patch(s) nettoyé(s)");
        return removedSubscriptions;
    }

    public int ResetDevicePatches(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        int removedRxSubscriptions = RemoveSubscriptionsFromDeviceRxChannels(device.Name);
        int removedTxSubscriptions = RemoveSubscriptionsReferencingDevice(device.Name);
        int total = removedRxSubscriptions + removedTxSubscriptions;
        RegisterChange("Patch machine reset", $"{deviceName}: {removedRxSubscriptions} entrée(s) RX et {removedTxSubscriptions} départ(s) TX supprimé(s)");
        return total;
    }

    public int ResetDeviceRxPatches(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        int removedRxSubscriptions = RemoveSubscriptionsFromDeviceRxChannels(device.Name);
        RegisterChange("Patch RX machine reset", $"{deviceName}: {removedRxSubscriptions} entrée(s) RX supprimée(s)");
        return removedRxSubscriptions;
    }

    public int ResetDeviceTxPatches(string deviceName)
    {
        DanteDevice device = FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        int removedTxSubscriptions = RemoveSubscriptionsReferencingDevice(device.Name);
        RegisterChange("Patch TX machine reset", $"{deviceName}: {removedTxSubscriptions} départ(s) TX supprimé(s)");
        return removedTxSubscriptions;
    }

    public IReadOnlyList<string> FindDuplicateDeviceNamesInXml(string path)
    {
        DanteProject importedProject = Load(path);
        return importedProject.Devices
            .Select(device => device.Name)
            .Where(name => Devices.Any(existing => string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyDictionary<string, string> BuildAutomaticDuplicateRenameMap(string path)
    {
        DanteProject importedProject = Load(path);
        HashSet<string> usedNames = Devices.Select(device => device.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> renameMap = new(StringComparer.OrdinalIgnoreCase);

        foreach (DanteDevice device in importedProject.Devices)
        {
            if (!usedNames.Contains(device.Name))
            {
                usedNames.Add(device.Name);
                continue;
            }

            string newName = BuildUniqueImportedDeviceName(device.Name, usedNames);
            renameMap[device.Name] = newName;
            usedNames.Add(newName);
        }

        return renameMap;
    }

    public DanteMergeResult MergeDevicesFromXml(string path, IReadOnlyDictionary<string, string>? duplicateRenameMap = null)
    {
        DanteProject importedProject = Load(path);
        Dictionary<string, string> cleanRenameMap = NormalizeRenameMap(duplicateRenameMap);
        HashSet<string> usedNames = Devices.Select(device => device.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<XElement> devicesToImport = [];
        List<string> skippedDuplicates = [];
        Dictionary<string, string> appliedRenames = new(StringComparer.OrdinalIgnoreCase);

        foreach (DanteDevice device in importedProject.Devices)
        {
            bool nameAlreadyUsed = usedNames.Contains(device.Name);
            XElement clone = new(device.Element);
            string targetName = device.Name;

            if (nameAlreadyUsed)
            {
                if (!cleanRenameMap.TryGetValue(device.Name, out string? renamedDeviceName) || string.IsNullOrWhiteSpace(renamedDeviceName))
                {
                    skippedDuplicates.Add(device.Name);
                    continue;
                }

                targetName = renamedDeviceName;
                RenameDeviceElement(clone, targetName);
                appliedRenames[device.Name] = targetName;
            }

            if (usedNames.Contains(targetName))
            {
                throw new InvalidOperationException($"Import refusé : le nom '{targetName}' est déjà utilisé.");
            }

            if (ContainsProblematicCharacters(targetName))
            {
                throw new InvalidOperationException($"Import refusé : le nom '{targetName}' contient des caractères non imprimables.");
            }

            usedNames.Add(targetName);
            devicesToImport.Add(clone);
        }

        foreach (XElement device in devicesToImport)
        {
            UpdateImportedSubscriptionDeviceNames(device, appliedRenames);
            Document.Root!.Add(device);
        }

        RegisterChange("Import XML", $"{devicesToImport.Count} machine(s) ajoutée(s) depuis {Path.GetFileName(path)}");
        return new DanteMergeResult(
            devicesToImport.Count,
            appliedRenames.Count,
            skippedDuplicates.Count,
            skippedDuplicates,
            appliedRenames);
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
            string newName = BuildBatchChannelName(cleanPrefix, number, digits, device.Name);

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

    private static string BuildBatchChannelName(string pattern, int number, int defaultDigits, string deviceName)
    {
        if (pattern.Contains('{', StringComparison.Ordinal))
        {
            string value = pattern
                .Replace("{device}", deviceName, StringComparison.OrdinalIgnoreCase)
                .Replace("{n}", number.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{number}", number.ToString(), StringComparison.OrdinalIgnoreCase);

            for (int digits = 1; digits <= 6; digits++)
            {
                value = value.Replace("{" + new string('0', digits) + "}", number.ToString().PadLeft(digits, '0'), StringComparison.OrdinalIgnoreCase);
            }

            return value.Trim();
        }

        return string.IsNullOrWhiteSpace(pattern)
            ? number.ToString().PadLeft(defaultDigits, '0')
            : $"{pattern} {number.ToString().PadLeft(defaultDigits, '0')}";
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
        string rawTxDeviceName = ShouldUseLocalSubscriptionMarker(rxElement, rxDeviceName, txDeviceName) ? "." : txDeviceName.Trim();
        // Si les balises de patch n'existent pas encore, elles sont créées avec
        // le premier nom reconnu par l'application.
        SetSubscriptionElements(rxElement, rawTxDeviceName, txChannelName.Trim());
        _modifiedRxElements[rxElement] = true;

        RegisterChange("Patch", $"{rxDeviceName} RX {rxIndex} -> {FormatDisplayTxDevice(rawTxDeviceName, txDeviceName)} {txChannelName}".Trim());
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

    private int RemoveSubscriptionsReferencingDevice(string txDeviceName)
    {
        int count = 0;
        foreach (XElement rxChannel in Document.Root!.Elements("device").Elements("rxchannel").ToArray())
        {
            XElement? subscribedDevice = FindFirstElement(rxChannel, SubscriptionDeviceElementNames);
            if (subscribedDevice is null || !string.Equals(subscribedDevice.Value.Trim(), txDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string elementName in SubscriptionDeviceElementNames.Concat(SubscriptionChannelElementNames))
            {
                rxChannel.Element(elementName)?.Remove();
            }

            _modifiedRxElements[rxChannel] = true;
            count++;
        }

        return count;
    }

    private int RemoveSubscriptionsFromDeviceRxChannels(string rxDeviceName)
    {
        DanteDevice device = FindDevice(rxDeviceName) ?? throw new InvalidOperationException("Device introuvable.");
        int count = 0;
        foreach (XElement rxChannel in device.Element.Elements("rxchannel").ToArray())
        {
            bool hadSubscription = SubscriptionDeviceElementNames
                .Concat(SubscriptionChannelElementNames)
                .Any(elementName => rxChannel.Element(elementName) is not null);

            if (!hadSubscription)
            {
                continue;
            }

            foreach (string elementName in SubscriptionDeviceElementNames.Concat(SubscriptionChannelElementNames))
            {
                rxChannel.Element(elementName)?.Remove();
            }

            _modifiedRxElements[rxChannel] = true;
            count++;
        }

        return count;
    }

    public DanteValidationResult Validate()
    {
        // Validation volontairement prudente : erreurs bloquantes pour les cas
        // structurels, avertissements pour les patchs non résolus.
        DanteValidationResult result = new();
        if (Document.Root is null)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, "Le document XML ne contient pas de racine.");
            return result;
        }

        if (Devices.Count == 0)
        {
            result.AddError(DanteIssueCategory.XmlCompatibility, "Aucun device Dante n'a été détecté.");
            return result;
        }

        result.Merge(DanteXmlCompatibilityService.ValidateCompatibility(Document, _originalCompatibilityProfile));
        result.Merge(ValidateXmlChangeGuard());

        foreach (string warning in BuildImportantWarnings())
        {
            result.AddWarning(DanteIssueCategory.Network, warning);
        }

        foreach (DanteDevice device in Devices)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                result.AddError(DanteIssueCategory.Device, "Un device a un nom vide.");
            }

            if (ContainsProblematicCharacters(device.Name))
            {
                result.AddError(DanteIssueCategory.Device, $"Le device '{device.Name}' contient des caractères non imprimables.", device.Name);
            }

            if (device.TxCount == 0)
            {
                result.AddWarning(DanteIssueCategory.Device, $"{device.Name} ne contient aucun canal TX.", device.Name);
            }

            if (device.RxCount == 0)
            {
                result.AddWarning(DanteIssueCategory.Device, $"{device.Name} ne contient aucun canal RX.", device.Name);
            }

            foreach (DanteChannel channel in device.TxChannels.Concat(device.RxChannels))
            {
                if (string.IsNullOrWhiteSpace(channel.DisplayName))
                {
                    result.AddWarning(DanteIssueCategory.Channel, $"{device.Name} contient un canal {channel.Kind} sans nom lisible.", device.Name, danteId: channel.DanteId);
                }

                if (ContainsProblematicCharacters(channel.DisplayName))
                {
                    result.AddWarning(DanteIssueCategory.Channel, $"{device.Name} / {channel.DisplayName} contient des caractères non imprimables.", device.Name, channel.DisplayName, channel.DanteId);
                }
            }
        }

        AddDuplicateDanteIdIssues(result);
        AddAudioFormatIssues(result);
        AddClockIssues(result);

        foreach (IGrouping<string, DanteDevice> group in Devices.GroupBy(device => device.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            {
                result.AddError(DanteIssueCategory.Device, $"Le nom de device '{group.Key}' est présent plusieurs fois.", group.Key);
            }
        }

        foreach (DanteSubscription subscription in PatchMatrix.Subscriptions)
        {
            if (!subscription.IsActive)
            {
                result.AddInfo(DanteIssueCategory.Patch, $"{subscription.Display} est libre.", subscription.RxDevice, subscription.RxChannelName, subscription.RxDanteId);
                continue;
            }

            if (subscription.IsLocalSubscription)
            {
                result.AddInfo(DanteIssueCategory.Patch, $"{subscription.Display} utilise une source locale '.'.", subscription.RxDevice, subscription.RxChannelName, subscription.RxDanteId);
            }

            if (subscription.IsExternalMissingDevice)
            {
                result.AddWarning(DanteIssueCategory.Patch, $"{subscription.Display} pointe vers un device TX absent du preset : {subscription.RawTxDeviceName}.", subscription.RxDevice, subscription.RxChannelName, subscription.RxDanteId);
            }
            else if (subscription.IsTxChannelMissing)
            {
                result.AddWarning(DanteIssueCategory.Patch, $"{subscription.Display} pointe vers un canal TX non retrouvé : {subscription.TxChannelName}.", subscription.RxDevice, subscription.RxChannelName, subscription.RxDanteId);
            }
            else if (subscription.IsConflict)
            {
                result.AddError(DanteIssueCategory.Patch, subscription.Status, subscription.RxDevice, subscription.RxChannelName, subscription.RxDanteId);
            }
        }

        return result;
    }

    public IReadOnlyList<string> BuildImportantWarnings()
    {
        return BuildImportantWarningDetails().Select(warning => warning.Message).ToArray();
    }

    public IReadOnlyList<DanteImportantWarning> BuildImportantWarningDetails()
    {
        List<DanteImportantWarning> warnings = [];

        DanteDevice[] redundantDevices = Devices.Where(device => device.IsRedundant).ToArray();
        DanteDevice[] daisychainDevices = Devices.Where(device => !device.IsRedundant).ToArray();
        int redundantCount = redundantDevices.Length;
        int daisychainCount = daisychainDevices.Length;
        if (redundantCount > 0 && daisychainCount > 0)
        {
            DanteDevice[] affectedDevices = redundantCount == daisychainCount
                ? Devices.ToArray()
                : redundantCount < daisychainCount ? redundantDevices : daisychainDevices;
            warnings.Add(new DanteImportantWarning(
                "Warning.MixedNetworkModes",
                $"ATTENTION : le fichier mélange {redundantCount} machine(s) en redondant et {daisychainCount} machine(s) en daisychain. Vérifiez que c'est volontaire pour ce réseau.",
                affectedDevices.Select(device => device.Name).ToArray(),
                $"WARNING: the file mixes {redundantCount} redundant device(s) and {daisychainCount} daisychain device(s). Check that this is intentional for this network."));
        }

        DanteDevice[] staticIpDevices = Devices.Where(device => device.UsesStaticIp).ToArray();
        if (staticIpDevices.Length > 0)
        {
            string devices = string.Join(", ", staticIpDevices.Take(12).Select(FormatStaticIpDevice));
            if (staticIpDevices.Length > 12)
            {
                devices += $", +{staticIpDevices.Length - 12} autre(s)";
            }

            warnings.Add(new DanteImportantWarning(
                "Warning.StaticIp",
                $"IP fixe détectée sur {staticIpDevices.Length} machine(s) : {devices}.",
                staticIpDevices.Select(device => device.Name).ToArray(),
                $"Static IP detected on {staticIpDevices.Length} device(s): {devices}."));
        }

        AddMixedValueWarningDetails(
            warnings,
            "Warning.MixedSampleRates",
            "fréquences d'échantillonnage",
            "sample rates",
            device => device.Samplerate,
            FormatSamplerateForDisplay);

        AddMixedValueWarningDetails(
            warnings,
            "Warning.MixedEncodings",
            "bits par échantillon",
            "bits per sample values",
            device => device.Encoding,
            FormatEncodingForDisplay);

        return warnings;
    }

    public IReadOnlyList<DeviceChangeRow> BuildDeviceChangeRows()
    {
        Dictionary<string, XElement> originalDevices = BuildDeviceIdentityMap(_originalDocument);
        Dictionary<string, XElement> currentDevices = BuildDeviceIdentityMap(Document);
        List<string> identities = originalDevices.Keys
            .Concat(currentDevices.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<DeviceChangeRow> rows = [];

        foreach (string identity in identities)
        {
            originalDevices.TryGetValue(identity, out XElement? originalElement);
            currentDevices.TryGetValue(identity, out XElement? currentElement);
            string originalName = ReadDeviceElementValue(originalElement, "name");
            string currentName = ReadDeviceElementValue(currentElement, "name");
            string displayName = !string.IsNullOrWhiteSpace(currentName) ? currentName : originalName;

            if (originalElement is null && currentElement is not null)
            {
                rows.Add(new DeviceChangeRow(displayName, "Machine", "Absente", "Ajoutée", "Ajoutée", true));
                continue;
            }

            if (currentElement is null && originalElement is not null)
            {
                rows.Add(new DeviceChangeRow(displayName, "Machine", "Présente", "Supprimée", "Supprimée", false));
                continue;
            }

            if (originalElement is null || currentElement is null)
            {
                continue;
            }

            AddDeviceFieldChanges(rows, displayName, originalElement, currentElement);
            AddChannelChanges(rows, displayName, originalElement, currentElement, DanteChannelKind.Tx);
            AddChannelChanges(rows, displayName, originalElement, currentElement, DanteChannelKind.Rx);
        }

        return rows;
    }

    public IReadOnlySet<string> GetModifiedDeviceNames()
    {
        if (!IsModified)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return BuildDeviceChangeRows()
            .Where(row => row.HasCurrentDevice)
            .Select(row => row.DeviceName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public int ApplyDeviceProfile(IEnumerable<string> deviceNames, DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidateLatency(profile.Latency);
        string samplerate = ValidateSamplerate(profile.Samplerate);
        string encoding = ValidateEncoding(profile.Encoding);
        HashSet<string> requestedNames = deviceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        DanteDevice[] targets = Devices.Where(device => requestedNames.Contains(device.Name)).ToArray();
        if (targets.Length == 0)
        {
            throw new InvalidOperationException("Aucune machine ne correspond à la cible du profil.");
        }

        int changedDevices = 0;
        foreach (DanteDevice device in targets)
        {
            bool changed = false;
            if (!string.Equals(device.Samplerate, samplerate, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "samplerate", samplerate);
                changed = true;
            }

            if (!string.Equals(device.Encoding, encoding, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "encoding", encoding);
                changed = true;
            }

            if (!string.Equals(device.Latency, profile.Latency, StringComparison.OrdinalIgnoreCase))
            {
                SetElementValue(device.Element, "unicast_latency", profile.Latency);
                changed = true;
            }

            if (profile.IsRedundant.HasValue && device.IsRedundant != profile.IsRedundant.Value)
            {
                SetBooleanElementAttribute(device.Element, "redundancy", "value", profile.IsRedundant.Value, afterElementName: "friendly_name");
                changed = true;
            }

            if (profile.SetIpAutomatic && SetDeviceIpAddressesDynamic(device))
            {
                changed = true;
            }

            if (changed)
            {
                changedDevices++;
            }
        }

        if (changedDevices > 0)
        {
            RegisterChange("Profil rapide", $"{profile.Key} appliqué à {changedDevices} machine(s)");
        }

        return changedDevices;
    }

    private void AddDuplicateDanteIdIssues(DanteValidationResult result)
    {
        foreach (DanteDevice device in Devices)
        {
            AddDuplicateDanteIdIssues(result, device, device.TxChannels, "TX");
            AddDuplicateDanteIdIssues(result, device, device.RxChannels, "RX");
        }
    }

    private static void AddDuplicateDanteIdIssues(DanteValidationResult result, DanteDevice device, IEnumerable<DanteChannel> channels, string kind)
    {
        foreach (IGrouping<int, DanteChannel> group in channels.GroupBy(channel => channel.DanteId))
        {
            if (group.Count() > 1)
            {
                result.AddError(DanteIssueCategory.Channel, $"{device.Name} contient un doublon de Dante Id {group.Key} dans les canaux {kind}.", device.Name, danteId: group.Key);
            }
        }
    }

    public DanteValidationResult ValidateXmlChangeGuard()
    {
        return DanteXmlChangeGuardService.ValidateChanges(_originalDocument, Document);
    }

    private void AddAudioFormatIssues(DanteValidationResult result)
    {
        AddDistinctValueWarning(result, "samplerate", DanteIssueCategory.AudioFormat, "Plusieurs samplerates sont présents dans le preset");
        AddDistinctValueWarning(result, "encoding", DanteIssueCategory.AudioFormat, "Plusieurs encodages sont présents dans le preset");
        AddDistinctValueWarning(result, "unicast_latency", DanteIssueCategory.Network, "Plusieurs latences sont présentes dans le preset");
    }

    private void AddDistinctValueWarning(DanteValidationResult result, string elementName, DanteIssueCategory category, string message)
    {
        string[] values = Devices
            .Select(device => device.Element.Element(elementName)?.Value.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (values.Length > 1)
        {
            result.AddWarning(category, $"{message} : {string.Join(", ", values)}.");
        }
    }

    private void AddClockIssues(DanteValidationResult result)
    {
        int preferredMasters = Devices.Count(device => device.PreferredMaster);
        if (preferredMasters == 0)
        {
            result.AddWarning(DanteIssueCategory.Clock, "Aucune machine preferred master n'est déclarée.");
        }
        else if (preferredMasters > 1)
        {
            result.AddWarning(DanteIssueCategory.Clock, $"{preferredMasters} machines sont déclarées preferred master.");
        }
    }

    public string BuildAllLatencyPreview(string latency)
    {
        ValidateLatency(latency);
        return BuildDevicePreview(
            $"appliquer la latence {DanteLatencyFormatter.FormatLatencyDisplay(latency)}",
            Devices.Select(device => (
                Device: device.Name,
                Before: DanteLatencyFormatter.FormatLatencyDisplay(device.Latency),
                After: DanteLatencyFormatter.FormatLatencyDisplay(latency),
                Changed: !string.Equals(device.Latency, latency, StringComparison.OrdinalIgnoreCase))));
    }

    public string BuildAllSampleratePreview(string samplerate)
    {
        string cleanSamplerate = ValidateSamplerate(samplerate);
        return BuildDevicePreview(
            $"appliquer la sample rate {FormatSamplerateForDisplay(cleanSamplerate)}",
            Devices.Select(device => (
                Device: device.Name,
                Before: FormatSamplerateForDisplay(device.Samplerate),
                After: FormatSamplerateForDisplay(cleanSamplerate),
                Changed: !string.Equals(device.Samplerate, cleanSamplerate, StringComparison.OrdinalIgnoreCase))));
    }

    public string BuildAllEncodingPreview(string encoding)
    {
        string cleanEncoding = ValidateEncoding(encoding);
        return BuildDevicePreview(
            $"appliquer les bits par échantillon {FormatEncodingForDisplay(cleanEncoding)}",
            Devices.Select(device => (
                Device: device.Name,
                Before: FormatEncodingForDisplay(device.Encoding),
                After: FormatEncodingForDisplay(cleanEncoding),
                Changed: !string.Equals(device.Encoding, cleanEncoding, StringComparison.OrdinalIgnoreCase))));
    }

    public string BuildAllIpAutoPreview()
    {
        return BuildDevicePreview(
            "mettre les adresses IPv4 en automatique",
            Devices.Select(device => (
                Device: device.Name,
                Before: device.IpModeDisplay,
                After: "Auto",
                Changed: DeviceHasStaticIpConfiguration(device))));
    }

    public string BuildAllStaticIpPreview(string prefix, int startHost, string netmask, string gateway)
    {
        string cleanPrefix = ValidateIpv4Prefix(prefix);
        string cleanNetmask = ValidateIpv4Address(string.IsNullOrWhiteSpace(netmask) ? "255.255.255.0" : netmask, "masque");
        string cleanGateway = ValidateIpv4Address(string.IsNullOrWhiteSpace(gateway) ? "0.0.0.0" : gateway, "passerelle");
        DanteDevice[] configurableDevices = Devices.Where(DeviceSupportsIpConfiguration).ToArray();
        if (configurableDevices.Length == 0)
        {
            throw new InvalidOperationException("Aucune machine du XML ne contient d'interface IPv4 modifiable.");
        }

        if (startHost < 1 || startHost > 254 || startHost + configurableDevices.Length - 1 > 254)
        {
            throw new InvalidOperationException("La plage IP demandée doit rester entre .1 et .254.");
        }

        Dictionary<string, string> targetByDeviceName = configurableDevices
            .Select((device, index) => new { device.Name, Address = $"{cleanPrefix}.{startHost + index}" })
            .ToDictionary(item => item.Name, item => item.Address, StringComparer.OrdinalIgnoreCase);

        return BuildDevicePreview(
            $"fixer les IP depuis {cleanPrefix}.{startHost} / {cleanNetmask} / gateway {cleanGateway}",
            Devices.Select(device =>
            {
                bool configurable = targetByDeviceName.TryGetValue(device.Name, out string? targetAddress);
                string before = device.UsesStaticIp ? device.IpModeDisplay : "Auto";
                return (
                    Device: device.Name,
                    Before: before,
                    After: configurable ? $"Fixe ({targetAddress})" : "non modifiable",
                    Changed: configurable && (!string.Equals(device.StaticIpAddress, targetAddress, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(device.StaticIpNetmask, cleanNetmask, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(device.StaticIpGateway, cleanGateway, StringComparison.OrdinalIgnoreCase)));
            }));
    }

    public string BuildAllNetworkModePreview(bool redundant)
    {
        string target = redundant ? "Redondant" : "Daisychain";
        return BuildDevicePreview(
            $"appliquer le mode réseau {target}",
            Devices.Select(device => (
                Device: device.Name,
                Before: device.NetworkMode,
                After: target,
                Changed: device.IsRedundant != redundant)));
    }

    public string BuildClearPreferredMastersPreview()
    {
        return BuildDevicePreview(
            "retirer tous les preferred masters",
            Devices.Select(device => (
                Device: device.Name,
                Before: device.PreferredMaster ? "Preferred master" : "Non preferred",
                After: "Non preferred",
                Changed: device.PreferredMaster)));
    }

    public string BuildSolePreferredMasterPreview(string deviceName)
    {
        return BuildDevicePreview(
            $"définir {deviceName} comme seul preferred master",
            Devices.Select(device => (
                Device: device.Name,
                Before: device.PreferredMaster ? "Preferred master" : "Non preferred",
                After: string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase) ? "Preferred master" : "Non preferred",
                Changed: device.PreferredMaster != string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase))));
    }

    public string BuildResetAllChannelsPreview()
    {
        StringBuilder builder = new();
        builder.AppendLine("Prévisualisation");
        builder.AppendLine("----------------");
        builder.AppendLine("Action : réinitialiser tous les noms de canaux TX/RX");
        builder.AppendLine();
        builder.AppendLine("Résumé :");
        builder.AppendLine($"- {Devices.Count} devices concernés");
        builder.AppendLine($"- {Devices.Sum(device => device.TxCount)} canaux TX potentiellement renommés");
        builder.AppendLine($"- {Devices.Sum(device => device.RxCount)} canaux RX potentiellement renommés");
        builder.AppendLine();
        builder.AppendLine("Cette action peut modifier beaucoup de noms et mettre à jour les patchs qui utilisent les TX reconnus.");
        return builder.ToString();
    }

    private static string BuildDevicePreview(string action, IEnumerable<(string Device, string Before, string After, bool Changed)> rows)
    {
        (string Device, string Before, string After, bool Changed)[] materializedRows = rows.ToArray();
        StringBuilder builder = new();
        builder.AppendLine("Prévisualisation");
        builder.AppendLine("----------------");
        builder.AppendLine("Action : " + action);
        builder.AppendLine();
        builder.AppendLine("Devices concernés :");
        foreach ((string device, string before, string after, bool changed) in materializedRows.Take(80))
        {
            builder.AppendLine($"- {device} : {Blank(before)} -> {(changed ? Blank(after) : "inchangé")}");
        }

        if (materializedRows.Length > 80)
        {
            builder.AppendLine($"- {materializedRows.Length - 80} device(s) supplémentaire(s) non affiché(s).");
        }

        builder.AppendLine();
        builder.AppendLine("Résumé :");
        builder.AppendLine($"- {materializedRows.Count(row => row.Changed)} devices modifiés");
        builder.AppendLine($"- {materializedRows.Count(row => !row.Changed)} devices inchangés");
        return builder.ToString();
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

    private static bool HasCompatibilityError(DanteValidationResult compatibility, string firstNeedle, string secondNeedle)
    {
        return compatibility.Errors.Any(error =>
            error.Contains(firstNeedle, StringComparison.OrdinalIgnoreCase)
            && error.Contains(secondNeedle, StringComparison.OrdinalIgnoreCase));
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
            CompareValue(differences, $"{deviceName} / samplerate", current.Element.Element("samplerate")?.Value.Trim() ?? string.Empty, compared.Element.Element("samplerate")?.Value.Trim() ?? string.Empty);
            CompareValue(differences, $"{deviceName} / encoding", current.Element.Element("encoding")?.Value.Trim() ?? string.Empty, compared.Element.Element("encoding")?.Value.Trim() ?? string.Empty);
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

        DanteValidationResult guard = ValidateXmlChangeGuard();
        if (guard.HasErrors)
        {
            throw new InvalidOperationException("Sauvegarde refusée : une modification interdite du XML Dante a été détectée." + Environment.NewLine + guard.ToDisplayText());
        }

        string temporaryPath = destinationPath + ".tmp";
        string backupPath = string.Empty;

        try
        {
            // On sauvegarde d'abord dans un fichier temporaire, puis on le relit.
            // Cela évite de remplacer le fichier final par un XML illisible.
            Document.Save(temporaryPath, SaveOptions.DisableFormatting);
            XDocument temporaryDocument = XDocument.Load(temporaryPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            DanteValidationResult compatibility = DanteXmlCompatibilityService.ValidateCompatibility(temporaryDocument, _originalCompatibilityProfile);
            if (compatibility.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : le XML temporaire casse la compatibilité Dante Controller." + Environment.NewLine + compatibility.ToDisplayText());
            }

            DanteValidationResult temporaryValidation = Load(temporaryPath).Validate();
            if (temporaryValidation.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : le XML temporaire contient des erreurs bloquantes." + Environment.NewLine + temporaryValidation.ToDisplayText());
            }

            DanteValidationResult temporaryGuard = DanteXmlChangeGuardService.ValidateChanges(_originalDocument, temporaryDocument);
            if (temporaryGuard.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : une modification interdite du XML Dante a été détectée dans le fichier temporaire." + Environment.NewLine + temporaryGuard.ToDisplayText());
            }

            backupPath = SafeFileService.CreateOriginalBackup(OriginalFilePath);

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

    private void AddMixedValueWarningDetails(
        List<DanteImportantWarning> warnings,
        string key,
        string label,
        string englishLabel,
        Func<DanteDevice, string> valueSelector,
        Func<string, string> formatter)
    {
        IGrouping<string, DanteDevice>[] groups = Devices
            .Where(device => !string.IsNullOrWhiteSpace(valueSelector(device)))
            .GroupBy(valueSelector, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groups.Length <= 1)
        {
            return;
        }

        IGrouping<string, DanteDevice> majority = groups
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .First();
        string[] affectedNames = groups
            .Where(group => !string.Equals(group.Key, majority.Key, StringComparison.OrdinalIgnoreCase))
            .SelectMany(group => group)
            .Select(device => device.Name)
            .ToArray();
        string[] distinctValues = groups.Select(group => group.Key).ToArray();
        warnings.Add(new DanteImportantWarning(
            key,
            $"ATTENTION : plusieurs {label} sont présentes dans le preset : {string.Join(", ", distinctValues.Select(formatter))}.",
            affectedNames,
            $"WARNING: multiple {englishLabel} are present in the preset: {string.Join(", ", distinctValues.Select(formatter))}."));
    }

    private static Dictionary<string, XElement> BuildDeviceIdentityMap(XDocument document)
    {
        Dictionary<string, XElement> devices = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> occurrences = new(StringComparer.OrdinalIgnoreCase);
        foreach (XElement device in document.Root?.Elements("device") ?? [])
        {
            string identity = BuildDeviceIdentity(device);
            occurrences.TryGetValue(identity, out int occurrence);
            occurrence++;
            occurrences[identity] = occurrence;
            devices[$"{identity}#{occurrence}"] = device;
        }

        return devices;
    }

    private static string BuildDeviceIdentity(XElement device)
    {
        string deviceId = device.Element("instance_id")?.Element("device_id")?.Value.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return "device-id:" + deviceId;
        }

        string defaultName = device.Element("default_name")?.Value.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(defaultName))
        {
            return "default-name:" + defaultName;
        }

        return "name:" + ReadDeviceElementValue(device, "name");
    }

    private static string ReadDeviceElementValue(XElement? device, string elementName)
    {
        return device?.Element(elementName)?.Value.Trim() ?? string.Empty;
    }

    private static void AddDeviceFieldChanges(
        List<DeviceChangeRow> rows,
        string deviceName,
        XElement originalElement,
        XElement currentElement)
    {
        DanteDevice original = new(originalElement);
        DanteDevice current = new(currentElement);
        AddDeviceChange(rows, deviceName, "Nom de machine", original.Name, current.Name);
        AddDeviceChange(rows, deviceName, "Friendly name", original.FriendlyName, current.FriendlyName);
        AddDeviceChange(rows, deviceName, "Mode réseau", original.NetworkMode, current.NetworkMode);
        AddDeviceChange(rows, deviceName, "Latence", original.LatencyDisplay, current.LatencyDisplay);
        AddDeviceChange(rows, deviceName, "Sample rate", original.SampleRateDisplay, current.SampleRateDisplay);
        AddDeviceChange(rows, deviceName, "Bits par échantillon", original.EncodingDisplay, current.EncodingDisplay);
        AddDeviceChange(rows, deviceName, "Preferred master", original.PreferredMaster ? "Oui" : "Non", current.PreferredMaster ? "Oui" : "Non");
        AddDeviceChange(rows, deviceName, "Adresse IP", FormatIpForComparison(original), FormatIpForComparison(current));
    }

    private static void AddChannelChanges(
        List<DeviceChangeRow> rows,
        string deviceName,
        XElement originalElement,
        XElement currentElement,
        DanteChannelKind kind)
    {
        DanteDevice originalDevice = new(originalElement);
        DanteDevice currentDevice = new(currentElement);
        IEnumerable<DanteChannel> originalChannels = kind == DanteChannelKind.Tx ? originalDevice.TxChannels : originalDevice.RxChannels;
        IEnumerable<DanteChannel> currentChannels = kind == DanteChannelKind.Tx ? currentDevice.TxChannels : currentDevice.RxChannels;
        Dictionary<string, DanteChannel> originalMap = BuildChannelIdentityMap(originalChannels);
        Dictionary<string, DanteChannel> currentMap = BuildChannelIdentityMap(currentChannels);
        string kindLabel = kind == DanteChannelKind.Tx ? "TX" : "RX";

        foreach (string identity in originalMap.Keys.Concat(currentMap.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            originalMap.TryGetValue(identity, out DanteChannel? originalChannel);
            currentMap.TryGetValue(identity, out DanteChannel? currentChannel);
            int danteId = currentChannel?.DanteId ?? originalChannel?.DanteId ?? 0;
            string channelLabel = $"Canal {kindLabel} {danteId}";

            if (originalChannel is null && currentChannel is not null)
            {
                rows.Add(new DeviceChangeRow(deviceName, channelLabel, "Absent", currentChannel.DisplayName, "Ajouté", true));
                continue;
            }

            if (currentChannel is null && originalChannel is not null)
            {
                rows.Add(new DeviceChangeRow(deviceName, channelLabel, originalChannel.DisplayName, "Supprimé", "Supprimé", true));
                continue;
            }

            if (originalChannel is null || currentChannel is null)
            {
                continue;
            }

            AddDeviceChange(rows, deviceName, channelLabel + " - nom", originalChannel.DisplayName, currentChannel.DisplayName);
            if (kind == DanteChannelKind.Rx)
            {
                AddDeviceChange(
                    rows,
                    deviceName,
                    channelLabel + " - patch",
                    ReadSubscriptionForComparison(originalChannel.Element),
                    ReadSubscriptionForComparison(currentChannel.Element));
            }
        }
    }

    private static Dictionary<string, DanteChannel> BuildChannelIdentityMap(IEnumerable<DanteChannel> channels)
    {
        Dictionary<string, DanteChannel> result = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> occurrences = new(StringComparer.OrdinalIgnoreCase);
        foreach (DanteChannel channel in channels)
        {
            string identity = channel.HasDanteId ? $"dante-id:{channel.DanteId}" : $"position:{channel.PositionIndex}";
            occurrences.TryGetValue(identity, out int occurrence);
            occurrence++;
            occurrences[identity] = occurrence;
            result[$"{identity}#{occurrence}"] = channel;
        }

        return result;
    }

    private static void AddDeviceChange(
        ICollection<DeviceChangeRow> rows,
        string deviceName,
        string parameter,
        string before,
        string after)
    {
        if (string.Equals(before?.Trim(), after?.Trim(), StringComparison.Ordinal))
        {
            return;
        }

        rows.Add(new DeviceChangeRow(
            deviceName,
            parameter,
            string.IsNullOrWhiteSpace(before) ? "-" : before,
            string.IsNullOrWhiteSpace(after) ? "-" : after,
            "Modifié",
            true));
    }

    private static string FormatIpForComparison(DanteDevice device)
    {
        if (!device.UsesStaticIp)
        {
            return "Automatique";
        }

        string address = string.IsNullOrWhiteSpace(device.StaticIpAddress) ? "adresse inconnue" : device.StaticIpAddress;
        string netmask = string.IsNullOrWhiteSpace(device.StaticIpNetmask) ? "masque inconnu" : device.StaticIpNetmask;
        string gateway = string.IsNullOrWhiteSpace(device.StaticIpGateway) ? "passerelle inconnue" : device.StaticIpGateway;
        return $"Fixe : {address} / {netmask} / {gateway}";
    }

    private static string ReadSubscriptionForComparison(XElement rxChannel)
    {
        string device = FindFirstElement(rxChannel, SubscriptionDeviceElementNames)?.Value.Trim() ?? string.Empty;
        string channel = FindFirstElement(rxChannel, SubscriptionChannelElementNames)?.Value.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(device) && string.IsNullOrWhiteSpace(channel))
        {
            return "Libre";
        }

        string displayDevice = string.Equals(device, ".", StringComparison.Ordinal) ? "LOCAL" : device;
        return $"{Blank(displayDevice)} / {Blank(channel)}";
    }

    private static XDocument LoadDocument(string path)
    {
        try
        {
            // PreserveWhitespace évite de réécrire inutilement tout le fichier.
            XDocument document = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            return document.Root is null
                ? throw new InvalidOperationException("Le fichier XML ne contient pas de racine.")
                : document;
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Le fichier XML n'est pas lisible : {ex.Message}", ex);
        }
    }

    private void MarkRecoveredPatchChanges()
    {
        Dictionary<string, XElement> originalDevices = BuildDeviceIdentityMap(_originalDocument);
        Dictionary<string, XElement> currentDevices = BuildDeviceIdentityMap(Document);
        foreach (string identity in originalDevices.Keys.Intersect(currentDevices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            DanteDevice originalDevice = new(originalDevices[identity]);
            DanteDevice currentDevice = new(currentDevices[identity]);
            Dictionary<string, DanteChannel> originalRx = BuildChannelIdentityMap(originalDevice.RxChannels);
            Dictionary<string, DanteChannel> currentRx = BuildChannelIdentityMap(currentDevice.RxChannels);
            foreach (string channelIdentity in originalRx.Keys.Intersect(currentRx.Keys, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.Equals(
                        ReadSubscriptionForComparison(originalRx[channelIdentity].Element),
                        ReadSubscriptionForComparison(currentRx[channelIdentity].Element),
                        StringComparison.Ordinal))
                {
                    _modifiedRxElements[currentRx[channelIdentity].Element] = true;
                }
            }
        }

        ReloadModel();
    }

    private static string FormatPatchbookSourceDevice(DanteSubscription subscription)
    {
        return subscription.IsLocalSubscription
            ? "LOCAL"
            : Blank(subscription.DisplayTxDeviceName);
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
                string rawTxDeviceName = FindFirstElement(rxChannel.Element, SubscriptionDeviceElementNames)?.Value.Trim() ?? string.Empty;
                string txChannelName = FindFirstElement(rxChannel.Element, SubscriptionChannelElementNames)?.Value.Trim() ?? string.Empty;
                string resolvedTxDeviceName = string.Equals(rawTxDeviceName, ".", StringComparison.Ordinal)
                    ? rxDevice.Name
                    : rawTxDeviceName;
                string displayTxDeviceName = FormatDisplayTxDevice(rawTxDeviceName, resolvedTxDeviceName);
                string status = "Libre";
                DanteSubscriptionKind kind = DanteSubscriptionKind.Free;

                // La table Patch indique aussi les conflits simples :
                // device TX absent ou canal TX introuvable.
                if (string.IsNullOrWhiteSpace(rawTxDeviceName) != string.IsNullOrWhiteSpace(txChannelName))
                {
                    status = "Conflit - abonnement incomplet";
                    kind = DanteSubscriptionKind.Conflict;
                }
                else if (!string.IsNullOrWhiteSpace(rawTxDeviceName))
                {
                    bool isLocal = string.Equals(rawTxDeviceName, ".", StringComparison.Ordinal);
                    if (!devicesByName.TryGetValue(resolvedTxDeviceName, out DanteDevice? txDevice))
                    {
                        status = "Warning - device TX absent du preset";
                        kind = DanteSubscriptionKind.ExternalMissingDevice;
                    }
                    else if (!string.IsNullOrWhiteSpace(txChannelName) && txDevice.TxChannels.Count > 0 && !ChannelExists(txDevice.TxChannels, txChannelName))
                    {
                        status = "Warning - canal TX absent";
                        kind = DanteSubscriptionKind.MissingChannel;
                    }
                    else if (isLocal)
                    {
                        status = "Patch local";
                        kind = DanteSubscriptionKind.Local;
                    }
                    else
                    {
                        status = "Patch actif";
                        kind = DanteSubscriptionKind.Normal;
                    }
                }

                subscriptions.Add(new DanteSubscription(
                    rxDevice.Name,
                    rxChannel.DanteId,
                    rxChannel.PositionIndex,
                    rxChannel.DisplayName,
                    rxChannel.Element,
                    rawTxDeviceName,
                    resolvedTxDeviceName,
                    displayTxDeviceName,
                    txChannelName,
                    _modifiedRxElements.ContainsKey(rxChannel.Element),
                    status,
                    kind));
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

    private static Dictionary<string, string> NormalizeRenameMap(IReadOnlyDictionary<string, string>? renameMap)
    {
        Dictionary<string, string> clean = new(StringComparer.OrdinalIgnoreCase);
        if (renameMap is null)
        {
            return clean;
        }

        foreach (KeyValuePair<string, string> item in renameMap)
        {
            string oldName = item.Key.Trim();
            string newName = item.Value.Trim();
            if (!string.IsNullOrWhiteSpace(oldName) && !string.IsNullOrWhiteSpace(newName))
            {
                clean[oldName] = newName;
            }
        }

        return clean;
    }

    private static string BuildUniqueImportedDeviceName(string originalName, ISet<string> usedNames)
    {
        string baseName = string.IsNullOrWhiteSpace(originalName) ? "Imported device" : originalName.Trim();
        string candidate = baseName + " (import)";
        int index = 2;
        while (usedNames.Contains(candidate))
        {
            candidate = $"{baseName} (import {index})";
            index++;
        }

        return candidate;
    }

    private static void RenameDeviceElement(XElement deviceElement, string newName)
    {
        SetElementValue(deviceElement, "name", newName.Trim());
        SetElementValue(deviceElement, "friendly_name", newName.Trim());
    }

    private static void UpdateImportedSubscriptionDeviceNames(XElement importedDeviceElement, IReadOnlyDictionary<string, string> renamedDevices)
    {
        if (renamedDevices.Count == 0)
        {
            return;
        }

        foreach (XElement rxChannel in importedDeviceElement.Elements("rxchannel"))
        {
            XElement? subscribedDevice = FindFirstElement(rxChannel, SubscriptionDeviceElementNames);
            if (subscribedDevice is not null && renamedDevices.TryGetValue(subscribedDevice.Value.Trim(), out string? newDeviceName))
            {
                subscribedDevice.Value = newDeviceName;
            }
        }
    }

    private static bool SetDeviceIpAddressesDynamic(DanteDevice device)
    {
        XElement[] ipv4Addresses = device.Element.Descendants("ipv4_address").ToArray();
        if (ipv4Addresses.Length == 0)
        {
            return false;
        }

        bool changed = false;
        foreach (XElement ipv4Address in ipv4Addresses)
        {
            XAttribute? modeAttribute = ipv4Address.Attribute("mode");
            if (!string.Equals(modeAttribute?.Value, "dynamic", StringComparison.OrdinalIgnoreCase))
            {
                ipv4Address.SetAttributeValue("mode", "dynamic");
                changed = true;
            }

            foreach (string attributeName in StaticIpAttributeNames)
            {
                XAttribute? attribute = ipv4Address.Attribute(attributeName);
                if (attribute is not null)
                {
                    attribute.Remove();
                    changed = true;
                }
            }

            foreach (string elementName in StaticIpElementNames)
            {
                XElement? element = ipv4Address.Element(elementName);
                if (element is not null)
                {
                    element.Remove();
                    changed = true;
                }
            }

            if (!ipv4Address.HasElements && !string.IsNullOrWhiteSpace(ipv4Address.Value))
            {
                ipv4Address.Value = string.Empty;
                changed = true;
            }
        }

        return changed;
    }

    private static void SetDeviceIpAddressStatic(DanteDevice device, string address, string netmask, string gateway)
    {
        XElement ipv4Address = FindOrCreateIpv4AddressElement(device.Element);
        ipv4Address.SetAttributeValue("mode", "static");
        foreach (string attributeName in StaticIpAttributeNames)
        {
            ipv4Address.Attribute(attributeName)?.Remove();
        }

        SetElementValue(ipv4Address, "address", address);
        SetElementValue(ipv4Address, "netmask", netmask);
        SetElementValue(ipv4Address, "gateway", gateway);
        SetElementValue(ipv4Address, "dnsserver", "0.0.0.0");
    }

    private static XElement FindOrCreateIpv4AddressElement(XElement deviceElement)
    {
        XElement? ipv4Address = deviceElement.Descendants("ipv4_address").FirstOrDefault();
        if (ipv4Address is not null)
        {
            return ipv4Address;
        }

        XElement? interfaceElement = deviceElement.Element("interface");
        if (interfaceElement is null)
        {
            throw new InvalidOperationException($"La machine {deviceElement.Element("name")?.Value.Trim()} ne contient pas de balise <interface> IPv4 modifiable.");
        }

        ipv4Address = new XElement("ipv4_address");
        interfaceElement.Add(ipv4Address);
        return ipv4Address;
    }

    private static bool DeviceSupportsIpConfiguration(DanteDevice device)
    {
        return device.Element.Descendants("ipv4_address").Any()
            || device.Element.Element("interface") is not null;
    }

    private static bool DeviceHasStaticIpConfiguration(DanteDevice device)
    {
        if (device.UsesStaticIp)
        {
            return true;
        }

        return device.Element.Descendants("ipv4_address").Any(ipv4Address =>
            !string.Equals(ipv4Address.Attribute("mode")?.Value, "dynamic", StringComparison.OrdinalIgnoreCase)
            || StaticIpAttributeNames.Any(attributeName => ipv4Address.Attribute(attributeName) is not null)
            || StaticIpElementNames.Any(elementName => ipv4Address.Element(elementName) is not null)
            || !string.IsNullOrWhiteSpace(ipv4Address.Value));
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

    private static void SetSubscriptionElements(XElement rxElement, string txDeviceName, string txChannelName)
    {
        XElement? channelElement = FindFirstElement(rxElement, SubscriptionChannelElementNames);
        XElement? deviceElement = FindFirstElement(rxElement, SubscriptionDeviceElementNames);

        if (channelElement is null)
        {
            channelElement = new XElement(SubscriptionChannelElementNames[0], txChannelName);
            XElement? nameElement = rxElement.Element("name");
            if (nameElement is not null)
            {
                nameElement.AddAfterSelf(channelElement);
            }
            else
            {
                rxElement.AddFirst(channelElement);
            }
        }
        else
        {
            channelElement.Value = txChannelName;
        }

        if (deviceElement is null)
        {
            deviceElement = new XElement(SubscriptionDeviceElementNames[0], txDeviceName);
            channelElement.AddAfterSelf(deviceElement);
        }
        else
        {
            deviceElement.Value = txDeviceName;
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
            string rxDeviceName = rxChannel.Parent?.Element("name")?.Value.Trim() ?? string.Empty;
            bool sameDevice = rxChannel.Elements()
                .Where(element => SubscriptionDeviceElementNames.Contains(element.Name.LocalName))
                .Any(element =>
                {
                    string subscribedDevice = element.Value.Trim();
                    return string.Equals(subscribedDevice, txDeviceName, StringComparison.OrdinalIgnoreCase)
                        || (string.Equals(subscribedDevice, ".", StringComparison.Ordinal) && string.Equals(rxDeviceName, txDeviceName, StringComparison.OrdinalIgnoreCase));
                });

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

    private static string ValidateSamplerate(string samplerate)
    {
        string cleanSamplerate = samplerate.Trim();
        if (!SupportedSamplerates.Contains(cleanSamplerate))
        {
            throw new InvalidOperationException("Sample rate non reconnue. Valeurs autorisées : 44100, 48000, 88200, 96000, 176400 ou 192000.");
        }

        return cleanSamplerate;
    }

    private static string ValidateEncoding(string encoding)
    {
        string cleanEncoding = encoding.Trim();
        if (!SupportedEncodings.Contains(cleanEncoding))
        {
            throw new InvalidOperationException("Bits par échantillon non reconnus. Valeurs autorisées : 16, 24 ou 32.");
        }

        return cleanEncoding;
    }

    private static string ValidateIpv4Address(string value, string label)
    {
        string cleanValue = value.Trim();
        if (!IPAddress.TryParse(cleanValue, out IPAddress? address)
            || address.AddressFamily is not System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"Le champ {label} doit être une adresse IPv4 valide.");
        }

        return address.ToString();
    }

    private static string ValidateIpv4Prefix(string value)
    {
        string cleanValue = value.Trim();
        string[] parts = cleanValue.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Le préfixe IP doit contenir les trois premiers octets, par exemple 192.168.1.");
        }

        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int octet) || octet < 0 || octet > 255)
            {
                throw new InvalidOperationException("Le préfixe IP contient un octet invalide.");
            }
        }

        return string.Join(".", parts.Select(part => int.Parse(part).ToString()));
    }

    private static string FormatSamplerateForDisplay(string samplerate)
    {
        if (!int.TryParse(samplerate, out int value) || value <= 0)
        {
            return Blank(samplerate);
        }

        decimal khz = value / 1000m;
        return $"{khz:0.#} kHz ({value})";
    }

    private static string FormatEncodingForDisplay(string encoding)
    {
        return string.IsNullOrWhiteSpace(encoding) ? Blank(encoding) : $"{encoding} bit";
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

    private bool ShouldUseLocalSubscriptionMarker(XElement rxElement, string rxDeviceName, string txDeviceName)
    {
        if (!string.Equals(rxDeviceName, txDeviceName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string currentRawDevice = FindFirstElement(rxElement, SubscriptionDeviceElementNames)?.Value.Trim() ?? string.Empty;
        return string.Equals(currentRawDevice, ".", StringComparison.Ordinal)
            || Document.Root!.Elements("device").Elements("rxchannel")
                .Select(channel => FindFirstElement(channel, SubscriptionDeviceElementNames)?.Value.Trim() ?? string.Empty)
                .Any(value => string.Equals(value, ".", StringComparison.Ordinal));
    }

    private static string FormatDisplayTxDevice(string rawTxDeviceName, string resolvedTxDeviceName)
    {
        if (string.IsNullOrWhiteSpace(rawTxDeviceName))
        {
            return string.Empty;
        }

        return string.Equals(rawTxDeviceName, ".", StringComparison.Ordinal)
            ? $"LOCAL / {resolvedTxDeviceName}"
            : resolvedTxDeviceName;
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
