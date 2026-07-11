using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class PatchAssignmentPlannerTests
{
    [Fact]
    public void MultipleTxChannelsMapToFollowingRxChannelsInXmlOrder()
    {
        PatchSourceDescriptor[] sources =
        [
            Source(1, 1, "TX 1"),
            Source(2, 2, "TX 2"),
            Source(3, 3, "TX 3")
        ];
        PatchTargetDescriptor[] targets =
        [
            Target(10, 1, "RX 10"),
            Target(30, 2, "RX 30"),
            Target(50, 3, "RX 50"),
            Target(70, 4, "RX 70")
        ];

        SequentialPatchPlan plan = PatchAssignmentPlanner.PlanSequential(sources, targets, targets[1]);

        Assert.Equal(3, plan.Assignments.Count);
        Assert.Empty(plan.UnassignedSources);
        Assert.Equal([30, 50, 70], plan.Assignments.Select(item => item.Target.DanteId).ToArray());
        Assert.Equal(["TX 1", "TX 2", "TX 3"], plan.Assignments.Select(item => item.Source.ChannelName).ToArray());
    }

    [Fact]
    public void SourcesBeyondTheLastRxAreReportedAsUnassigned()
    {
        PatchSourceDescriptor[] sources =
        [
            Source(1, 1, "TX 1"),
            Source(2, 2, "TX 2"),
            Source(3, 3, "TX 3")
        ];
        PatchTargetDescriptor[] targets = [Target(1, 1, "RX 1"), Target(2, 2, "RX 2")];

        SequentialPatchPlan plan = PatchAssignmentPlanner.PlanSequential(sources, targets, targets[1]);

        Assert.Single(plan.Assignments);
        Assert.Equal(2, plan.UnassignedSources.Count);
        Assert.Equal("TX 2", plan.UnassignedSources[0].ChannelName);
        Assert.Equal("TX 3", plan.UnassignedSources[1].ChannelName);
    }

    [Fact]
    public void SequentialPlanAppliedAsBatchUsesExistingSafePatchWriter()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));
        DanteDevice txDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-A"));
        DanteDevice rxDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-B"));
        PatchSourceDescriptor[] sources = txDevice.TxChannels.Select(channel => new PatchSourceDescriptor(
            txDevice.Name,
            channel.DanteId,
            channel.PositionIndex,
            channel.DisplayName)).ToArray();
        PatchTargetDescriptor[] targets = rxDevice.RxChannels.Select(channel => new PatchTargetDescriptor(
            rxDevice.Name,
            channel.DanteId,
            channel.PositionIndex,
            channel.DisplayName)).ToArray();
        SequentialPatchPlan plan = PatchAssignmentPlanner.PlanSequential(sources.Reverse().ToArray(), targets, targets[0]);

        project.ApplyBatch(batch =>
        {
            foreach (PlannedPatchAssignment assignment in plan.Assignments)
            {
                batch.ApplyPatch(
                    assignment.Target.DeviceName,
                    assignment.Target.DanteId,
                    assignment.Source.DeviceName,
                    assignment.Source.ChannelName);
            }
        });

        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
        DanteSubscription[] subscriptions = project.PatchMatrix.Subscriptions
            .Where(subscription => subscription.RxDevice == "DEVICE-B")
            .OrderBy(subscription => subscription.RxDanteId)
            .ToArray();
        Assert.Equal(["PROGRAM R", "PROGRAM L"], subscriptions.Select(item => item.TxChannelName).ToArray());
    }

    [Fact]
    public void PlannerRejectsAStartRxOutsideTheAvailableDeviceTargets()
    {
        PatchSourceDescriptor[] sources = [Source(1, 1, "TX")];
        PatchTargetDescriptor[] targets = [Target(1, 1, "RX")];
        PatchTargetDescriptor foreignTarget = new("OTHER-RX", 1, 1, "RX");

        Assert.Throws<InvalidOperationException>(() => PatchAssignmentPlanner.PlanSequential(sources, targets, foreignTarget));
    }

    [Fact]
    public void PlannerUsesTheKnownXmlPositionForTheStartingRx()
    {
        PatchSourceDescriptor[] sources = [Source(1, 1, "TX")];
        PatchTargetDescriptor[] targets =
        [
            Target(10, 1, "RX 10"),
            Target(30, 2, "RX 30")
        ];
        PatchTargetDescriptor callerCopyWithWrongPosition = new("RX-DEVICE", 30, 0, "RX 30");

        SequentialPatchPlan plan = PatchAssignmentPlanner.PlanSequential(sources, targets, callerCopyWithWrongPosition);

        Assert.Single(plan.Assignments);
        Assert.Equal(30, plan.Assignments[0].Target.DanteId);
    }

    [Fact]
    public void WorkspaceKeepsEditsPendingUntilTheyAreAppliedByTheCaller()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));
        DanteDevice txDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-A"));
        DanteDevice rxDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-B"));
        PatchWorkspaceSession workspace = new(project.PatchMatrix.Subscriptions);
        PatchSourceDescriptor source = SourceFrom(txDevice.TxChannels[1]);
        PatchTargetDescriptor target = TargetFrom(rxDevice.RxChannels[0]);

        workspace.Assign(new PlannedPatchAssignment(source, target));

        Assert.True(workspace.HasChanges);
        Assert.False(project.IsModified);
        Assert.Equal("PROGRAM R", workspace.GetEffectiveAssignment(target).TxChannelName);
        Assert.Equal(
            "PROGRAM L",
            project.PatchMatrix.Subscriptions.Single(item =>
                string.Equals(item.RxDevice, target.DeviceName, StringComparison.OrdinalIgnoreCase)
                && item.RxDanteId == target.DanteId).TxChannelName);
    }

    [Fact]
    public void WorkspaceRemovesPendingEditWhenAssignmentReturnsToOriginalSource()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));
        DanteDevice txDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-A"));
        DanteDevice rxDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-B"));
        PatchWorkspaceSession workspace = new(project.PatchMatrix.Subscriptions);
        PatchTargetDescriptor target = TargetFrom(rxDevice.RxChannels[0]);

        workspace.Assign(new PlannedPatchAssignment(SourceFrom(txDevice.TxChannels[1]), target));
        workspace.Assign(new PlannedPatchAssignment(SourceFrom(txDevice.TxChannels[0]), target));

        Assert.False(workspace.HasChanges);
        Assert.Empty(workspace.Edits);
    }

    [Fact]
    public void WorkspaceCanResetAllPendingVisualChanges()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));
        DanteDevice rxDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-B"));
        PatchWorkspaceSession workspace = new(project.PatchMatrix.Subscriptions);
        PatchTargetDescriptor target = TargetFrom(rxDevice.RxChannels[0]);

        workspace.Remove(target);
        Assert.Single(workspace.Edits);

        workspace.Reset();

        Assert.False(workspace.HasChanges);
        Assert.True(workspace.GetEffectiveAssignment(target).IsActive);
    }

    [Fact]
    public void WorkspaceEditsApplyAsOneSafeProjectBatch()
    {
        using TestDirectory directory = new();
        DanteProject project = DanteProject.Load(directory.CopyFixture("representative-preset.xml"));
        DanteDevice txDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-A"));
        DanteDevice rxDevice = Assert.IsType<DanteDevice>(project.FindDevice("DEVICE-B"));
        PatchWorkspaceSession workspace = new(project.PatchMatrix.Subscriptions);
        PatchTargetDescriptor firstRx = TargetFrom(rxDevice.RxChannels[0]);
        PatchTargetDescriptor secondRx = TargetFrom(rxDevice.RxChannels[1]);

        workspace.Remove(firstRx);
        workspace.Assign(new PlannedPatchAssignment(SourceFrom(txDevice.TxChannels[0]), secondRx));

        project.ApplyBatch(batch =>
        {
            foreach (PatchEditRequest edit in workspace.Edits)
            {
                if (edit.IsRemoval)
                {
                    batch.RemovePatch(edit.RxDeviceName, edit.RxDanteId);
                }
                else
                {
                    batch.ApplyPatch(edit.RxDeviceName, edit.RxDanteId, edit.TxDeviceName!, edit.TxChannelName!);
                }
            }
        });

        Assert.False(project.ValidateXmlChangeGuard().HasErrors);
        DanteSubscription[] subscriptions = project.PatchMatrix.Subscriptions
            .Where(item => string.Equals(item.RxDevice, rxDevice.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.RxPositionIndex)
            .ToArray();
        Assert.False(subscriptions[0].IsActive);
        Assert.Equal("PROGRAM L", subscriptions[1].TxChannelName);
    }

    private static PatchSourceDescriptor Source(int danteId, int position, string name)
    {
        return new PatchSourceDescriptor("TX-DEVICE", danteId, position, name);
    }

    private static PatchTargetDescriptor Target(int danteId, int position, string name)
    {
        return new PatchTargetDescriptor("RX-DEVICE", danteId, position, name);
    }

    private static PatchSourceDescriptor SourceFrom(DanteChannel channel)
    {
        return new PatchSourceDescriptor(channel.DeviceName, channel.DanteId, channel.PositionIndex, channel.DisplayName);
    }

    private static PatchTargetDescriptor TargetFrom(DanteChannel channel)
    {
        return new PatchTargetDescriptor(channel.DeviceName, channel.DanteId, channel.PositionIndex, channel.DisplayName);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.PatchPlannerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string CopyFixture(string name)
        {
            string destination = Path.Combine(Root, name);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), destination, true);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
