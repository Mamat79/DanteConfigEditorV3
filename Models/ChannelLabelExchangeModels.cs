namespace DanteConfigEditor.Models;

public enum ChannelLabelDirection
{
    Tx,
    Rx,
    ConsoleInput
}

public sealed record ChannelLabelEntry(int ChannelNumber, string Label, int? DanteId = null);

public sealed record ChannelLabelSet
{
    [System.Text.Json.Serialization.JsonConstructor]
    public ChannelLabelSet(string deviceName, ChannelLabelDirection direction, IReadOnlyList<ChannelLabelEntry> channels)
    {
        DeviceName = deviceName?.Trim() ?? string.Empty;
        Direction = direction;
        Channels = channels?.ToArray() ?? throw new ArgumentNullException(nameof(channels));
    }

    public string DeviceName { get; }

    public ChannelLabelDirection Direction { get; }

    public IReadOnlyList<ChannelLabelEntry> Channels { get; }

    public string DisplayName => $"{DeviceName} - {Direction} ({Channels.Count})";
}

public sealed record ChannelLabelDocument
{
    [System.Text.Json.Serialization.JsonConstructor]
    public ChannelLabelDocument(
        string format,
        int schemaVersion,
        string sourceApplication,
        string? sourceVersion,
        IReadOnlyList<ChannelLabelSet> sets)
    {
        Format = format;
        SchemaVersion = schemaVersion;
        SourceApplication = sourceApplication;
        SourceVersion = sourceVersion;
        Sets = sets?.ToArray() ?? throw new ArgumentNullException(nameof(sets));
    }

    public string Format { get; }

    public int SchemaVersion { get; }

    public string SourceApplication { get; }

    public string? SourceVersion { get; }

    public IReadOnlyList<ChannelLabelSet> Sets { get; }
}

public sealed record ChannelLabelAssignment(
    string DeviceName,
    DanteChannelKind Kind,
    int DanteId,
    string Label);

public enum ChannelLabelTransferStatus
{
    Ready,
    Unchanged,
    MissingSource,
    MissingTarget,
    EmptyLabel
}

public sealed record ChannelLabelTransferPreviewRow(
    string SourceDevice,
    int SourceChannel,
    string TargetDevice,
    DanteChannelKind TargetKind,
    int TargetDanteId,
    string CurrentLabel,
    string NewLabel,
    ChannelLabelTransferStatus Status)
{
    public bool CanApply => Status is ChannelLabelTransferStatus.Ready or ChannelLabelTransferStatus.Unchanged;

    public bool WillChange => Status == ChannelLabelTransferStatus.Ready;

    public string StatusDisplay => Status switch
    {
        ChannelLabelTransferStatus.Ready => "Prêt",
        ChannelLabelTransferStatus.Unchanged => "Inchangé",
        ChannelLabelTransferStatus.MissingSource => "Source absente",
        ChannelLabelTransferStatus.MissingTarget => "Canal cible absent",
        ChannelLabelTransferStatus.EmptyLabel => "Label vide ignoré",
        _ => Status.ToString()
    };

    public string StatusDisplayEnglish => Status switch
    {
        ChannelLabelTransferStatus.Ready => "Ready",
        ChannelLabelTransferStatus.Unchanged => "Unchanged",
        ChannelLabelTransferStatus.MissingSource => "Missing source",
        ChannelLabelTransferStatus.MissingTarget => "Missing target channel",
        ChannelLabelTransferStatus.EmptyLabel => "Empty label ignored",
        _ => Status.ToString()
    };
}

public sealed record DmtLabelCompatibility(bool IsCompatible, string AdaptedLabel, IReadOnlyList<string> Warnings);

public sealed record DmtWorkbookReadResult(string TemplateVersion, ChannelLabelDocument Document);
