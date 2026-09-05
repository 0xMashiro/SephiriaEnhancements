using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOperationOwnershipChecks
{
    internal static void Run()
    {
        VerifySearchCancellation();
        VerifySwapAcknowledgements();
        VerifyRotationAcknowledgements();
        Console.WriteLine("InventoryOperationOwnership: task isolation, cancellation, stale/duplicate acknowledgements, whole-board rejection and multi-click rotations passed");
    }

    private static RuntimeStateSnapshot Runtime(long revision = 1, long epoch = 1, uint player = 1,
        RuntimeConsistencyState consistency = RuntimeConsistencyState.Consistent) => new("fixture", epoch, 1, revision, 1, player,
            RuntimeCapabilities.InventorySnapshot | RuntimeCapabilities.SettledInventoryObservation, consistency, 0, "");

    private static void VerifySearchCancellation()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 1 }, new[] { 0 });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
        var proposal = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(4, 10, 1000));
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var old = new InventoryOptimizationSearch(snapshot, Runtime(), token =>
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
            token.ThrowIfCancellationRequested();
            return proposal;
        });
        try
        {
            Require(started.Wait(TimeSpan.FromSeconds(5)), "worker did not start");
            old.Dispose();
            using var next = new InventoryOptimizationSearch(snapshot, Runtime(revision: 2), _ => proposal);
            Require(next.Task.Wait(TimeSpan.FromSeconds(5)) && ReferenceEquals(next.Task.Result, proposal), "replacement task failed");
            Require(next.SourceRuntime.InventoryRevision == 2 && old.SourceRuntime.InventoryRevision == 1 &&
                ReferenceEquals(next.SourceSnapshot, snapshot), "search inputs changed across replacement");
            release.Set();
            try { old.Task.GetAwaiter().GetResult(); throw new InvalidOperationException("cancelled task returned a proposal"); }
            catch (OperationCanceledException) { }
            Require(old.Task.IsCanceled && next.Task.IsCompletedSuccessfully, "old cancellation affected replacement");
        }
        finally { release.Set(); }
        using var fault = new InventoryOptimizationSearch(snapshot, Runtime(), _ => throw new InvalidOperationException("solver fault"));
        try { fault.Task.GetAwaiter().GetResult(); throw new Exception("fault was swallowed"); }
        catch (InvalidOperationException ex) when (ex.Message == "solver fault") { }
    }

    private static InventoryLayoutApplication Application(InventorySnapshot source, InventoryLayoutProjection target)
    {
        Require(InventoryLayoutPlanner.TryCreate(source, target, out var plan, out _), "plan rejected");
        var policy = InventoryOptimizationPolicyResolver.Resolve(source, InventoryOptimizationPreferences.Default);
        var scorer = new InventoryOptimizationScorer(source, policy);
        var current = InventoryLayoutProjection.Current(source);
        var expected = InventorySettlementProjector.Evaluate(source, target);
        var proposal = new InventoryOptimizationProposal(true, target,
            scorer.Score(current, InventorySettlementProjector.Evaluate(source, current)),
            scorer.Score(target, expected), 1, Array.Empty<string>(), policy);
        return new InventoryLayoutApplication(source, Runtime(), proposal, plan, expected, 20);
    }

    private static void VerifySwapAcknowledgements()
    {
        int[] levels = { -1, 0, 2, 6 };
        var source = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 0, 1, 2, 3 });
        var target = new InventoryLayoutProjection(new[] { 1, 0, 3, 2 }, new int[4]);
        var application = Application(source, target);
        var first = application.Plan.Swaps[0];
        var intermediate = application.ConfirmedLayout.WithCellsSwapped(first.FirstCell, first.SecondCell);
        var observed = InventorySnapshotFixture.ArtifactsAtLevels(levels, intermediate.CopyCells());
        application.BeginSwap(1);
        foreach (var stale in new[] { Runtime(), Runtime(0), Runtime(2, epoch: 2), Runtime(2, player: 2),
            Runtime(2, consistency: RuntimeConsistencyState.PendingSettlement) })
            Require(!application.TryObservePendingOperation(observed, stale, out _) && application.NextSwap == 0, "stale acknowledgement advanced cursor");
        Require(!application.TryObservePendingOperation(source, Runtime(2), out _), "unchanged board acknowledged swap");
        var unrelatedMove = InventorySnapshotFixture.ArtifactsAtLevels(levels, target.CopyCells());
        Require(application.TryObservePendingOperation(unrelatedMove, Runtime(2), out var rejected) && !rejected.Matched &&
            application.NextSwap == 0 && application.ConfirmedRevision == 1 &&
            application.PendingOperation == InventoryPendingOperation.Swap, "unrelated move advanced cursor");
        var wrongSettlement = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0, 3, 6 }, intermediate.CopyCells());
        Require(application.TryObservePendingOperation(wrongSettlement, Runtime(2), out rejected) && !rejected.Matched &&
            application.NextSwap == 0, "changed settlement advanced cursor");
        Require(application.TryObservePendingOperation(observed, Runtime(2), out var accepted) && accepted.Matched &&
            application.NextSwap == 1 && application.ConfirmedRevision == 2 &&
            application.ConfirmedLayout.ContentEquals(intermediate), "valid swap did not advance");
        Require(!application.TryObservePendingOperation(observed, Runtime(3), out _) && application.NextSwap == 1, "duplicate acknowledgement advanced cursor");
        application.BeginSwap(2);
        Require(!application.TryObservePendingOperation(unrelatedMove, Runtime(2), out _), "second operation reused first revision");
        Require(application.TryObservePendingOperation(unrelatedMove, Runtime(3), out accepted) && accepted.Matched &&
            application.NextSwap == application.Plan.Swaps.Count && application.ConfirmedLayout.ContentEquals(target), "second swap did not complete");
        var replacement = Application(source, target);
        Require(replacement.NextSwap == 0 && replacement.ConfirmedRevision == 1 &&
            replacement.PendingOperation == InventoryPendingOperation.None, "application state leaked into replacement");
    }

    private static void VerifyRotationAcknowledgements()
    {
        var source = InventorySnapshotFixture.Tablets(0, 0);
        var target = new InventoryLayoutProjection(new[] { 0, 1 }, new[] { 0, 3 });
        var application = Application(source, target);
        application.BeginRotation(1, 0);
        Require(!application.TryObservePendingOperation(InventorySnapshotFixture.Tablets(1, 0), Runtime(2), out _), "wrong tablet acknowledged rotation");
        for (int rotation = 1; rotation <= 3; rotation++)
        {
            Require(application.TryObservePendingOperation(InventorySnapshotFixture.Tablets(0, rotation), Runtime(rotation + 1), out var report) &&
                report.Matched && application.NextRotation == (rotation == 3 ? 1 : 0) &&
                application.PendingOperation == InventoryPendingOperation.None &&
                application.ConfirmedLayout.GetRotation(1) == rotation, "rotation click advanced incorrectly");
            Require(!application.TryObservePendingOperation(InventorySnapshotFixture.Tablets(0, rotation), Runtime(rotation + 2), out _), "duplicate rotation acknowledged");
            if (rotation < 3) application.BeginRotation(rotation + 1, rotation);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
