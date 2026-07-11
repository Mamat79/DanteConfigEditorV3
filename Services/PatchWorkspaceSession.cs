using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public sealed record EffectivePatchAssignment(
    string RxDeviceName,
    int RxDanteId,
    string? TxDeviceName,
    string? TxChannelName,
    bool IsPending)
{
    public bool IsActive => !string.IsNullOrWhiteSpace(TxDeviceName);
}

public sealed class PatchWorkspaceSession
{
    private readonly Dictionary<PatchTargetKey, PatchSourceValue> _originalAssignments;
    private readonly Dictionary<PatchTargetKey, PatchEditRequest> _pendingEdits = new();

    public PatchWorkspaceSession(IEnumerable<DanteSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        _originalAssignments = subscriptions.ToDictionary(
            subscription => PatchTargetKey.Create(subscription.RxDevice, subscription.RxDanteId),
            subscription => OriginalSource(subscription));
    }

    public bool HasChanges => _pendingEdits.Count > 0;

    public int PendingCount => _pendingEdits.Count;

    public IReadOnlyList<PatchEditRequest> Edits => _pendingEdits.Values
        .OrderBy(edit => edit.RxDeviceName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(edit => edit.RxDanteId)
        .ToArray();

    public EffectivePatchAssignment GetEffectiveAssignment(PatchTargetDescriptor target)
    {
        ArgumentNullException.ThrowIfNull(target);
        PatchTargetKey key = RequireKnownTarget(target);

        if (_pendingEdits.TryGetValue(key, out PatchEditRequest? pending))
        {
            return new EffectivePatchAssignment(
                target.DeviceName,
                target.DanteId,
                pending.TxDeviceName,
                pending.TxChannelName,
                IsPending: true);
        }

        PatchSourceValue original = _originalAssignments[key];
        return new EffectivePatchAssignment(
            target.DeviceName,
            target.DanteId,
            original.DeviceName,
            original.ChannelName,
            IsPending: false);
    }

    public void Assign(PlannedPatchAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        SetDesiredSource(
            assignment.Target,
            assignment.Source.DeviceName,
            assignment.Source.ChannelName);
    }

    public SequentialPatchPlan AssignSequential(
        IReadOnlyList<PatchSourceDescriptor> selectedSources,
        IReadOnlyList<PatchTargetDescriptor> availableTargets,
        PatchTargetDescriptor firstTarget)
    {
        SequentialPatchPlan plan = PatchAssignmentPlanner.PlanSequential(
            selectedSources,
            availableTargets,
            firstTarget);

        foreach (PlannedPatchAssignment assignment in plan.Assignments)
        {
            Assign(assignment);
        }

        return plan;
    }

    public void Remove(PatchTargetDescriptor target)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetDesiredSource(target, null, null);
    }

    public void Reset()
    {
        _pendingEdits.Clear();
    }

    private void SetDesiredSource(PatchTargetDescriptor target, string? txDeviceName, string? txChannelName)
    {
        PatchTargetKey key = RequireKnownTarget(target);
        PatchSourceValue original = _originalAssignments[key];
        PatchSourceValue desired = new(NormalizeOptional(txDeviceName), NormalizeOptional(txChannelName));

        // Revenir exactement à l'affectation d'origine retire l'édition en
        // attente au lieu de produire une écriture XML inutile.
        if (SameSource(original, desired))
        {
            _pendingEdits.Remove(key);
            return;
        }

        _pendingEdits[key] = string.IsNullOrWhiteSpace(desired.DeviceName)
            ? PatchEditRequest.Remove(target)
            : new PatchEditRequest(
                target.DeviceName,
                target.DanteId,
                desired.DeviceName,
                desired.ChannelName);
    }

    private PatchTargetKey RequireKnownTarget(PatchTargetDescriptor target)
    {
        PatchTargetKey key = PatchTargetKey.Create(target.DeviceName, target.DanteId);
        if (!_originalAssignments.ContainsKey(key))
        {
            throw new InvalidOperationException("Le canal RX sélectionné n'existe pas dans le projet chargé.");
        }

        return key;
    }

    private static PatchSourceValue OriginalSource(DanteSubscription subscription)
    {
        if (!subscription.IsActive)
        {
            return new PatchSourceValue(null, null);
        }

        string? deviceName = subscription.IsLocalSubscription
            ? subscription.RxDevice
            : FirstNonEmpty(subscription.ResolvedTxDeviceName, subscription.RawTxDeviceName);

        return new PatchSourceValue(
            NormalizeOptional(deviceName),
            NormalizeOptional(subscription.TxChannelName));
    }

    private static bool SameSource(PatchSourceValue left, PatchSourceValue right)
    {
        return string.Equals(left.DeviceName, right.DeviceName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.ChannelName, right.ChannelName, StringComparison.Ordinal);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private readonly record struct PatchTargetKey(string NormalizedDeviceName, int DanteId)
    {
        public static PatchTargetKey Create(string deviceName, int danteId)
        {
            return new PatchTargetKey(deviceName.Trim().ToUpperInvariant(), danteId);
        }
    }

    private readonly record struct PatchSourceValue(string? DeviceName, string? ChannelName);
}
