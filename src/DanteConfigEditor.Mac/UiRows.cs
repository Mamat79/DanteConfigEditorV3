using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DanteConfigEditor.Mac;

internal sealed record DeviceRow(
    string Name,
    string FriendlyName,
    string Network,
    string Latency,
    string SampleRate,
    string Bits,
    string Ip,
    bool PreferredMaster,
    int Tx,
    int Rx);

internal sealed record PatchRow(
    string RxDevice,
    int RxIndex,
    int RxDanteId,
    string RxChannel,
    string TxDevice,
    string TxChannel,
    string Source,
    string Type,
    string Status);

internal sealed record HealthRow(
    string Severity,
    string Category,
    string Device,
    string Channel,
    string Message);

internal sealed record ChoiceValue(string Value, string Display)
{
    public override string ToString() => Display;
}

internal sealed class EditableChannelRow : INotifyPropertyChanged
{
    private string _name;

    public EditableChannelRow(int index, int danteId, string name)
    {
        Index = index;
        DanteId = danteId;
        OriginalName = name;
        _name = name;
    }

    public int Index { get; }

    public int DanteId { get; }

    public string OriginalName { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public bool IsChanged => !string.Equals(OriginalName, Name, StringComparison.Ordinal);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed class DuplicateRenameRow : INotifyPropertyChanged
{
    private string _newName;

    public DuplicateRenameRow(string originalName, string newName)
    {
        OriginalName = originalName;
        _newName = newName;
    }

    public string OriginalName { get; }

    public string NewName
    {
        get => _newName;
        set
        {
            if (string.Equals(_newName, value, StringComparison.Ordinal))
            {
                return;
            }

            _newName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewName)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
