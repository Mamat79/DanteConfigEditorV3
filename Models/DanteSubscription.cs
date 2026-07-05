using System.Xml.Linq;

namespace DanteConfigEditor.Models;

public sealed class DanteSubscription
{
    internal DanteSubscription(
        string rxDevice,
        int rxIndex,
        string rxChannelName,
        XElement rxElement,
        string txDevice,
        string txChannelName,
        bool isModified,
        string status)
    {
        RxDevice = rxDevice;
        RxIndex = rxIndex;
        RxChannelName = rxChannelName;
        RxElement = rxElement;
        TxDevice = txDevice;
        TxChannelName = txChannelName;
        IsModified = isModified;
        Status = status;
    }

    public string RxDevice { get; }

    public int RxIndex { get; }

    public string RxChannelName { get; }

    public string TxDevice { get; }

    public string TxChannelName { get; }

    public bool IsActive => !string.IsNullOrWhiteSpace(TxDevice);

    public bool IsModified { get; }

    public bool IsConflict => Status.StartsWith("Conflit", StringComparison.OrdinalIgnoreCase);

    public string RowState => IsConflict ? "Conflict" : IsModified ? "Modified" : IsActive ? "Active" : "Free";

    public string Status { get; }

    public string Display => $"{RxDevice} / {RxChannelName}";

    internal XElement RxElement { get; }
}
