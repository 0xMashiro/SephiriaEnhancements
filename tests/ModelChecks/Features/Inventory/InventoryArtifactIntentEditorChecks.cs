using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryArtifactIntentEditorChecks
{
    internal static string Run()
    {
        VerifyQueueLevelOwnership();
        InventoryOptimizationPreferences original =
            InventoryOptimizationPreferences.Default;
        InventoryOptimizationPreferences marked =
            InventoryArtifactIntentEditor.Toggle(original, 501, 10);
        ArtifactOptimizationPreference rule = marked.ArtifactPreferences.
            Single();
        if (!rule.TargetsInstance || rule.InstanceId != 501 ||
            rule.EntityId != 10 ||
            rule.Level != InventoryPreferenceLevel.Priority ||
            rule.MinimumEffectiveLevel != 0 ||
            rule.PriorityOrder != 0 ||
            !InventoryArtifactIntentEditor.IsMarked(marked, new InventoryItemKey(10, 501)) ||
            InventoryArtifactIntentEditor.Count(marked) != 1)
        {
            throw new InvalidOperationException(
                "temporary priority marks must target one artifact instance");
        }

        InventoryOptimizationPreferences unmarked =
            InventoryArtifactIntentEditor.Toggle(marked, 501, 10);
        if (unmarked.ArtifactPreferences.Count != 0 ||
            InventoryArtifactIntentEditor.IsMarked(unmarked, new InventoryItemKey(10, 501)))
        {
            throw new InvalidOperationException(
                "toggling a marked artifact must restore automatic behavior");
        }

        InventoryOptimizationPreferences twoMarks =
            InventoryArtifactIntentEditor.Toggle(
                InventoryArtifactIntentEditor.Toggle(original, 501, 10),
                502, 10);
        InventoryOptimizationPreferences reordered =
            InventoryArtifactIntentEditor.PlacePriority(twoMarks, 502, 10, 0);
        ArtifactOptimizationPreference[] ordered =
            InventoryArtifactIntentEditor.OrderedPriorities(reordered);
        if (ordered.Length != 2 || ordered[0].InstanceId != 502 ||
            ordered[0].PriorityOrder != 0 ||
            ordered[1].InstanceId != 501 || ordered[1].PriorityOrder != 1)
        {
            throw new InvalidOperationException(
                "priority queue placement must define a stable order");
        }
        InventoryOptimizationPreferences avoided =
            InventoryArtifactIntentEditor.PlaceAvoid(reordered, 501, 10, 0);
        if (InventoryArtifactIntentEditor.OrderedPriorities(avoided).Length != 1 ||
            InventoryArtifactIntentEditor.AvoidedInstances(avoided).Single().
                InstanceId != 501)
        {
            throw new InvalidOperationException(
                "moving an artifact to exclusion must remove it from priority");
        }
        InventoryOptimizationPreferences pruned =
            InventoryArtifactIntentEditor.Prune(twoMarks, new[] { new InventoryItemKey(10, 502) });
        if (pruned.ArtifactPreferences.Count != 1 ||
            !InventoryArtifactIntentEditor.IsMarked(pruned, new InventoryItemKey(10, 502)) ||
            InventoryArtifactIntentEditor.IsMarked(pruned, new InventoryItemKey(10, 501)))
        {
            throw new InvalidOperationException(
                "marks for artifact instances outside the inventory must be removed");
        }

        return "ordered priority, exclusion and stale intent pruning passed";
    }

    private static void VerifyQueueLevelOwnership()
    {
        var snapshot = SephiriaEnhancements.ModelChecks.Runtime.Inventory.InventorySnapshotFixture.
            DuplicateArtifactsAtLevels(new[] { 4, 0 }, new[] { 0, 1 }, maxLevel: 5);
        var first = snapshot.Items[0].ItemKey;
        var second = snapshot.Items[1].ItemKey;
        var preferences = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default,
            first.NativeInstanceId, first.EntityId, 0);
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, second.NativeInstanceId, second.EntityId, 1);
        var changed = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, first, 4);
        if (preferences.ArtifactPreferences.Any(rule => rule.MinimumEffectiveLevel != 0) ||
            changed.ArtifactPreferences.Single(rule => rule.ItemKey == first).MinimumEffectiveLevel != 4 ||
            changed.ArtifactPreferences.Single(rule => rule.ItemKey == second).MinimumEffectiveLevel != 0)
            throw new InvalidOperationException("a queue level belongs to exactly one artifact instance, including duplicate names");
        var reordered = InventoryArtifactIntentEditor.PlacePriority(changed, first.NativeInstanceId, first.EntityId, 1);
        if (reordered.ArtifactPreferences.Single(rule => rule.ItemKey == first).MinimumEffectiveLevel != 4 ||
            reordered.ArtifactPreferences.Single(rule => rule.ItemKey == first).PriorityOrder != 1)
            throw new InvalidOperationException("reordering must preserve the level target on the artifact");
        foreach (var (requested, expected) in new[] { (-1, 0), (0, 0), (99, 5) })
        {
            var bounded = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(changed, snapshot, first, requested);
            if (bounded.ArtifactPreferences.Single(rule => rule.ItemKey == first).MinimumEffectiveLevel != expected)
                throw new InvalidOperationException("queue levels must include zero and respect the artifact's maximum");
        }
        var avoided = InventoryArtifactIntentEditor.PlaceAvoid(changed, first.NativeInstanceId, first.EntityId, 0);
        if (!ReferenceEquals(avoided, InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(avoided, snapshot, first, 3)) ||
            !ReferenceEquals(changed, InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(changed, null, first, 3)))
            throw new InvalidOperationException("excluded or missing artifacts must not acquire a level target");
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, changed);
        var layout = InventoryLayoutProjection.Current(snapshot);
        var settlement = InventorySettlementProjector.Evaluate(snapshot, layout);
        var evaluation = new InventoryOptimizationScorer(snapshot, policy).EvaluateTargets(settlement, settlement)
            .Single(target => target.Target == "Artifact:" + first.EntityId + ":" + first.NativeInstanceId);
        if (evaluation.RequiredValue != 4 || !evaluation.AfterConditionReached)
            throw new InvalidOperationException("the queue editor must update the exact target used by the solver");
    }
}
