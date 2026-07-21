using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public static class ChannelLabelTransferPlanner
{
    public static IReadOnlyList<ChannelLabelTransferPreviewRow> BuildPreview(
        DanteProject project,
        ChannelLabelSet source,
        IEnumerable<string> targetDeviceNames,
        DanteChannelKind targetKind,
        int sourceStartChannel,
        int targetStartChannel,
        int count)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(source);
        if (sourceStartChannel <= 0 || targetStartChannel <= 0 || count <= 0)
        {
            throw new InvalidOperationException("Les premiers canaux et le nombre de canaux doivent être supérieurs à zéro.");
        }

        string[] targetNames = (targetDeviceNames ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (targetNames.Length == 0)
        {
            throw new InvalidOperationException("Sélectionnez au moins une machine cible.");
        }

        Dictionary<int, ChannelLabelEntry> sourceByNumber = source.Channels
            .GroupBy(channel => channel.ChannelNumber)
            .ToDictionary(group => group.Key, group => group.First());
        List<ChannelLabelTransferPreviewRow> rows = [];

        foreach (string targetName in targetNames)
        {
            DanteDevice? device = project.FindDevice(targetName);
            IReadOnlyList<DanteChannel> targetChannels = device is null
                ? []
                : targetKind == DanteChannelKind.Tx ? device.TxChannels : device.RxChannels;
            Dictionary<int, DanteChannel> targetsById = targetChannels
                .GroupBy(channel => channel.DanteId)
                .ToDictionary(group => group.Key, group => group.First());

            for (int offset = 0; offset < count; offset++)
            {
                int sourceNumber = sourceStartChannel + offset;
                int targetId = targetStartChannel + offset;
                sourceByNumber.TryGetValue(sourceNumber, out ChannelLabelEntry? sourceChannel);
                targetsById.TryGetValue(targetId, out DanteChannel? targetChannel);
                string newLabel = sourceChannel?.Label.Trim() ?? string.Empty;
                ChannelLabelTransferStatus status = sourceChannel is null
                    ? ChannelLabelTransferStatus.MissingSource
                    : targetChannel is null
                        ? ChannelLabelTransferStatus.MissingTarget
                        : string.IsNullOrWhiteSpace(newLabel)
                            ? ChannelLabelTransferStatus.EmptyLabel
                            : string.Equals(targetChannel.DisplayName, newLabel, StringComparison.Ordinal)
                                ? ChannelLabelTransferStatus.Unchanged
                                : ChannelLabelTransferStatus.Ready;

                rows.Add(new ChannelLabelTransferPreviewRow(
                    source.DeviceName,
                    sourceNumber,
                    targetName,
                    targetKind,
                    targetId,
                    targetChannel?.DisplayName ?? string.Empty,
                    newLabel,
                    status));
            }
        }

        return rows;
    }

    public static IReadOnlyList<ChannelLabelAssignment> BuildAssignments(IEnumerable<ChannelLabelTransferPreviewRow> preview)
    {
        ChannelLabelTransferPreviewRow[] rows = (preview ?? []).ToArray();
        ChannelLabelTransferPreviewRow[] errors = rows.Where(row => !row.CanApply).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException($"Le transfert contient {errors.Length} ligne(s) non applicable(s). Corrigez la plage avant de continuer.");
        }

        return rows
            .Where(row => row.WillChange)
            .Select(row => new ChannelLabelAssignment(row.TargetDevice, row.TargetKind, row.TargetDanteId, row.NewLabel))
            .ToArray();
    }
}
