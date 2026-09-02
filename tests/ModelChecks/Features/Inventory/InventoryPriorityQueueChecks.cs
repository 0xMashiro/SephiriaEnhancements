using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryPriorityQueueChecks
{
    internal static void Run()
    {
        VerifySixSlotQueue(scarceLevels: false);
        VerifySixSlotQueue(scarceLevels: true);
        VerifyReorderedQueue();
        VerifySatisfiedTargetYieldsToNextSlot();
        VerifyExclusionsPrecedeQueue();
        VerifyBudgetKeepsPriorityImprovement();
        Console.WriteLine("InventoryPriorityQueue: six slots, scarce levels, reordering, exclusions and interrupted search passed");
    }

    private static InventoryOptimizationPreferences Queue(InventorySnapshot snapshot,
        bool scarceLevels)
    {
        var preferences = InventoryOptimizationPreferences.Default;
        for (int slot = 0; slot < 6; slot++)
        {
            var item = snapshot.Items[slot];
            preferences = InventoryArtifactIntentEditor.PlacePriority(preferences,
                item.InstanceId, item.EntityId, slot);
            preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(
                preferences, snapshot, item.ItemKey, scarceLevels ? 6 : 6 - slot);
        }
        return preferences;
    }

    private static ResolvedInventoryOptimizationPolicy Compose(InventorySnapshot snapshot,
        InventoryOptimizationPreferences preferences) => InventoryOptimizationPolicyResolver.Resolve(
            snapshot, InventoryOptimizationPreferenceComposer.Compose(
                InventoryOptimizationPreferences.Default, preferences,
                InventorySearchEffort.Balanced, allowStoneTabletRotation: true));

    private static void VerifySixSlotQueue(bool scarceLevels)
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, maxLevel: 6);
        var policy = Compose(snapshot, Queue(snapshot, scarceLevels));
        var budget = new InventorySearchBudget(16, 10000, 10000);
        foreach (var proposal in new[]
        {
            InventoryOptimizerSelector.Solve(snapshot, policy, budget),
            InventoryOptimizer.Solve(snapshot, policy, budget)
        })
        {
            if (!proposal.Succeeded) throw new InvalidOperationException("six-slot queue rejected");
            var settlement = InventorySettlementProjector.Evaluate(snapshot, proposal.Layout);
            for (int slot = 0; slot < 6; slot++)
            {
                int level = settlement.Artifacts.Single(a => a.ItemKey == snapshot.Items[slot].ItemKey).CappedEffectiveLevel;
                if (level != 6 - slot)
                    throw new InvalidOperationException($"priority slot {slot + 1} received level {level}; scarce={scarceLevels}");
            }
        }
    }

    private static void VerifyReorderedQueue()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, maxLevel: 6);
        var preferences = Queue(snapshot, scarceLevels: true);
        var first = snapshot.Items[0];
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences,
            first.InstanceId, first.EntityId, 5);
        var proposal = InventoryOptimizerSelector.Solve(snapshot, Compose(snapshot, preferences),
            new InventorySearchBudget(16, 10000, 10000));
        var settlement = InventorySettlementProjector.Evaluate(snapshot, proposal.Layout);
        foreach (var rule in InventoryArtifactIntentEditor.OrderedPriorities(preferences))
        {
            int level = settlement.Artifacts.Single(a => a.ItemKey == rule.ItemKey).CappedEffectiveLevel;
            if (level != 6 - rule.PriorityOrder || rule.MinimumEffectiveLevel != 6)
                throw new InvalidOperationException("reordering must change allocation order while keeping the item's target");
        }
    }

    private static void VerifyExclusionsPrecedeQueue()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0, 1 }, new[] { 0, 1 });
        var priority = snapshot.Items[0];
        var excluded = snapshot.Items[1];
        var preferences = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default,
            priority.InstanceId, priority.EntityId, 0);
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, priority.ItemKey, 1);
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, excluded.InstanceId, excluded.EntityId, 0);
        var policy = Compose(snapshot, preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        var current = InventoryLayoutProjection.Current(snapshot);
        var priorityOnly = current.WithCellsSwapped(0, 2);
        var exclusionFirst = current.WithCellsSwapped(0, 1);
        if (scorer.Score(exclusionFirst, InventorySettlementProjector.Evaluate(snapshot, exclusionFirst)).CompareTo(
                scorer.Score(priorityOnly, InventorySettlementProjector.Evaluate(snapshot, priorityOnly))) <= 0)
            throw new InvalidOperationException("an exclusion must outrank completing the priority target");
        var limited = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(8, 2, 10000));
        if (!limited.Improved || limited.BestScore.AvoidedTargetsActive != 0)
            throw new InvalidOperationException("a discovered exclusion improvement must survive the search budget");
        var proposal = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(8, 100, 10000));
        var settlement = InventorySettlementProjector.Evaluate(snapshot, proposal.Layout);
        if (settlement.Artifacts.Single(a => a.ItemKey == excluded.ItemKey).Enabled ||
            settlement.Artifacts.Single(a => a.ItemKey == priority.ItemKey).CappedEffectiveLevel != 1)
            throw new InvalidOperationException("compatible exclusion and priority requirements must both be satisfied");
    }

    private static void VerifySatisfiedTargetYieldsToNextSlot()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 4, 5 }, maxLevel: 6);
        foreach (int firstTarget in new[] { 0, 1 })
        {
            var preferences = Queue(snapshot, scarceLevels: true);
            for (int slot = 0; slot < 6; slot++)
                preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot,
                    snapshot.Items[slot].ItemKey, slot == 0 ? firstTarget : 7 - slot);
            var proposal = InventoryOptimizerSelector.Solve(snapshot, Compose(snapshot, preferences),
                new InventorySearchBudget(16, 10000, 10000));
            var settlement = InventorySettlementProjector.Evaluate(snapshot, proposal.Layout);
            for (int slot = 0; slot < 6; slot++)
            {
                int expected = slot == 0 ? 1 : 7 - slot;
                var artifact = settlement.Artifacts.Single(a => a.ItemKey == snapshot.Items[slot].ItemKey);
                if (!artifact.Enabled || artifact.CappedEffectiveLevel != expected)
                    throw new InvalidOperationException("a completed earlier target must yield upgrades to the next queue slot");
            }
        }
    }

    private static void VerifyBudgetKeepsPriorityImprovement()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 2, 1 }, new[] { 0, 1 });
        var priority = snapshot.Items[0];
        var preferences = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default,
            priority.InstanceId, priority.EntityId, 0);
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, priority.ItemKey, 2);
        var proposal = InventoryOptimizer.Solve(snapshot, Compose(snapshot, preferences),
            new InventorySearchBudget(8, 2, 10000));
        if (!proposal.Succeeded || !proposal.Improved || proposal.Layout.GetCell(0) != 1 ||
            proposal.CandidateEvaluations != 2 || proposal.TerminationReason != InventorySearchTerminationReason.CandidateEvaluationLimit)
            throw new InvalidOperationException("search discarded an already evaluated layout satisfying slot 1 when its budget expired");
        if (!proposal.TargetEvaluations.Single(t => t.Target == "Artifact:" + priority.EntityId + ":" + priority.InstanceId).AfterConditionReached)
            throw new InvalidOperationException("returned target feedback must describe the retained best layout");
    }
}
