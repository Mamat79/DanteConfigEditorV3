namespace DanteConfigEditor.Models;

public sealed class DantePatchMatrix
{
    public DantePatchMatrix(IReadOnlyList<DanteSubscription> subscriptions)
    {
        Subscriptions = subscriptions;
    }

    public IReadOnlyList<DanteSubscription> Subscriptions { get; }

    public int ActivePatchCount => Subscriptions.Count(subscription => subscription.IsActive);

    public int ConflictCount => Subscriptions.Count(subscription => subscription.Status.StartsWith("Conflit", StringComparison.OrdinalIgnoreCase));
}
