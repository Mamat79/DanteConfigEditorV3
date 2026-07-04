using System.Text;
using System.Xml;
using System.Xml.Linq;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed class DanteProject
{
    private static readonly string[] SubscriptionDeviceElementNames =
    [
        "subscribed_device",
        "subscription_device",
        "tx_device",
        "source_device"
    ];

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

    private readonly Dictionary<XElement, bool> _modifiedRxElements = [];
    private readonly List<ChangeRecord> _changes = [];

    private DanteProject(string originalFilePath, XDocument document)
    {
        OriginalFilePath = originalFilePath;
        Document = document;
        ReloadModel();
    }

    public string OriginalFilePath { get; }

    public string? LastSavedPath { get; private set; }

    public XDocument Document { get; }

    public IReadOnlyList<DanteDevice> Devices { get; private set; } = [];

    public DantePatchMatrix PatchMatrix { get; private set; } = new([]);

    public IReadOnlyList<ChangeRecord> Changes => _changes;

    public bool IsModified { get; private set; }

    public static DanteProject Load(string path)
    {
        XDocument document;
        try
        {
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
            SetElementValue(channel.Element, "label", trimmedNewName);
            UpdateSubscriptionsForRenamedTxChannel(device.Name, oldName, trimmedNewName);
        }
        else
        {
            SetElementValue(channel.Element, "name", trimmedNewName);
        }

        RegisterChange("Nom canal", $"{deviceName} {channelKind} {channelIndex}: {oldName} -> {trimmedNewName}");
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

    public string BuildSaveSummary()
    {
        StringBuilder builder = new();
        DanteValidationResult validation = Validate();

        builder.AppendLine($"Fichier original : {OriginalFilePath}");
        builder.AppendLine($"Dernier fichier sauvegardé : {LastSavedPath ?? "aucun"}");
        builder.AppendLine($"Devices : {Devices.Count}");
        builder.AppendLine($"Canaux TX : {Devices.Sum(device => device.TxCount)}");
        builder.AppendLine($"Canaux RX : {Devices.Sum(device => device.RxCount)}");
        builder.AppendLine($"Patchs actifs : {PatchMatrix.ActivePatchCount}");
        builder.AppendLine($"Patchs modifiés : {_modifiedRxElements.Count}");
        builder.AppendLine();
        builder.AppendLine(validation.ToDisplayText());
        builder.AppendLine();
        builder.AppendLine("Historique des modifications :");

        if (_changes.Count == 0)
        {
            builder.AppendLine("- Aucune modification depuis le chargement.");
        }
        else
        {
            foreach (ChangeRecord change in _changes)
            {
                builder.AppendLine("- " + change.Display);
            }
        }

        return builder.ToString();
    }

    public string SaveAs(string destinationPath)
    {
        DanteValidationResult validation = Validate();
        if (validation.HasErrors)
        {
            throw new InvalidOperationException("Sauvegarde impossible tant que des erreurs bloquantes existent." + Environment.NewLine + validation.ToDisplayText());
        }

        string backupPath = SafeFileService.CreateOriginalBackup(OriginalFilePath);
        Document.Save(destinationPath);
        LastSavedPath = destinationPath;
        IsModified = false;
        RegisterChange("Sauvegarde", $"Fichier sauvegardé sous {destinationPath}");
        IsModified = false;
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

    private void ReloadModel()
    {
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

    private static void ResetDeviceChannels(DanteDevice device)
    {
        int index = 1;
        foreach (DanteChannel channel in device.TxChannels)
        {
            SetElementValue(channel.Element, "label", index.ToString());
            index++;
        }

        index = 1;
        foreach (DanteChannel channel in device.RxChannels)
        {
            SetElementValue(channel.Element, "name", index.ToString());
            index++;
        }
    }

    private void UpdateSubscriptionsForRenamedTxChannel(string txDeviceName, string oldChannelName, string newChannelName)
    {
        if (string.IsNullOrWhiteSpace(oldChannelName))
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
                bool sameChannel = string.Equals(subscribedChannel.Value.Trim(), oldChannelName, StringComparison.OrdinalIgnoreCase);
                if (sameChannel)
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
}
