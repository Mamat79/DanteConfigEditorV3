using System.IO;
using System.Xml.Linq;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed partial class DanteProject
{
    public IReadOnlyList<string> FindDuplicateDeviceNamesInXml(string path)
    {
        DanteProject importedProject = Load(path);
        return importedProject.Devices
            .Select(device => device.Name)
            .Where(name => Devices.Any(existing => string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyDictionary<string, string> BuildAutomaticDuplicateRenameMap(string path, string suffix = "Import")
    {
        DanteProject importedProject = Load(path);
        string cleanSuffix = NormalizeImportSuffix(suffix);
        HashSet<string> usedNames = Devices.Select(device => device.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> renameMap = new(StringComparer.OrdinalIgnoreCase);

        foreach (DanteDevice device in importedProject.Devices)
        {
            if (!usedNames.Contains(device.Name))
            {
                usedNames.Add(device.Name);
                continue;
            }

            string newName = BuildUniqueImportedDeviceName(device.Name, cleanSuffix, usedNames);
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

    private static string NormalizeImportSuffix(string suffix)
    {
        string clean = (suffix ?? string.Empty).Trim().Trim('(', ')').Trim();
        clean = string.Join("-", clean.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw new InvalidOperationException("Le suffixe de renommage ne peut pas être vide.");
        }
        if (ContainsProblematicCharacters(clean))
        {
            throw new InvalidOperationException("Le suffixe de renommage contient des caractères non imprimables.");
        }
        return clean;
    }

    private static string BuildUniqueImportedDeviceName(string originalName, string suffix, ISet<string> usedNames)
    {
        string baseName = string.IsNullOrWhiteSpace(originalName) ? "Imported device" : originalName.Trim();
        string candidate = $"{baseName}-{suffix}";
        int index = 2;
        while (usedNames.Contains(candidate))
        {
            candidate = $"{baseName}-{suffix}-{index}";
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

        foreach (XElement rxChannel in importedDeviceElement.Children("rxchannel"))
        {
            XElement? subscribedDevice = FindFirstElement(rxChannel, SubscriptionDeviceElementNames);
            if (subscribedDevice is not null && renamedDevices.TryGetValue(subscribedDevice.Value.Trim(), out string? newDeviceName))
            {
                subscribedDevice.Value = newDeviceName;
            }
        }
    }
}
