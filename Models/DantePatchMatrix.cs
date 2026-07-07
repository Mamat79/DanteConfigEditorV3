namespace DanteConfigEditor.Models;

public sealed class DantePatchMatrix
{
    public DantePatchMatrix(IReadOnlyList<DanteSubscription> subscriptions)
    {
        Subscriptions = subscriptions;
    }

    public IReadOnlyList<DanteSubscription> Subscriptions { get; }

    public int ActivePatchCount => Subscriptions.Count(subscription => subscription.IsActive);

    public int FreeRxCount => Subscriptions.Count(subscription => !subscription.IsActive);

    public int LocalPatchCount => Subscriptions.Count(subscription => subscription.IsLocalSubscription);

    public int ExternalMissingDeviceCount => Subscriptions.Count(subscription => subscription.IsExternalMissingDevice);

    public int MissingTxChannelCount => Subscriptions.Count(subscription => subscription.IsTxChannelMissing);

    public int WarningCount => Subscriptions.Count(subscription => subscription.IsWarning);

    public int ModifiedCount => Subscriptions.Count(subscription => subscription.IsModified);

    public int ConflictCount => Subscriptions.Count(subscription => subscription.IsConflict);
}
