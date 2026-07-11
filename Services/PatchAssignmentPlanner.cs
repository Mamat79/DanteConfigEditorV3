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

public sealed record PatchAssignmentPlan(
    IReadOnlyList<PlannedPatchAssignment> Assignments);

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
    public static PatchAssignmentPlan PlanSelection(
        IReadOnlyList<PatchSourceDescriptor> selectedSources,
        IReadOnlyList<PatchTargetDescriptor> selectedTargets)
    {
        ArgumentNullException.ThrowIfNull(selectedSources);
        ArgumentNullException.ThrowIfNull(selectedTargets);

        if (selectedSources.Count == 0)
        {
            throw new InvalidOperationException("Sélectionnez au moins un canal TX.");
        }

        if (selectedTargets.Count == 0)
        {
            throw new InvalidOperationException("Sélectionnez au moins un canal RX.");
        }

        EnsureUniqueTargets(selectedTargets);

        if (selectedSources.Count > 1 && selectedTargets.Count == 1)
        {
            throw new InvalidOperationException("Plusieurs TX ne peuvent pas être affectés à un seul RX.");
        }

        if (selectedSources.Count != 1 && selectedSources.Count != selectedTargets.Count)
        {
            throw new InvalidOperationException("Sélectionnez autant de TX que de RX, ou un seul TX pour plusieurs RX.");
        }

        PlannedPatchAssignment[] assignments = selectedSources.Count == 1
            ? selectedTargets.Select(target => new PlannedPatchAssignment(selectedSources[0], target)).ToArray()
            : selectedSources.Zip(selectedTargets, (source, target) => new PlannedPatchAssignment(source, target)).ToArray();

        return new PatchAssignmentPlan(assignments);
    }

    public static PatchAssignmentPlan PlanRange(
        IReadOnlyList<PatchSourceDescriptor> availableSources,
        PatchSourceDescriptor firstSource,
        IReadOnlyList<PatchTargetDescriptor> availableTargets,
        PatchTargetDescriptor firstTarget,
        int count)
    {
        ArgumentNullException.ThrowIfNull(availableSources);
        ArgumentNullException.ThrowIfNull(firstSource);
        ArgumentNullException.ThrowIfNull(availableTargets);
        ArgumentNullException.ThrowIfNull(firstTarget);

        if (count <= 0)
        {
            throw new InvalidOperationException("Le nombre de canaux doit être supérieur à zéro.");
        }

        PatchSourceDescriptor knownFirstSource = FindKnownSource(availableSources, firstSource)
            ?? throw new InvalidOperationException("Le canal TX de départ n'appartient pas à la liste disponible.");
        PatchTargetDescriptor knownFirstTarget = FindKnownTarget(availableTargets, firstTarget)
            ?? throw new InvalidOperationException("Le canal RX de départ n'appartient pas à la liste disponible.");

        PatchSourceDescriptor[] sources = availableSources
            .Where(source => string.Equals(source.DeviceName, knownFirstSource.DeviceName, StringComparison.OrdinalIgnoreCase))
            .Where(source => source.PositionIndex >= knownFirstSource.PositionIndex)
            .OrderBy(source => source.PositionIndex)
            .Take(count)
            .ToArray();
        PatchTargetDescriptor[] targets = availableTargets
            .Where(target => string.Equals(target.DeviceName, knownFirstTarget.DeviceName, StringComparison.OrdinalIgnoreCase))
            .Where(target => target.PositionIndex >= knownFirstTarget.PositionIndex)
            .OrderBy(target => target.PositionIndex)
            .Take(count)
            .ToArray();

        if (sources.Length != count || targets.Length != count)
        {
            throw new InvalidOperationException("La plage demandée dépasse les canaux TX ou RX disponibles. Aucun patch n'a été préparé.");
        }

        return new PatchAssignmentPlan(
            sources.Zip(targets, (source, target) => new PlannedPatchAssignment(source, target)).ToArray());
    }

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

        PatchTargetDescriptor? knownFirstTarget = availableTargets.FirstOrDefault(target =>
            string.Equals(target.DeviceName, firstTarget.DeviceName, StringComparison.OrdinalIgnoreCase)
            && target.DanteId == firstTarget.DanteId);
        if (knownFirstTarget is null)
        {
            throw new InvalidOperationException("Le canal RX de départ n'appartient pas à la liste disponible.");
        }

        // Le danteId n'est pas forcément contigu. L'ordre XML, conservé dans
        // PositionIndex, détermine donc les RX qui suivent réellement. La
        // position est reprise de la liste connue, jamais d'un objet appelant.
        PatchTargetDescriptor[] followingTargets = availableTargets
            .Where(target => string.Equals(target.DeviceName, firstTarget.DeviceName, StringComparison.OrdinalIgnoreCase))
            .Where(target => target.PositionIndex >= knownFirstTarget.PositionIndex)
            .OrderBy(target => target.PositionIndex)
            .ToArray();

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

    private static void EnsureUniqueTargets(IEnumerable<PatchTargetDescriptor> targets)
    {
        bool hasDuplicate = targets
            .GroupBy(target => (target.DeviceName.Trim().ToUpperInvariant(), target.DanteId))
            .Any(group => group.Count() > 1);
        if (hasDuplicate)
        {
            throw new InvalidOperationException("La sélection RX contient plusieurs fois le même canal.");
        }
    }

    private static PatchSourceDescriptor? FindKnownSource(
        IEnumerable<PatchSourceDescriptor> availableSources,
        PatchSourceDescriptor requested)
    {
        return availableSources.FirstOrDefault(source =>
            string.Equals(source.DeviceName, requested.DeviceName, StringComparison.OrdinalIgnoreCase)
            && source.DanteId == requested.DanteId);
    }

    private static PatchTargetDescriptor? FindKnownTarget(
        IEnumerable<PatchTargetDescriptor> availableTargets,
        PatchTargetDescriptor requested)
    {
        return availableTargets.FirstOrDefault(target =>
            string.Equals(target.DeviceName, requested.DeviceName, StringComparison.OrdinalIgnoreCase)
            && target.DanteId == requested.DanteId);
    }
}
