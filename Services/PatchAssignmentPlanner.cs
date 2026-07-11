namespace DanteConfigEditor.Services;

public sealed record PatchSourceDescriptor(
    string DeviceName,
    int DanteId,
    int PositionIndex,
    string ChannelName)
{
    public string Display => $"{DanteId:000} - {ChannelName}";

    public string FullDisplay => $"{DeviceName} / {Display}";
}

public sealed record PatchTargetDescriptor(
    string DeviceName,
    int DanteId,
    int PositionIndex,
    string ChannelName)
{
    public string Display => $"{DanteId:000} - {ChannelName}";

    public string FullDisplay => $"{DeviceName} / {Display}";
}

public sealed record PlannedPatchAssignment(
    PatchSourceDescriptor Source,
    PatchTargetDescriptor Target);

public sealed record SequentialPatchPlan(
    IReadOnlyList<PlannedPatchAssignment> Assignments,
    IReadOnlyList<PatchSourceDescriptor> UnassignedSources);

public sealed record PatchEditRequest(
    string RxDeviceName,
    int RxDanteId,
    string? TxDeviceName,
    string? TxChannelName)
{
    public bool IsRemoval => string.IsNullOrWhiteSpace(TxDeviceName);

    public static PatchEditRequest Apply(PlannedPatchAssignment assignment)
    {
        return new PatchEditRequest(
            assignment.Target.DeviceName,
            assignment.Target.DanteId,
            assignment.Source.DeviceName,
            assignment.Source.ChannelName);
    }

    public static PatchEditRequest Remove(PatchTargetDescriptor target)
    {
        return new PatchEditRequest(target.DeviceName, target.DanteId, null, null);
    }
}

public static class PatchAssignmentPlanner
{
    public static SequentialPatchPlan PlanSequential(
        IReadOnlyList<PatchSourceDescriptor> selectedSources,
        IReadOnlyList<PatchTargetDescriptor> availableTargets,
        PatchTargetDescriptor firstTarget)
    {
        ArgumentNullException.ThrowIfNull(selectedSources);
        ArgumentNullException.ThrowIfNull(availableTargets);
        ArgumentNullException.ThrowIfNull(firstTarget);

        if (selectedSources.Count == 0)
        {
            throw new InvalidOperationException("Sélectionnez au moins un canal TX.");
        }

        // Le danteId n'est pas forcément contigu. L'ordre XML, conservé dans
        // PositionIndex, détermine donc les RX qui suivent réellement.
        PatchTargetDescriptor[] followingTargets = availableTargets
            .Where(target => string.Equals(target.DeviceName, firstTarget.DeviceName, StringComparison.OrdinalIgnoreCase))
            .Where(target => target.PositionIndex >= firstTarget.PositionIndex)
            .OrderBy(target => target.PositionIndex)
            .ToArray();

        if (followingTargets.Length == 0
            || !followingTargets.Any(target => target.DanteId == firstTarget.DanteId))
        {
            throw new InvalidOperationException("Le canal RX de départ n'appartient pas à la liste disponible.");
        }

        int assignmentCount = Math.Min(selectedSources.Count, followingTargets.Length);
        List<PlannedPatchAssignment> assignments = new(assignmentCount);
        for (int index = 0; index < assignmentCount; index++)
        {
            assignments.Add(new PlannedPatchAssignment(selectedSources[index], followingTargets[index]));
        }

        return new SequentialPatchPlan(
            assignments,
            selectedSources.Skip(assignmentCount).ToArray());
    }
}
